using System;
using System.IO;
using System.Linq;
using Dargon.Games;
using Dargon.IO.DSP.Results;

namespace Dargon.IO.DSP.ClientImpl
{
   public class DSPExLITGetResourceTreeRootInfosHandler : DSPExLITransactionHandler
   {
      /// <summary>
      /// The game whose resource tree UID we requested.
      /// </summary>
      public DargonGame[] RequestGames { get; private set; }

      /// <summary>
      /// The response data sent to us by the remote endpoint.
      /// </summary>
      public RootInfoResult[] Results { get; private set; }

      /// <summary>
      /// Initializes a new instance of a DSPEx Locally Initialized Get Resource Tree UID handler.
      /// </summary>
      /// <param name="transactionId">
      /// The id assigned to this DSPEx transaction
      /// </param>
      /// <param name="games">
      /// The games whose data we wish to load.  If this is null, we will return all game root 
      /// infos.
      /// </param>
      public DSPExLITGetResourceTreeRootInfosHandler(uint transactionId, DargonGame[] games)
         : base(transactionId)
      {
         RequestGames = (games != null && games.Any() && games[0] != DargonGame.Any) ? games : null;
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
            if (RequestGames == null)
            {
               //RequestGames was null,
               writer.Write((uint)0xFFFFFFFFU);
            }
            else
            {
               writer.Write((uint)RequestGames.Length);
               for(int i = 0; i < RequestGames.Length; i++)
                  writer.Write(RequestGames[i]);
            }
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
         uint gameCount;
         int dataOffset = message.DataOffset;
         if (RequestGames == null)
         {
            gameCount = BitConverter.ToUInt32(message.DataBuffer, dataOffset);
            dataOffset += 4;
         }
         else
         {
            gameCount = (uint)RequestGames.Length;
         }

         Results = new RootInfoResult[gameCount];
         using (MemoryStream ms = new MemoryStream(message.DataBuffer) { Position = dataOffset })
         using (BinaryReader reader = new BinaryReader(ms))
         {
            for (int i = 0; i < gameCount; i++)
            {
               DargonGame gameId = RequestGames != null ? RequestGames[i] : reader.ReadDargonGame();
               uint rootNodeId = reader.ReadUInt32();
               uint treeId = reader.ReadUInt32();
               string gameName = reader.ReadTinyText();
               Results[i] = new RootInfoResult(gameId, rootNodeId, treeId, gameName);
            }
         }

         session.DeregisterLITransactionHandler(this);
         OnCompletion();
      }
   }
}
