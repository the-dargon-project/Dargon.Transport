namespace Dargon.IO.DSP
{
   /// <summary>
   /// Represents a client-initiated interaction which is initialized remotely and sent to the
   /// local DSP implementation.
   /// </summary>
   public abstract class DSPExRITransactionHandler
   {
      /// <summary>
      /// The Transaction ID associated with this transaction handler.
      /// </summary>
      public uint TransactionID { get; private set; }

      /// <summary>
      /// Initializes a new instance of a Remotely Initialized Transaction Handler with the given
      /// transactionId.
      /// </summary>
      /// <param name="transactionId"></param>
      protected DSPExRITransactionHandler(uint transactionId)
      {
         this.TransactionID = transactionId;
      }

      /// <summary>
      /// Handles the initial message (server-sent) which begins our transaction.
      /// </summary>
      public abstract void ProcessInitialMessage(IDSPExSession session, DSPExInitialMessage message);

      /// <summary>
      /// Processes a message recieved from the remote endpoint
      /// </summary>
      /// <param name="session">
      /// DSPEx session object, which permits us to send messages.
      /// </param>
      /// <param name="message">
      /// The recieved DSPEx message which we are to process.
      /// </param>
      public abstract void ProcessMessage(IDSPExSession session, DSPExMessage message);
   }
}
