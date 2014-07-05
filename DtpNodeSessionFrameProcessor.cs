using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Threading;

using Logger = Dargon.Transport.__DummyLoggerThisIsHorrible;

namespace Dargon.Transport
{
   public class DtpNodeSessionFrameProcessor
   {
      private readonly DtpNode m_node;
      private readonly DtpNodeSession m_session;
      private readonly Action<DtpNodeSessionFrameProcessor, byte[]> m_onFrameProcessed;

      private readonly Thread m_thread;

      private readonly Semaphore m_semaphore = new Semaphore(0, Int32.MaxValue);
      private readonly ConcurrentQueue<byte[]> m_frameQueue = new ConcurrentQueue<byte[]>();

      public DtpNodeSessionFrameProcessor(
         DtpNode node, 
         DtpNodeSession session, 
         Action<DtpNodeSessionFrameProcessor, byte[]> onFrameProcessed)
      {
         m_node = node;
         m_session = session;
         m_onFrameProcessed = onFrameProcessed;
         m_thread = new Thread(ThreadStart) { IsBackground = true };
         m_thread.Start();
      }

      private void ThreadStart()
      {
         var id = Thread.CurrentThread.ManagedThreadId;
         while (m_node.IsAlive && m_session.IsAlive)
         {
            if (!m_semaphore.WaitOne(10000))
               continue;

            byte[] assignedFrame = null;
            while (!m_frameQueue.TryDequeue(out assignedFrame)) ;

            Logger.L(LoggerLevel.Info, "Frame Processor " + id + " got frame of buffer size " + assignedFrame.Length);
            using (var ms = new MemoryStream(assignedFrame))
            using (var reader = new BinaryReader(ms))
            {
               UInt32 frameSize = reader.ReadUInt32();
               UInt32 transactionId = reader.ReadUInt32();
               Logger.L(LoggerLevel.Info, " => Frame Size: " + frameSize);

               bool isLit = m_session.IsLocallyInitializedTransaction(transactionId);
               Logger.L(LoggerLevel.Info, " => Is LIT?: " + isLit);
               if (isLit)
               {
                  var handler = m_session.GetLocallyInitializedTransactionHandler(transactionId);
                  if (handler == null)
                     throw new KeyNotFoundException("Referenced nonexistent LIT " + transactionId);

                  handler.ProcessMessage(
                     m_session,
                     new TransactionMessage(
                        transactionId,
                        assignedFrame, 
                        8,
                        (int)(frameSize - 8)
                     )
                  );
               }
               else // riTransaction:
               {
                  var opcode = frameSize > 8 ? assignedFrame[8] : (byte)0;

                  // GoCRITH returns true if the handler was created, false if it existed
                  var handler = m_session.GetRemotelyInitializedTransactionHandler(transactionId, opcode);
                  if (handler == null)
                  {
                     Logger.L(LoggerLevel.Info, " => Handler Nonexistant! Opcode: " + opcode);
                     handler = m_session.CreateAndRegisterRITransactionHandler(transactionId, opcode);
                     handler.ProcessInitialMessage(
                        m_session,
                        new TransactionInitialMessage(
                           transactionId,
                           opcode,
                           assignedFrame,
                           9,
                           (int)(frameSize - 9)
                        )
                     );
                  }
                  else
                  {
                     Logger.L(LoggerLevel.Info, " => Handler Existant!");
                     handler.ProcessMessage(
                        m_session,
                        new TransactionMessage(
                           transactionId,
                           assignedFrame,
                           8,
                           (int)(frameSize - 8)
                        )
                     );
                  }
               }
            } // using
            m_onFrameProcessed(this, assignedFrame);
         } // while
      }

      /// <summary>
      /// Assigns the given frame content to this frame processor.
      /// </summary>
      internal void EnqueueFrame(byte[] frame)
      {
         if (frame == null)
            throw new ArgumentNullException("frame");

         m_frameQueue.Enqueue(frame);
         Thread.MemoryBarrier();
         m_semaphore.Release();
      }
   }
}