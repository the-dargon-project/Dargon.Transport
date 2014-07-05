using System;
using System.IO;
using Dargon.Games;
using Dargon.IO.DSP.Results;

namespace Dargon.IO.DSP.ClientImpl
{
   public class DSPExLITGetResourceTreeRootInfoHandler : DSPExLITransactionHandler
   {
      /// <summary>
      /// The game whose resource tree UID we requested.
      /// </summary>
      public DargonGame RequestGame { get; private set; }

      /// <summary>
      /// The response data sent to us by the remote endpoint.
      /// </summary>
      public RootInfoResult Result { get; private set; }

      /// <summary>
      /// Initializes a new instance of a DSPEx Locally Initialized Get Resource Tree UID handler.
      /// </summary>
      /// <param name="transactionId">
      /// The id assigned to this DSPEx transaction
      /// </param>
      public DSPExLITGetResourceTreeRootInfoHandler(uint transactionId, DargonGame game)
         : base(transactionId)
      {
         RequestGame = game;
      }

      /// <summary>
      /// Initializes our Locally Initialized transaction handler.
      /// </summary>
      /// <param name="session"></param>
      public override void InitializeInteraction(IDSPExSession session)
      {
         using (var ms = new MemoryStream() { Position = 0 })
         using (var writer = new BinaryWriter(ms))
         {
            writer.Write(RequestGame);
            session.SendMessage(
               new DSPExInitialMessage(
                  TransactionId,
                  (byte)DSPEx.DSP_EX_C2S_IO_GET_ROOT_INFO,
                  ms.GetBuffer(),
                  0,
                  (int)ms.Length
               )
            );
         }
      }

      /// <summary>
      /// Processes the response to our LIT Handler.  The data segment of the message should be
      /// identical to our request data, as this is an echo implementation.
      /// </summary>
      /// <param name="session"></param>
      /// <param name="message"></param>
      public override void ProcessMessage(IDSPExSession session, DSPExMessage message)
      {
         uint rootNodeId = BitConverter.ToUInt32(message.DataBuffer, message.DataOffset);
         uint treeId = BitConverter.ToUInt32(message.DataBuffer, message.DataOffset + 4);
         string gameName;

         using (MemoryStream ms = new MemoryStream(message.DataBuffer) { Position = message.DataOffset + 8 })
         using (BinaryReader reader = new BinaryReader(ms))
         {
            gameName = reader.ReadTinyText();
         }

         Result = new RootInfoResult(RequestGame, rootNodeId, treeId, gameName);
         session.DeregisterLITransactionHandler(this);
         OnCompletion();
      }
   }
}
