using System;

namespace Dargon.Transport
{
   public interface IDSPExSession : IDisposable
   {
      event ClientDisconnectedEventHandler Disconnected;

      uint TakeLocallyInitializedTransactionId();

      /// <summary>
      /// Adds a DSPEx Instruction set for handling remotely-initiated transactions.
      /// </summary>
      /// <param name="instructionSet"></param>
      void AddInstructionSet(IInstructionSet instructionSet);

      /// <summary>
      /// Registers a locally initialized transaction handler so that future messages can be 
      /// routed to it.  This method respects the Transaction Handler's TransactionID property; 
      /// that value is not mutated by this method.
      /// </summary>
      /// <param name="th">
      /// The transaction handler which we are registering.
      /// </param>
      void RegisterAndInitializeLITransactionHandler(LocallyInitializedTransactionHandler th);

      /// <summary>
      /// Deregisters the given locally initialized transaction handler, freeing its transaction id.
      /// This method is called assuming that the transaction has reached a state where both DSP
      /// endpoints are aware of the transaction ending.  If such is not a case, this method call
      /// will result in a memory leak which will last on the other endpoint until the DSP 
      /// connection is closed.
      /// </summary>
      /// <param name="th"></param>
      void DeregisterLITransactionHandler(LocallyInitializedTransactionHandler th);

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
      RemotelyInitializedTransactionHandler CreateAndRegisterRITransactionHandler(uint transactionId, byte opcode);

      /// <summary>
      /// Deregisters the remotely initialized transaction's handler, freeing its transaction id.
      /// This method is called assuming that the transaction has reached a state where both DSP
      /// endpoints are aware of the transaction ending.  If such is not a case, this method call
      /// will result in a memory leak which will last on the other endpoint until the DSP 
      /// connection is closed.
      /// </summary>
      /// <param name="handler"></param>
      void DeregisterRITransactionHandler(RemotelyInitializedTransactionHandler handler);

      /// <summary>
      /// Sends a DSPEX Initial message to the remote endpoint.  Before this method is called, 
      /// DSPEx's RegisterTransaction() method must be called to modify the associated transaction's
      /// transactionId field.
      /// </summary>
      /// <param name="message">
      /// The initial message which we are sending.
      /// </param>
      void SendMessage(TransactionInitialMessage message);

      /// <summary>
      /// Sends a DSPEx message.  This method must be called after DSPExInitialMessage is sent once.
      /// If this is the last DSPEx message to be sent, DeregisterTransaction() must be called by
      /// the associated transaction to free its transactionId.
      /// </summary>
      /// <param name="message">
      /// The message which we are sending.
      /// </param>
      void SendMessage(TransactionMessage message);
   }
}
