﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using ItzWarty;
using ItzWarty.Collections;

namespace Dargon.Transport
{
   /// <summary>
   /// Represents a session between one DSPEx node and another DSPEx node.
   /// </summary>
   public unsafe partial class DtpNodeSession : IDSPExSession
   {
      private readonly DtpNode m_node;
      private readonly NodeRole m_localRole;

      public bool IsAlive { get { return m_isAlive; } set { m_isAlive = value && m_isAlive; } }
      private bool m_isAlive = true;
      private readonly CancellationTokenSource aliveCancellationTokenSource = new CancellationTokenSource();
      private readonly CancellationToken aliveCancellationToken;

      // Instruction set assigned to this specific DSPEx instance.  
      // If a handler isn't found here, then a lookup occurs at the parent node
      private List<IInstructionSet> m_instructionSets = new List<IInstructionSet>();

      // Note: UIDSets are thread safe
      private readonly UniqueIdentificationSet m_locallyInitiatedUidSet;
      private readonly UniqueIdentificationSet m_remotelyInitiatedUidSet = new UniqueIdentificationSet(false); // TODO: Not really necessary

      private readonly System.Collections.Concurrent.ConcurrentDictionary<uint, LocallyInitializedTransactionHandler> m_liTransactions = new System.Collections.Concurrent.ConcurrentDictionary<uint, LocallyInitializedTransactionHandler>();
      private readonly System.Collections.Concurrent.ConcurrentDictionary<uint, RemotelyInitializedTransactionHandler> m_riTransactions = new System.Collections.Concurrent.ConcurrentDictionary<uint, RemotelyInitializedTransactionHandler>();

      public event ClientDisconnectedEventHandler Disconnected;

      internal DtpNodeSession(DtpNode node, Stream connection, NodeRole localRole)
      {
         Trace.Assert(localRole.HasFlag(NodeRole.Client) != localRole.HasFlag(NodeRole.Server));

         aliveCancellationToken = aliveCancellationTokenSource.Token;

         m_node = node;
         m_connection = connection;
         m_localRole = localRole;

         if (m_localRole == NodeRole.Client)
            m_locallyInitiatedUidSet = new UniqueIdentificationSet(kClientLitIdLow, kClientLitIdHigh);
         else // Server
            m_locallyInitiatedUidSet = new UniqueIdentificationSet(kServerLitIdLow, kServerLitIdHigh);

         // - Setup Frame Processing --------------------------------------------------------------
         // : Setup Frame Processors :
         InitializeFrameProcessors(Environment.ProcessorCount);

         // : Setup Reader/Writer Workers :
         m_reader = new BinaryReader(m_connection, Encoding.Default, true);
         m_frameReaderThread = new Thread(FrameReaderThreadStart) { IsBackground = true };
         m_frameReaderThread.Start();

         m_writer = new BinaryWriter(m_connection, Encoding.Default, true);
         m_frameWriterThread = new Thread(FrameWriterThreadStart) { IsBackground = true };
         m_frameWriterThread.Start();

         // If we're the client, Send DSP_EX_INIT to elevate from DSP to DSPEx
         // If we're the server, the DSP_EX_INIT opcode will be read by the frame reader thread.
         if (m_localRole == NodeRole.Client)
         {
            m_writer.Write((byte)DTP.DSPEX_INIT);
         }
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
         m_liTransactions.TryGetValue(transactionId, out handler);
         return handler;
      }

      public RemotelyInitializedTransactionHandler GetRemotelyInitializedTransactionHandler(
         uint transactionId, 
         byte opcode)
      {
         RemotelyInitializedTransactionHandler handler;
         m_riTransactions.TryGetValue(transactionId, out handler);
         return handler;
      }

      // - Utility Methods ------------------------------------------------------------------------
      internal NodeRole GetTransactionOrigin(UInt32 transactionId) 
      {
         if ((transactionId >> 31) == 0x00) // low TID = client
            return NodeRole.Client;
         else
            return NodeRole.Server;
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
      public void AddInstructionSet(IInstructionSet instructionSet)
      {
         m_instructionSets.Add(instructionSet);
      }

      public void RegisterAndInitializeLITransactionHandler(LocallyInitializedTransactionHandler th)
      {
         m_liTransactions.AddOrUpdate(th.TransactionId, th, (key, existingValue) => { throw new InvalidOperationException("TransactionID already existed!"); });
         th.InitializeInteraction(this);
      }

      public void DeregisterLITransactionHandler(LocallyInitializedTransactionHandler th)
      {
         LocallyInitializedTransactionHandler removedLith = null;
         while(removedLith == null)
            m_liTransactions.TryRemove(th.TransactionId, out removedLith);
         Trace.Assert(removedLith == th);

         Thread.MemoryBarrier();

         m_locallyInitiatedUidSet.GiveUniqueID(th.TransactionId); // Thread Safe, goes after removal
      }

      public RemotelyInitializedTransactionHandler CreateAndRegisterRITransactionHandler(
         uint transactionId, 
         byte opcode)
      {

         RemotelyInitializedTransactionHandler riTh = null;
         for (int i = 0; i < m_instructionSets.Count && riTh == null; i++)
         {
            var instructionSet = m_instructionSets[i];

            instructionSet.TryCreateRemotelyInitializedTransactionHandler(opcode, transactionId, out riTh);
         }

         if (riTh == null) {
            m_node.TryCreateRemotelyInitializedTransactionHandler(opcode, transactionId, out riTh);
         }

         if (riTh == null)
            throw new KeyNotFoundException("No instruction set supported opcode " + opcode);

         // Register the RIT Handler
         m_riTransactions.AddOrUpdate(riTh.TransactionID, riTh, (key, existingValue) => { throw new InvalidOperationException("RITH TID already existed in concurrent dict!"); });
         return riTh;
      }

      public void DeregisterRITransactionHandler(RemotelyInitializedTransactionHandler handler)
      {
         RemotelyInitializedTransactionHandler removedRith = null;
         while (removedRith == null)
            m_riTransactions.TryRemove(handler.TransactionID, out removedRith);
         Trace.Assert(handler == removedRith);
      }

      // - SendMessage ----------------------------------------------------------------------------
      // Messages are copied to a frame buffer and then queued to be sent.
      [DllImport("msvcrt.dll", SetLastError = false, CallingConvention = CallingConvention.Cdecl)]
      static extern IntPtr memcpy(IntPtr dest, IntPtr src, int count);

      public void SendMessage(TransactionInitialMessage message)
      {
         // Ensure that we aren't going to run over memory
         if (message.DataBuffer.Length < message.DataOffset + message.DataLength)
            throw new ArgumentException("DataOffset + DataLength > Message Buffer size");
         if (message.DataOffset < 0 || message.DataLength < 0)
            throw new ArgumentException("Either DataOffset or DataLength was negative");
         if (message.DataLength + 4 + 4 + 1 > DTPConstants.kMaxMessageSize)
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

      public void SendMessage(TransactionMessage message)
      {
         // Ensure that we aren't going to run over memory
         if (message.DataBuffer.Length < message.DataOffset + message.DataLength)
            throw new ArgumentException("DataOffset + DataLength > Message Buffer size");
         if (message.DataOffset < 0 || message.DataLength < 0)
            throw new ArgumentException("Either DataOffset or DataLength was negative");
//         if (message.DataLength + 4 + 4 > DTPConstants.kMaxMessageSize)
//            throw new ArgumentException("Frame (header + payload) was too big (> max size)");

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

      protected virtual void OnDisconnected(ClientDisconnectedEventArgs e)
      {
         ClientDisconnectedEventHandler handler = Disconnected;
         if (handler != null) handler(this, e);
      }

      private void Shutdown() { m_isAlive = false; aliveCancellationTokenSource.Cancel(); }

      public void Dispose() {
         Shutdown();
      }
   }
}
