using System;

namespace Dargon.Transport.ClientImpl
{
   public class DSPExRITEchoHandler : DSPExRITransactionHandler
   {
      /// <summary>
      /// Initializes a new instance of a DSPExRITEchoHandler.
      /// </summary>
      /// <param name="transactionId">
      /// The transaction id of new handler.
      /// </param>
      public DSPExRITEchoHandler(uint transactionId)
         : base(transactionId)
      {
      }

      /// <summary>
      /// Processes the initial echo message
      /// </summary>
      /// <param name="session"></param>
      /// <param name="message"></param>
      public override void ProcessInitialMessage(IDSPExSession session, DSPExInitialMessage message)
      {
         // Echo the message's data
         Console.WriteLine("Sending echo response of length " + message.DataLength);
         var response = new DSPExMessage(TransactionID, message.DataBuffer, message.DataOffset, message.DataLength);
         session.SendMessage(response);
         session.DeregisterRITransactionHandler(this);
      }

      /// <summary>
      /// DSPExRITEchoHandler shouldn't ever get here.
      /// </summary>
      /// <param name="session"></param>
      /// <param name="message"></param>
      public override void ProcessMessage(IDSPExSession session, DSPExMessage message)
      {
         //Just eat the packet [do nothing]
      }
   }
}
