using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.ServiceModel.Channels;
using System.Threading;
using ItzWarty;
using Logger = Dargon.Transport.__DummyLoggerThisIsHorrible;

namespace Dargon.Transport
{
   public unsafe partial class DtpNodeSession
   {
      // - Static Constants -----------------------------------------------------------------------
      private const uint kClientLitIdLow = 0x00000000U;
      private const uint kClientLitIdHigh = 0x7FFFFFFFU;
      private const uint kServerLitIdLow = 0x80000000U;
      private const uint kServerLitIdHigh = 0xFFFFFFFFU;

      private readonly Stream m_connection;

      // - Producer and Consumer Architecture -----------------------------------------------------
      // We define one Frame Reader and one Frame Writer thread, along with numerous processor
      // threads. Frame readers assign input frames to processors.  Processors process the frames
      // and pass output frames to the writer thread, which writes them to the underlying stream.
      // A LIFO stack is used for idle frame processors in hope of getting a warm cache.
      private readonly Thread m_frameReaderThread;
      private readonly Thread m_frameWriterThread;
      private readonly BinaryReader m_reader;
      private readonly BinaryWriter m_writer;

      private DtpNodeSessionFrameProcessor[] m_frameProcessors;
      
      // : Frame Reader : - Actually touched by reader, writer, and processors at the moment.
      private readonly BufferManager m_frameBufferPool = BufferManager.CreateBufferManager(20, DTPConstants.kMaxMessageSize);
      private readonly object m_frameBufferPoolLock = new object();

      // : Frame Writer :
      private readonly BlockingCollection<byte[]> m_frameBuffersToSend = new BlockingCollection<byte[]>(new ConcurrentQueue<byte[]>());

      // : dspex state :
      private bool m_dspExElevated = false; // If false, the connection hasn't been elevated to DSPEx yet

      // Note: Frame Reader owns m_reader and can access it without a lock
      private void FrameReaderThreadStart()
      {
         if (m_localRole == DSPExNodeRole.Server)
         {
            // Read dspex init elevation
            var opcode = m_reader.ReadByte();
            if (opcode != (byte)DTP.DSPEX_INIT)
               throw new NotSupportedException("Expected DSP_EX_INIT opcode!");
            m_dspExElevated = true;
         }
         else
         {
            m_dspExElevated = true; // performed at ctor
         }

         try
         {
            int frameProcessorRoundRobinIndex = 0;
            while (m_node.IsAlive && IsAlive)
            {
               var frameLength = m_reader.ReadUInt32(); // includes frameLength
               Logger.L(LoggerLevel.Info, "Reading DSPEx Frame of Length " + frameLength);
               var frameContentLength = (int)frameLength - 4;
               var buffer = TakeFrameBuffer(frameContentLength); // thread safe

               // Copy frame length to the buffer's first four bytes
               buffer[0] = (byte)((frameLength >> 0) & 0xFF);
               buffer[1] = (byte)((frameLength >> 8) & 0xFF);
               buffer[2] = (byte)((frameLength >> 16) & 0xFF);
               buffer[3] = (byte)((frameLength >> 24) & 0xFF);

               var bytesToRead = frameContentLength;
               while (bytesToRead > 0)
               {
                  var bytesRead = m_reader.Read(buffer, 4 + frameContentLength - bytesToRead, bytesToRead);
                  bytesToRead -= bytesRead;
               }

               //for (int i = 0; i < frameContentLength + 4; i++)
               //   Console.WriteLine(i + ": " + buffer[i].ToString());

               Logger.L(LoggerLevel.Info, "Sending DSPEx Frame of Length " + frameLength + " to processor");
               var index = frameProcessorRoundRobinIndex;
               var processor = m_frameProcessors[frameProcessorRoundRobinIndex];
               processor.EnqueueFrame(buffer);
               frameProcessorRoundRobinIndex = (index + 1) % m_frameProcessors.Length;
               Logger.L(LoggerLevel.Info, "Sent DSPEx Frame of Length " + frameLength + " to processor");
            }
         }
         catch (EndOfStreamException e)
         {
            // end of session
            IsAlive = false;
            Console.WriteLine("Disconnected.");
         }
      }

      // Note: Frame Writer owns m_writer and can access it without a lock
      private void FrameWriterThreadStart()
      {
         while (m_node.IsAlive && IsAlive)
         {
            // used byte shifting rather than binaryreader/fixed as this is a fairly simple op...
            var buffer = m_frameBuffersToSend.Take(); // Note: Buffer length != frame length!!!
            int frameLength = buffer[0] | (buffer[1] << 8) | (buffer[2] << 16) | (buffer[3] << 24);
            Logger.L(LoggerLevel.Info, "Writing DSPEx Frame of Length " + frameLength);
            m_writer.Write(buffer, 0, frameLength);
            Logger.L(LoggerLevel.Info, "Wrote DSPEx Frame of Length " + frameLength);

            ReturnFrameBuffer(buffer);
         }
      }

      // - Frame Processor Management -------------------------------------------------------------
      // TODO: This method should only be used by the constructor!
      private void InitializeFrameProcessors(int threadCount)
      {
         var frameProcessors = Util.Generate(
            threadCount,
            (i) => new DtpNodeSessionFrameProcessor(
               m_node,
               this,
               (processor, frame) => ReturnFrameBuffer(frame) 
            ));

         m_frameProcessors = frameProcessors;
      }

      // - Frame Reader Utility Methods -----------------------------------------------------------
      internal byte[] TakeFrameBuffer(int size)
      {
         lock (m_frameBufferPoolLock)
            return m_frameBufferPool.TakeBuffer(size);
      }

      internal void ReturnFrameBuffer(byte[] buffer)
      {
         lock (m_frameBufferPoolLock)
            m_frameBufferPool.ReturnBuffer(buffer);
      }
   }
}
