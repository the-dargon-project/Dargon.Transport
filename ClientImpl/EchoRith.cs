using System;

using Logger = Dargon.Transport.__DummyLoggerThisIsHorrible;

namespace Dargon.Transport.ClientImpl
{
   public class EchoRith : RemotelyInitializedTransactionHandler
   {
      /// <summary>
      /// Initializes a new instance of a DSPExRITEchoHandler.
      /// </summary>
      /// <param name="transactionId">
      /// The transaction id of new handler.
      /// </param>
      public EchoRith(uint transactionId)
         : base(transactionId)
      {
      }

      /// <summary>
      /// Processes the initial echo message
      /// </summary>
      /// <param name="session"></param>
      /// <param name="message"></param>
      public override void ProcessInitialMessage(IDSPExSession session, TransactionInitialMessage message)
      {
         // Echo the message's data
         Logger.L(LoggerLevel.Info, "Sending echo response of length " + message.DataLength);
         var response = new TransactionMessage(TransactionID, message.DataBuffer, message.DataOffset, message.DataLength);
         session.SendMessage(response);
         session.DeregisterRITransactionHandler(this);
      }

      /// <summary>
      /// DSPExRITEchoHandler shouldn't ever get here.
      /// </summary>
      /// <param name="session"></param>
      /// <param name="message"></param>
      public override void ProcessMessage(IDSPExSession session, TransactionMessage message)
      {
         //Just eat the packet [do nothing]
      }
   }
}
