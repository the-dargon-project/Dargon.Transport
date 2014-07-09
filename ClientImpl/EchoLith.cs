using System.Runtime.InteropServices;

using Logger = Dargon.Transport.__DummyLoggerThisIsHorrible;

namespace Dargon.Transport.ClientImpl
{
   public class EchoLith : LocallyInitializedTransactionHandler
   {
      /// <summary>
      /// The request data which we originally send to the remote endpoint.
      /// If the protocol is implemented properly, this will equal ResponseData when the
      /// transaction ends.
      /// </summary>
      public byte[] RequestData { get; private set; }

      /// <summary>
      /// Whether or not the response data matched the request data.
      /// </summary>
      public bool ResponseDataMatched { get; private set; }

      /// <summary>
      /// Initializes a new instance of a DSPEx Locally Initialized echo handler.  
      /// </summary>
      /// <param name="transactionId">
      /// The id assigned to this DSPEx transaction
      /// </param>
      /// <param name="data">
      /// The data which we will send to the server in hopes of getting an identical response
      /// </param>
      public EchoLith(uint transactionId, byte[] data)
         : base(transactionId)
      {
         RequestData = data;
      }

      /// <summary>
      /// Initializes our Locally Initialized transaction handler, sending over the initial echo
      /// request.
      /// </summary>
      /// <param name="session"></param>
      public override void InitializeInteraction(IDSPExSession session)
      {
         session.SendMessage(
            new TransactionInitialMessage(
               TransactionId,
               (byte)DTP.ECHO,
               RequestData,
               0,
               RequestData.Length
            )
         );
         Logger.L(LoggerLevel.Info, "Sent Echo");
      }

      [DllImport("msvcrt.dll")]
      private unsafe static extern int memcmp(void* b1, void* b2, long count);

      /// <summary>
      /// Processes the response to our LIT Handler.  The data segment of the message should be
      /// identical to our request data, as this is an echo implementation.
      /// </summary>
      /// <param name="session"></param>
      /// <param name="message"></param>
      public unsafe override void ProcessMessage(IDSPExSession session, TransactionMessage message)
      {
         bool match = RequestData.Length == message.DataLength;

         //if (match)
         //{
         //   fixed (byte* pRequestBuffer = RequestData)
         //   fixed (byte* pResponseBuffer = message.DataBuffer)
         //   {
         //      byte* pResponseBuffer_ = pResponseBuffer + message.DataOffset;
         //      match = memcmp(pRequestBuffer, pResponseBuffer_, RequestData.Length) == 0;
         //   }
         //}

         ResponseDataMatched = match;
         session.DeregisterLITransactionHandler(this);
         OnCompletion();

         Logger.L(LoggerLevel.Info, "Echo response matched? " + match);
      }
   }
}
