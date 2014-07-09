using ItzWarty;

namespace Dargon.Transport.ClientImpl
{
   public class GetVersionLith : LocallyInitializedTransactionHandler
   {
      /// <summary>
      /// The response data sent to us by the remote endpoint.
      /// This should equal {major major major, major major, major, minor, versionFlag (a/b/n/ )}
      /// </summary>
      public byte[] ResponseData { get; private set; }

      /// <summary>
      /// Initializes a new instance of a DSPEx Locally Initialized Get Dargon Version handler.  
      /// </summary>
      /// <param name="transactionId">
      /// The id assigned to this DSPEx transaction
      /// </param>
      public GetVersionLith(uint transactionId)
         : base(transactionId)
      {
      }

      /// <summary>
      /// Initializes our Locally Initialized transaction handler.
      /// </summary>
      /// <param name="session"></param>
      public override void InitializeInteraction(IDSPExSession session)
      {
         session.SendMessage(
            new TransactionInitialMessage(
               TransactionId,
               (byte)DTP.GET_VERSION
            )
         );
      }

      /// <summary>
      /// Processes the response to our LIT Handler.  The data segment of the message should be
      /// identical to our request data, as this is an echo implementation.
      /// </summary>
      /// <param name="session"></param>
      /// <param name="message"></param>
      public override void ProcessMessage(IDSPExSession session, TransactionMessage message)
      {
         ResponseData = message.DataBuffer.SubArray(message.DataOffset, message.DataLength);
         session.DeregisterLITransactionHandler(this);
         OnCompletion();
      }
   }
}
