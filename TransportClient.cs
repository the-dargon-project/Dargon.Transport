using System;
using System.Collections.Generic;
using System.IO;
using System.ServiceModel.Channels;
using System.Text;
using Dargon.Transport.ClientImpl;
using ItzWarty;

using Logger = Dargon.Transport.__DummyLoggerThisIsHorrible;

namespace Dargon.Transport
{
   public class TransportClient : IDSPExSession
   {
      //-------------------------------------------------------------------------------------------
      // Static Implementation
      //-------------------------------------------------------------------------------------------
      /// <summary>
      /// Opcode handlers for DSPEx remotely-initialized transactions
      /// </summary>
      private static readonly Dictionary<byte, Type> kDSPExOpcodeHandlers = new Dictionary<byte, Type>();

      /// <summary>
      /// The static initializer for the DSPEx Client registers any Remotely Initialized 
      /// Transaction handlers.  
      /// </summary>
      static TransportClient()
      {
         //----------------------------------------------------------------------------------------
         // Register DSP Extended Opcode Handlers
         //----------------------------------------------------------------------------------------
         //kDSPExOpcodeHandlers[(int)DSPEx.DSP_EX_C2S_ECHO] = typeof(DSPExRITransactionHandler);
      }

      /// <summary>
      /// Whether or not our DSPEx client connection is connected
      /// </summary>
      public bool Terminated { get; private set; }

      /// <summary>
      /// The IO lock ensures that only one message is being written to our network stream
      /// at a given time.
      /// </summary>
      private object m_ioLock = new object();

      /// <summary>
      /// The locally initialized transaction lock ensures that the locally initializedtransaction
      /// list is only mutated or accessed by one thread at a time.
      /// </summary>
      private object m_locallyInitializedTransactionLock = new object();

      /// <summary>
      /// The remotely initialized transaction lock ensures that the remotely initializedtransaction
      /// list is only mutated or accessed by one thread at a time.
      /// </summary>
      private object m_remotelyInitializedTransactionLock = new object();

      /// <summary>
      /// This provides an initially filled Unique Identification Set for interactions IDing.
      /// We take UIDs from this set to label our locally initiated interactions.  Initially,
      /// the set is filled. to contain all possible values.  
      /// 
      /// As this is a server-side implementation, when we take values from this set we
      /// set their HIGH bit; The set is initially filled to the range [0x00000000, 0x7FFFFFFF]
      /// low: 0b00000000 00000000 00000000 00000000 high: 0b01111111 11111111 11111111 11111111
      /// </summary>
      private readonly UniqueIdentificationSet m_locallyInitiatedUIDSet = new UniqueIdentificationSet(0x00000000U, 0x7FFFFFFFU);

      /// <summary>
      /// This provides an initially filled Unique Identification Set for interactions IDing.
      /// We give UIDs from this set to label remotely initiated interactions.  Initially,
      /// the set is empty.  Technically, the valid range of the set is [0x80000000, 0xFFFFFFFF]
      /// </summary>
      private readonly UniqueIdentificationSet m_remotelyInitiatedUIDSet = new UniqueIdentificationSet(0x80000000U, 0xFFFFFFFFU);

      /// <summary>
      /// Pairs locally initialized transactions with their associated transaction handlers.  
      /// </summary>
      private readonly Dictionary<uint, LocallyInitializedTransactionHandler> m_locallyInitializedTransactions = new Dictionary<uint, LocallyInitializedTransactionHandler>();

      /// <summary>
      /// Pairs remotely initialized transactions with their associated transaction handlers.  
      /// </summary>
      private readonly Dictionary<uint, RemotelyInitializedTransactionHandler> m_remotelyInitializedTransactions = new Dictionary<uint, RemotelyInitializedTransactionHandler>();

      /// <summary>
      /// The DSPEx Frame Transmitter which we are using to send frames to/from us and the remote
      /// endpoint.
      /// </summary>
      private readonly IDSPExFrameTransmitter m_frameTransmitter;

      /// <summary>
      /// Whether or not debug logging is enabled.
      /// </summary>
      private readonly bool kDebugEnabled = false;

      /// <summary>
      /// The output buffer pool provides a pool of buffers.  We use this for message transmitting
      /// so that we don't spend a lot of time allocating and deallocating blocks of memory.
      /// </summary>
      private readonly BufferManager m_outputBufferPool;

      /// <summary>
      /// Initializes a new instance of a DSPEx Client which connects to the server at the
      /// given hostname and port.
      /// </summary>
      /// <param name="host">The hostname of the server to connect to</param>
      /// <param name="port">The port of the server to connect to</param>
      public TransportClient(string host, int port)
      {
         Terminated = false;
         m_outputBufferPool = BufferManager.CreateBufferManager(100, DTPConstants.kMaxMessageSize);
         m_frameTransmitter = new DSPExTCPFrameTransmitter(host, port);

         // Begin our Asynchronous IO.
         m_frameTransmitter.BeginReceivingMessageFrames(RunDSPExIteration);
      }

      /// <summary>
      /// Initializes a new instance of a DSPEx Client which uses the given named pipe.
      /// </summary>
      /// <param name="pipeName">
      /// The name of the named pipe which we are connecting to.
      /// </param>
      /// <param name="dargon">
      /// (Optional) The Dargon Service, which is responsible for supporting game-specific opcodes
      /// in the DSPEx.DSP_EX_GAME_OP range.
      /// </param>
      public TransportClient(string pipeName)
      {
         Terminated = false;
         m_outputBufferPool = BufferManager.CreateBufferManager(100, DTPConstants.kMaxMessageSize);
         m_frameTransmitter = new DSPExNamedPipeFrameTransmitter(pipeName);

         // Begin our Asynchronous IO.
         m_frameTransmitter.BeginReceivingMessageFrames(RunDSPExIteration);
      }

      /// <summary>
      /// Runs an iteration of the DSPEx main loop
      /// </summary>
      /// <param name="messageBuffer">
      /// The buffer containing our message
      /// </param>
      private void RunDSPExIteration(byte[] messageBuffer)
      {
         uint blockLength = BitConverter.ToUInt32(messageBuffer, 0);
         uint transactionId = BitConverter.ToUInt32(messageBuffer, 4);
         int remainingByteCount = (int)blockLength - 8; //2 * sizeof(uint32)
         //Console.WriteLine("Got DSP Message of Length {0} TransactionID {1}", blockLength, transactionId);

         //-------------------------------------------------------------------------------------
         // See if transactionId is in our transaction set.  If not, we're initiating a new 
         // transaction and have to read in the transaction's command opcode.
         //-------------------------------------------------------------------------------------
         bool isLocallyInitializedTransaction = (transactionId >> 31) != 0x01;
         if (isLocallyInitializedTransaction)
         {
            // TODO: Use TryGetValue, this is a race condition.
            if (m_locallyInitializedTransactions.ContainsKey(transactionId))
            {
               //Read the data of the message (No opcode at first byte)
               var message = new DSPExMessage(transactionId, messageBuffer, 8, remainingByteCount);
               DumpToConsole(message);

               LocallyInitializedTransactionHandler transaction;
               lock(m_locallyInitializedTransactionLock)
                  transaction = m_locallyInitializedTransactions[transactionId];
               transaction.ProcessMessage(this, message);
            }
            else
            {
               //Eat the data and log an error.
               Logger.L(LoggerLevel.Error, "Unrecognized transaction " + transactionId + "... eating message frame and continuing onwards");
            }
         }
         else
         {
            if (m_remotelyInitializedTransactions.ContainsKey(transactionId))
            {
               //Read the data of the message (No opcode at first byte)
               var message = new DSPExMessage(transactionId, messageBuffer, 8, remainingByteCount);
               DumpToConsole(message);

               RemotelyInitializedTransactionHandler transaction;
               lock(m_remotelyInitializedTransactionLock)
                  transaction = m_remotelyInitializedTransactions[transactionId];
               transaction.ProcessMessage(this, message);
            }
            else
            {
               //We're starting a new transaction.  Read the opcode and then the data block.
               byte opcode = messageBuffer[8]; remainingByteCount--;
               DSPExInitialMessage message = new DSPExInitialMessage(transactionId, opcode, messageBuffer, (int)(blockLength - remainingByteCount), remainingByteCount);
               DumpToConsole(message);

               RemotelyInitializedTransactionHandler transaction = CreateAndRegisterRITransactionHandler(transactionId, opcode);
               transaction.ProcessInitialMessage(this, message);
            }
         }
      }

      /// <summary>
      /// Sends an echo message to the remote endpoint.  Then, blocks until the remote endpoint
      /// sends an appropriate response.
      /// </summary>
      /// <param name="data">The data to echo</param>
      /// <returns></returns>
      public bool Echo(byte[] data)
      {
         uint transactionId = m_locallyInitiatedUIDSet.TakeUniqueID();
         var handler = new EchoLith(transactionId, data);
         RegisterAndInitializeLITransactionHandler(handler);
         handler.CompletionCountdownEvent.Wait();
         return handler.ResponseDataMatched;
      }

      /// <summary>
      /// Gets the version of the Dargon Daemon which we are connected to.
      /// </summary>
      /// <returns>
      /// The version of the Dargon Daemon.  Throws if the response was invalid.
      /// </returns>
      public byte[] GetDargonVersion()
      {
         uint transactionId = m_locallyInitiatedUIDSet.TakeUniqueID();
         var handler = new GetVersionLith(transactionId);
         RegisterAndInitializeLITransactionHandler(handler);
         handler.CompletionCountdownEvent.Wait();

         if (handler.ResponseData.Length == 5)
            return handler.ResponseData;
         else
            throw new Exception("Expected Dargon Version of 5 bytes but got one of {0} bytes".F(handler.ResponseData.Length));
      }

      /// <summary>
      /// Creates a remotely initialized transaction handler for the given opcode
      /// </summary>
      /// <param name="transactionId">
      /// Unique identifier associated with the transaction.
      /// </param>
      /// <param name="opcode">
      /// The opcode associated with the given transaction
      /// </param>
      /// <returns>
      /// The transaction handler, or null if such a transaction handler doesn't exist
      /// </returns>
      public RemotelyInitializedTransactionHandler CreateAndRegisterRITransactionHandler(uint transactionId, byte opcode)
      {
         RemotelyInitializedTransactionHandler riTh = (RemotelyInitializedTransactionHandler)Activator.CreateInstance(
            kDSPExOpcodeHandlers[(byte)opcode],
            transactionId
         );
         lock (m_remotelyInitializedTransactionLock)
            m_remotelyInitializedTransactions.Add(transactionId, riTh);
         m_remotelyInitiatedUIDSet.GiveUniqueID(transactionId);
         return riTh;
      }

      /// <summary>
      /// Deregisters the remotely initialized transaction's handler, freeing its transaction id.
      /// This method is called assuming that the transaction has reached a state where both DSP
      /// endpoints are aware of the transaction ending.  If such is not a case, this method call
      /// will result in a memory leak which will last on the other endpoint until the DSP 
      /// connection is closed.
      /// </summary>
      /// <param name="handler"></param>
      public void DeregisterRITransactionHandler(RemotelyInitializedTransactionHandler handler)
      {
         lock(m_remotelyInitializedTransactionLock)
            m_remotelyInitializedTransactions.Remove(handler.TransactionID);
         m_remotelyInitiatedUIDSet.TakeUniqueID(handler.TransactionID);
      }

      public uint TakeLocallyInitializedTransactionId()
      {
         return m_locallyInitiatedUIDSet.TakeUniqueID();
      }

      /// <summary>
      /// Adds a DSPEx Instruction set for handling remotely-initiated transactions.
      /// </summary>
      /// <param name="instructionSet"></param>
      public void AddInstructionSet(IDSPExInstructionSet instructionSet)
      {
         throw new NotImplementedException("Use DSPExNode for this.");
      }

      /// <summary>
      /// Registers a locally initialized transaction handler so that future messages can be 
      /// routed to it.  This method respects the Transaction Handler's TransactionID property; 
      /// that value is not mutated by this method.
      /// </summary>
      /// <param name="th">
      /// The transaction handler which we are registering.
      /// </param>
      public void RegisterAndInitializeLITransactionHandler(LocallyInitializedTransactionHandler th)
      {
         lock(m_locallyInitializedTransactionLock)
            m_locallyInitializedTransactions.Add(th.TransactionId, th);
         th.InitializeInteraction(this);
      }

      /// <summary>
      /// Deregisters the given locally initialized transaction handler, freeing its transaction id.
      /// This method is called assuming that the transaction has reached a state where both DSP
      /// endpoints are aware of the transaction ending.  If such is not a case, this method call
      /// will result in a memory leak which will last on the other endpoint until the DSP 
      /// connection is closed.
      /// </summary>
      /// <param name="th"></param>
      public void DeregisterLITransactionHandler(LocallyInitializedTransactionHandler th)
      {
         lock (m_locallyInitializedTransactionLock)
            m_locallyInitializedTransactions.Remove(th.TransactionId);
         m_locallyInitiatedUIDSet.GiveUniqueID(th.TransactionId);
      }

      /// <summary>
      /// Sends the given DSPEx Initial Message.
      /// The message transactionId must have been registered as a locally initialized 
      /// transaction before this method call.
      /// </summary>
      /// <param name="message">
      /// The DSPEx Initial message which we are sending over to the remote endpoint.
      /// </param>
      public void SendMessage(DSPExInitialMessage message)
      {
         if (!m_locallyInitializedTransactions.ContainsKey(message.TransactionId))
         {
            throw new Exception("The transaction id was not registered with the DSP Manager!");
         }
         else
         {
            byte[] buffer = m_outputBufferPool.TakeBuffer(DTPConstants.kMaxMessageSize);
            using (MemoryStream ms = new MemoryStream(buffer) { Position = 0 })
            using (BinaryWriter br = new BinaryWriter(ms))
            {
               uint messageFrameSize = 4 + 4 + 1 + (uint)message.DataLength; //2uints, 1 byte opcode, data
               br.Write((uint)messageFrameSize);
               br.Write((uint)message.TransactionId);
               br.Write((byte)message.Opcode);
               br.Write((byte[])message.DataBuffer, message.DataOffset, message.DataLength);
               br.Flush();
               lock (m_ioLock)
               {
                  m_frameTransmitter.SendRawFrame(
                     buffer,
                     0,
                     (int)messageFrameSize,
                     () => m_outputBufferPool.ReturnBuffer(buffer));
               }
            }
         }
      }

      /// <summary>
      /// Sends the given DSPEx intermediate message
      /// The message transactionId must have been registered as a locally initialized
      /// transaction before this method call.
      /// </summary>
      /// <param name="message">
      /// The DSPEx intermediate message which we are sending over to the remote endpoint.
      /// </param>
      public void SendMessage(DSPExMessage message)
      {
         if (!m_locallyInitializedTransactions.ContainsKey(message.TransactionId) &&
             !m_remotelyInitializedTransactions.ContainsKey(message.TransactionId))
         {
            throw new Exception("The transaction id was not registered with the DSP Manager!");
         }
         else
         {
            MemoryStream ms = new MemoryStream();
            BinaryWriter br = new BinaryWriter(ms);
            uint messageFrameSize = 4 + 4 + (uint)message.DataBuffer.Length; //2uints, data
            br.Write((uint)messageFrameSize);
            br.Write((uint)message.TransactionId);
            br.Write((byte[])message.DataBuffer, message.DataOffset, message.DataLength);
            br.Flush();
            br.Close();
            lock (m_ioLock)
            {
               byte[] buffer = ms.ToArray();
               m_frameTransmitter.SendRawFrame(
                  buffer,
                  0,
                  buffer.Length,
                  () => {}
               );
            }
            ms.Close();
         }
      }

      /// <summary>
      /// Prints a dump of the given message to console
      /// </summary>
      /// <param name="message"></param>
      private void DumpToConsole(DSPExMessage message)
      {
         if (!kDebugEnabled) return;
         Console.WriteLine("Transaction ID: " + message.TransactionId);
         for (int i = 0; i < message.DataLength; i += 16)
         {
            StringBuilder sb = new StringBuilder();
            for (int offset = 0; offset < 16 && i + offset < message.DataLength; offset++)
            {
               sb.Append(message.DataBuffer[message.DataOffset + i + offset].ToString("X").PadLeft(2, '0'));
               if (offset % 2 == 1)
                  sb.Append(" ");
            }
            Console.WriteLine(sb.ToString());
         }
      }

      /// Prints a dump of the given message to console
      /// </summary>
      /// <param name="message"></param>
      private void DumpToConsole(DSPExInitialMessage message)
      {
         if (!kDebugEnabled) return;
         Console.WriteLine("Transaction ID: " + message.TransactionId + " opcode " + message.Opcode);
         for (int i = 0; i < message.DataLength; i += 16)
         {
            StringBuilder sb = new StringBuilder();
            for (int offset = 0; offset < 16 && i + offset < message.DataLength; offset++)
            {
               sb.Append(message.DataBuffer[message.DataOffset + i + offset].ToString("X").PadLeft(2, '0'));
               if (offset != 0 && offset % 2 == 0)
                  sb.Append(" ");
            }
            Console.WriteLine(sb.ToString());
         }
      }
   }
}
