using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using ItzWarty;

namespace Dargon.Transport
{
   /// <summary>
   /// Represents a session between one DSPEx node and another DSPEx node.
   /// </summary>
   public unsafe partial class DSPExNodeSession : IDSPExSession
   {
      private readonly DSPExNode m_node;
      private readonly DSPExNodeRole m_localRole;

      public bool IsAlive { get { return m_isAlive; } set { m_isAlive = value && m_isAlive; } }
      private bool m_isAlive = true;

      // Instruction set assigned to this specific DSPEx instance.  
      // If a handler isn't found here, then a lookup occurs at the parent node
      private List<IDSPExInstructionSet> m_instructionSets = new List<IDSPExInstructionSet>();

      // Note: UIDSets are thread safe
      private readonly UniqueIdentificationSet m_locallyInitiatedUidSet;
      private readonly UniqueIdentificationSet m_remotelyInitiatedUidSet = new UniqueIdentificationSet(false);

      private readonly Dictionary<uint, LocallyInitializedTransactionHandler> m_liTransactions = new Dictionary<uint, LocallyInitializedTransactionHandler>();
      private readonly object m_liTransactionsLock = new object();

      private readonly Dictionary<uint, RemotelyInitializedTransactionHandler> m_riTransactions = new Dictionary<uint, RemotelyInitializedTransactionHandler>();
      private readonly object m_riTransactionsLock = new object();
      
      internal DSPExNodeSession(DSPExNode node, Stream connection, DSPExNodeRole localRole)
      {
         Trace.Assert(localRole.HasFlag(DSPExNodeRole.Client) != localRole.HasFlag(DSPExNodeRole.Server));

         m_node = node;
         m_connection = connection;
         m_localRole = localRole;

         if (m_localRole == DSPExNodeRole.Client)
            m_locallyInitiatedUidSet = new UniqueIdentificationSet(kClientLitIdLow, kClientLitIdHigh);
         else // Server
            m_locallyInitiatedUidSet = new UniqueIdentificationSet(kServerLitIdLow, kServerLitIdHigh);

         // - Setup Frame Processing --------------------------------------------------------------
         // : Setup Reader/Writer Workers :
         m_reader = new BinaryReader(m_connection, Encoding.Default, true);
         m_frameReaderThread = new Thread(FrameReaderThreadStart);
         m_frameReaderThread.Start();

         m_writer = new BinaryWriter(m_connection, Encoding.Default, true);
         m_frameWriterThread = new Thread(FrameWriterThreadStart);
         m_frameWriterThread.Start();

         // If we're the client, Send DSP_EX_INIT to elevate from DSP to DSPEx
         // If we're the server, the DSP_EX_INIT opcode will be read by the frame reader thread.
         if (m_localRole == DSPExNodeRole.Client)
         {
            m_writer.Write((byte)DSP.DSP_EX_INIT);
            m_dspExElevated = true;
         }

         // : Setup Frame Processors :
         for (var i = 0; i < 2; i++)
            AddFrameProcessor();
      }

      // - m_locallyInitiatedUidSet Utility Methods -----------------------------------------------
      public uint TakeLocallyInitializedTransactionId()
      {
         return m_locallyInitiatedUidSet.TakeUniqueID(); // thread safe
      }

      // - Frame Processor Utility Methods --------------------------------------------------------
      public LocallyInitializedTransactionHandler GetLocallyInitializedTransactionHandler(uint transactionId)
      {
         LocallyInitializedTransactionHandler handler;
         lock (m_liTransactionsLock)
            m_liTransactions.TryGetValue(transactionId, out handler);
         return handler;
      }

      public RemotelyInitializedTransactionHandler GetRemotelyInitializedTransactionHandler(
         uint transactionId, 
         byte opcode)
      {
         RemotelyInitializedTransactionHandler handler;
         lock (m_riTransactionsLock)
            m_riTransactions.TryGetValue(transactionId, out handler);
         return handler;
      }

      // - Utility Methods ------------------------------------------------------------------------
      internal DSPExNodeRole GetTransactionOrigin(UInt32 transactionId) 
      {
         if ((transactionId >> 31) == 0x00) // low TID = client
            return DSPExNodeRole.Client;
         else
            return DSPExNodeRole.Server;
      }

      internal bool IsLocallyInitializedTransaction(UInt32 transactionId) 
      {
         return GetTransactionOrigin(transactionId) == m_localRole;
      }

      // - IDSPExSession Implementation -----------------------------------------------------------
      /// <summary>
      /// Adds a DSPEx Instruction set for handling remotely-initiated transactions.
      /// </summary>
      /// <param name="instructionSet"></param>
      public void AddInstructionSet(IDSPExInstructionSet instructionSet)
      {
         m_instructionSets.Add(instructionSet);
      }

      public void RegisterAndInitializeLITransactionHandler(LocallyInitializedTransactionHandler th)
      {
         lock (m_liTransactionsLock)
            m_liTransactions.Add(th.TransactionId, th);

         th.InitializeInteraction(this);
      }

      public void DeregisterLITransactionHandler(LocallyInitializedTransactionHandler th)
      {
         lock (m_liTransactionsLock)
         {
            m_locallyInitiatedUidSet.GiveUniqueID(th.TransactionId);
            m_liTransactions.Remove(th.TransactionId);
         }
      }

      public RemotelyInitializedTransactionHandler CreateAndRegisterRITransactionHandler(
         uint transactionId, 
         byte opcode)
      {

         RemotelyInitializedTransactionHandler riTh = null;
         for (int i = 0; i < m_instructionSets.Count && riTh == null; i++)
         {
            var instructionSet = m_instructionSets[i];
            var handlerType = instructionSet.GetRemotelyInitializedTransactionHandlerType(opcode);
            if (handlerType != null)
            {
               if (!instructionSet.UseConstructionContext)
               {
                  riTh = (RemotelyInitializedTransactionHandler)Activator.CreateInstance(
                     handlerType,
                     transactionId
                  );
               }
               else
               {
                  riTh = (RemotelyInitializedTransactionHandler)Activator.CreateInstance(
                     handlerType,
                     transactionId,
                     instructionSet.ConstructionContext
                  );
               }
            }
         }

         if (riTh == null)
         {
            var handlerType = m_node.GetRITOpcodeHandlerType(opcode);
            if (handlerType != null)
            {
               riTh = (RemotelyInitializedTransactionHandler)Activator.CreateInstance(
                  handlerType,
                  transactionId
               );
            }
         }

         if (riTh == null)
            throw new KeyNotFoundException("No instruction set supported opcode " + opcode);

         // Register the RIT Handler
         lock (m_riTransactionsLock)
            m_riTransactions.Add(riTh.TransactionID, riTh);
         return riTh;
      }

      public void DeregisterRITransactionHandler(RemotelyInitializedTransactionHandler handler)
      {
         lock (m_riTransactionsLock)
            m_riTransactions.Remove(handler.TransactionID);
      }

      // - SendMessage ----------------------------------------------------------------------------
      // Messages are copied to a frame buffer and then queued to be sent.
      [DllImport("msvcrt.dll", SetLastError = false, CallingConvention = CallingConvention.Cdecl)]
      static extern IntPtr memcpy(IntPtr dest, IntPtr src, int count);

      public void SendMessage(DSPExInitialMessage message)
      {
         // Ensure that we aren't going to run over memory
         if (message.DataBuffer.Length < message.DataOffset + message.DataLength)
            throw new ArgumentException("DataOffset + DataLength > Message Buffer size");
         if (message.DataOffset < 0 || message.DataLength < 0)
            throw new ArgumentException("Either DataOffset or DataLength was negative");
         if (message.DataLength + 4 + 4 + 1 > DSPConstants.kMaxMessageSize)
            throw new ArgumentException("Frame (header + payload) was too big (> max size)");

         // Take frame buffer and copy contents to it
         int frameSize = 4 + 4 + 1 + message.DataLength;
         var frameBuffer = TakeFrameBuffer(frameSize);
         fixed (byte* pFrameBuffer = frameBuffer)
         fixed (byte* pMessageBuffer = message.DataBuffer)
         {
            *(uint*)(pFrameBuffer + 0) = (uint)frameSize;
            *(uint*)(pFrameBuffer + 4) = (uint)message.TransactionId;
            *(byte*)(pFrameBuffer + 8) = (byte)message.Opcode;

            byte* pMessageContent = pMessageBuffer + message.DataOffset;
            byte* pFrameBufferContent = pFrameBuffer + 9;

            memcpy(new IntPtr(pFrameBufferContent), new IntPtr(pMessageContent), message.DataLength);
         }
         m_frameBuffersToSend.Add(frameBuffer);
      }

      public void SendMessage(DSPExMessage message)
      {
         // Ensure that we aren't going to run over memory
         if (message.DataBuffer.Length < message.DataOffset + message.DataLength)
            throw new ArgumentException("DataOffset + DataLength > Message Buffer size");
         if (message.DataOffset < 0 || message.DataLength < 0)
            throw new ArgumentException("Either DataOffset or DataLength was negative");
         if (message.DataLength + 4 + 4 > DSPConstants.kMaxMessageSize)
            throw new ArgumentException("Frame (header + payload) was too big (> max size)");

         // Take frame buffer and copy contents to it
         int frameSize = 4 + 4 + message.DataLength;
         var frameBuffer = TakeFrameBuffer(frameSize);
         fixed (byte* pFrameBuffer = frameBuffer)
         fixed (byte* pMessageBuffer = message.DataBuffer)
         {
            *(uint*)(pFrameBuffer + 0) = (uint)frameSize;
            *(uint*)(pFrameBuffer + 4) = (uint)message.TransactionId;

            byte* pMessageContent = pMessageBuffer + message.DataOffset;
            byte* pFrameBufferContent = pFrameBuffer + 8;

            memcpy(new IntPtr(pFrameBufferContent), new IntPtr(pMessageContent), message.DataLength);
         }
         m_frameBuffersToSend.Add(frameBuffer);
      }
   }
}
