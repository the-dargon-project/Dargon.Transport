using System.Threading;

namespace Dargon.Transport
{
   /// <summary>
   /// Represents a client-initiated interaction which is initialized locally and sent to a remote
   /// DSP implementation.
   /// </summary>
   public abstract class DSPExLITransactionHandler
   {
      /// <summary>
      /// The transaction ID associated with this locally initialized transaction handler
      /// </summary>
      public uint TransactionId { get; private set; }

      /// <summary>
      /// When our transaction completes, this countdown event is signalled so that any threads
      /// awaiting this DSPExTransactionHandler's results can continue onwards, with the transaction
      /// results available to them.
      /// </summary>
      public CountdownEvent CompletionCountdownEvent { get { return m_completionCountdownEvent; } }
      private CountdownEvent m_completionCountdownEvent = new CountdownEvent(1);

      /// <summary>
      /// Initializes a new instance of a DSP Ex Locally Initiated transaction handler, assigning
      /// it the given transaction id.
      /// </summary>
      /// <param name="transactionId">
      /// As we are a locally initialized transaction handler, you will likely have to get the
      /// transaction ID from the DSPManager.
      /// </param>
      protected DSPExLITransactionHandler(uint transactionId)
      {
         this.TransactionId = transactionId;
      }

      /// <summary>
      /// Creates the initial message which begins our interaction.
      /// </summary>
      public abstract void InitializeInteraction(IDSPExSession session);

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

      /// <summary>
      /// This method should be invoked when the transaction ends.
      /// </summary>
      protected void OnCompletion()
      {
         m_completionCountdownEvent.Signal();
      }
   }
}
