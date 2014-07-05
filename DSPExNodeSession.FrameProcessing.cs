using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.ServiceModel.Channels;
using System.Threading;

namespace Dargon.Transport
{
   public unsafe partial class DSPExNodeSession
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

      private readonly BlockingCollection<DSPExNodeSessionFrameProcessor> m_idleFrameProcessors = new BlockingCollection<DSPExNodeSessionFrameProcessor>(new ConcurrentStack<DSPExNodeSessionFrameProcessor>());
      private readonly List<DSPExNodeSessionFrameProcessor> m_frameProcessors = new List<DSPExNodeSessionFrameProcessor>();
      private readonly object m_frameProcessorCollectionLock = new object(); // only for m_frameProcessors if we ever spin up new instances...

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
         if (!m_dspExElevated)
         {
            var opcode = m_reader.ReadByte();
            if (opcode != (byte)DTP.DSPEX_INIT)
               throw new NotSupportedException("Expected DSP_EX_INIT opcode!");
            m_dspExElevated = true;
         }

         try
         {
            while (m_node.IsAlive && IsAlive)
            {
               var frameLength = m_reader.ReadUInt32(); // includes frameLength
               Console.WriteLine("Reading DSPEx Frame of Length " + frameLength);
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

               Console.WriteLine("Sending DSPEx Frame of Length " + frameLength + " to processor");
               var processor = m_idleFrameProcessors.Take();
               processor.AssignFrame(buffer);
               Console.WriteLine("Sent DSPEx Frame of Length " + frameLength + " to processor");
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
            Console.WriteLine("Writing DSPEx Frame of Length " + frameLength);
            m_writer.Write(buffer, 0, frameLength);
            Console.WriteLine("Wrote DSPEx Frame of Length " + frameLength);

            ReturnFrameBuffer(buffer);
         }
      }

      // - Frame Processor Management -------------------------------------------------------------
      private void AddFrameProcessor()
      {
         var instance = new DSPExNodeSessionFrameProcessor(
            m_node,
            this,
            (processor) => {
               // Return the processor's frame buffer to the buffer pool
               ReturnFrameBuffer(processor.ReleaseAssignedFrame());

               m_idleFrameProcessors.Add(processor);
            } // lambda
            );

         lock (m_frameProcessorCollectionLock)
            m_frameProcessors.Add(instance);
         m_idleFrameProcessors.Add(instance); // innately thread safe due to collection type
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
