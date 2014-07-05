using System.IO;

namespace Dargon.IO.DSP.ClientImpl
{
   public class DSPExLITGetNodeInfosHandler : DSPExLITransactionHandler
   {
      /// <summary>
      /// The node whose child data we requested
      /// </summary>
      public uint[] RequestNodeIDs { get; private set; }

      /// <summary>
      /// The request requestFlags transmitted in our initial request, which specify the data we will
      /// be receiving.
      /// </summary>
      public DSPExNodeRequestFlags RequestFlags { get; private set; }

      /// <summary>
      /// Whether or not the given node exists
      /// </summary>
      public bool[] ResponseNodesExist { get; private set; }

      /// <summary>
      /// An array of response descriptors.  This is used over a IDargonNode 
      /// </summary>
      public DSPExNodeResponseDescriptor[] ResponseChildNodes { get; private set; }

      /// <summary>
      /// Initializes a new instance of a DSPEx Locally Initialized Get Resource Tree UID handler.
      /// </summary>
      /// <param name="transactionId">
      /// The id assigned to this DSPEx transaction
      /// </param>
      /// <param name="nodeId">
      /// The id of the node whose children descriptors we are getting
      /// </param>
      /// <param name="requestFlags">
      /// Request requestFlags describing the type of data we want to get from the remote endpoint.
      /// </param>
      public DSPExLITGetNodeInfosHandler(uint transactionId, uint[] nodeIds, DSPExNodeRequestFlags requestFlags)
         : base(transactionId)
      {
         RequestNodeIDs = nodeIds;
         RequestFlags = requestFlags;
      }

      /// <summary>
      /// Initializes our Locally Initialized transaction handler.
      /// </summary>
      /// <param name="session"></param>
      public override void InitializeInteraction(IDSPExSession session)
      {
         using (var ms = new MemoryStream(1 + 4 + 4 * RequestNodeIDs.Length) { Position = 0 })
         using (var writer = new BinaryWriter(ms))
         {
            writer.Write((uint)RequestNodeIDs.Length);
            for(int i = 0; i < RequestNodeIDs.Length; i++)
               writer.Write((uint)RequestNodeIDs[i]);
            writer.Write(RequestFlags);
            session.SendMessage(
               new DSPExInitialMessage(
                  TransactionId,
                  (byte)DSPEx.DSP_EX_C2S_IO_GET_NODE_INFO,
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
         using (var ms = new MemoryStream(message.DataBuffer) { Position = message.DataOffset })
         using (var reader = new BinaryReader(ms))
         {
            ResponseNodesExist = new bool[RequestNodeIDs.Length];
            ResponseChildNodes = new DSPExNodeResponseDescriptor[RequestNodeIDs.Length];

            for (int i = 0; i < RequestNodeIDs.Length; i++)
            {
               ResponseNodesExist[i] = reader.ReadByte() != 0;
               if (ResponseNodesExist[i])
                  ResponseChildNodes[i] = reader.ReadNodeResponseDescriptor(RequestFlags);
            }
         }
      }
   }
}
