using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows.Forms;
using ItzWarty;

namespace Dargon.IO.DSP
{
   public class DSPExNodeSessionFrameProcessor
   {
      private readonly DSPExNode m_node;
      private readonly DSPExNodeSession m_session;
      private readonly Action<DSPExNodeSessionFrameProcessor> m_onFrameProcessed;

      private readonly Thread m_thread;

      private readonly AutoResetEvent m_synchronization = new AutoResetEvent(false);
      private byte[] m_assignedFrame = null;

      public DSPExNodeSessionFrameProcessor(
         DSPExNode node, 
         DSPExNodeSession session, 
         Action<DSPExNodeSessionFrameProcessor> onFrameProcessed)
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
            if (!m_synchronization.WaitOne(10000)) continue;
            Console.WriteLine("Frame Processor " + id + " got frame of buffer size " + m_assignedFrame.Length);
            using (var ms = new MemoryStream(m_assignedFrame))
            using (var reader = new BinaryReader(ms))
            {
               UInt32 frameSize = reader.ReadUInt32();
               UInt32 transactionId = reader.ReadUInt32();
               Console.WriteLine(" => Frame Size: " + frameSize);

               bool isLit = m_session.IsLocallyInitializedTransaction(transactionId);
               Console.WriteLine(" => Is LIT?: " + isLit);
               if (isLit)
               {
                  var handler = m_session.GetLocallyInitializedTransactionHandler(transactionId);
                  if (handler == null)
                     throw new KeyNotFoundException("Referenced nonexistent LIT " + transactionId);

                  handler.ProcessMessage(
                     m_session,
                     new DSPExMessage(
                        transactionId, 
                        m_assignedFrame, 
                        8,
                        (int)(frameSize - 8)
                     )
                  );
               }
               else // riTransaction:
               {
                  var opcode = frameSize > 8 ? m_assignedFrame[8] : (byte)0;

                  // GoCRITH returns true if the handler was created, false if it existed
                  var handler = m_session.GetRemotelyInitializedTransactionHandler(transactionId, opcode);
                  if (handler == null)
                  {
                     Console.WriteLine(" => Handler Nonexistant! Opcode: " + opcode);
                     handler = m_session.CreateAndRegisterRITransactionHandler(transactionId, opcode, m_session.SelectedGame);
                     handler.ProcessInitialMessage(
                        m_session,
                        new DSPExInitialMessage(
                           transactionId,
                           opcode,
                           m_assignedFrame,
                           9,
                           (int)(frameSize - 9)
                        )
                     );
                  }
                  else
                  {
                     Console.WriteLine(" => Handler Existant!");
                     handler.ProcessMessage(
                        m_session,
                        new DSPExMessage(
                           transactionId,
                           m_assignedFrame,
                           8,
                           (int)(frameSize - 8)
                        )
                     );
                  }
               }
            } // using
            m_onFrameProcessed(this);
         } // while
      }

      /// <summary>
      /// Assigns the given frame content to this frame processor.
      /// </summary>
      internal void AssignFrame(byte[] frame)
      {
         m_assignedFrame = frame;
         m_synchronization.Set();
      }

      public byte[] ReleaseAssignedFrame()
      {
         var temp = m_assignedFrame;
         m_assignedFrame = null;
         return temp;
      }
   }
}