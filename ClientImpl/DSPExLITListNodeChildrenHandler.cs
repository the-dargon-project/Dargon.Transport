using System.Collections.Generic;
using System.IO;

namespace Dargon.IO.DSP.ClientImpl
{
   class DSPExLITListNodeChildrenHandler : DSPExLITransactionHandler
   {
      /// <summary>
      /// The node whose child data we requested
      /// </summary>
      public uint RequestNodeID { get; private set; }

      /// <summary>
      /// The request requestFlags transmitted in our initial request, which specify the data we will
      /// be receiving.
      /// </summary>
      public DSPExNodeRequestFlags RequestFlags { get; private set; }

      /// <summary>
      /// Whether or not the given node exists
      /// </summary>
      public bool ResponseNodeExists { get; private set; }

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
      public DSPExLITListNodeChildrenHandler(uint transactionId, uint nodeId, DSPExNodeRequestFlags requestFlags)
         : base(transactionId)
      {
         RequestNodeID = nodeId;
         RequestFlags = requestFlags;
      }

      /// <summary>
      /// Initializes our Locally Initialized transaction handler.
      /// </summary>
      /// <param name="session"></param>
      public override void InitializeInteraction(IDSPExSession session)
      {
         using (var ms = new MemoryStream(5) { Position = 0 })
         using (var writer = new BinaryWriter(ms))
         {
            writer.Write(RequestNodeID);
            writer.Write(RequestFlags);
            session.SendMessage(
               new DSPExInitialMessage(
                  TransactionId,
                  (byte)DSPEx.DSP_EX_C2S_IO_GET_NODE_CHILDREN,
                  ms.GetBuffer(),
                  0,
                  4
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
            ResponseNodeExists = reader.ReadBoolean();
            if (ResponseNodeExists)
            {
               uint childCount = reader.ReadUInt32();

               List<DSPExNodeResponseDescriptor> responseDescriptors = new List<DSPExNodeResponseDescriptor>();
               for (int i = 0; i < childCount; i++)
               {
                  DSPExNodeResponseDescriptor desc = new DSPExNodeResponseDescriptor(RequestFlags);

                  if (RequestFlags.HasFlag(DSPExNodeRequestFlags.ID))
                     desc.ID = reader.ReadUInt32();

                  if (RequestFlags.HasFlag(DSPExNodeRequestFlags.ParentID))
                     desc.ParentID = reader.ReadUInt32();

                  if (RequestFlags.HasFlag(DSPExNodeRequestFlags.FileDescriptor))
                  {
                     desc.FileDataState = reader.ReadCompressionAlgorithm();
                     if (desc.FileDataState == DataState.DS_NO_DATA) ;
                     else
                     {
                        desc.FileOffset = checked((int)reader.ReadUInt32());
                        desc.FileLength = checked((int)reader.ReadUInt32());
                        desc.FilePath = reader.ReadText();
                     }
                  }

                  if (RequestFlags.HasFlag(DSPExNodeRequestFlags.ChildrenCount))
                  {
                     desc.ChildrenCount = checked((int)reader.ReadUInt32());
                     if (RequestFlags.HasFlag(DSPExNodeRequestFlags.Children))
                     {
                        uint[] childrenIDs = new uint[desc.ChildrenCount];
                        for (int j = 0; j < desc.ChildrenCount; j++)
                           childrenIDs[j] = reader.ReadUInt32();
                        desc.ChildrenIDs = childrenIDs;
                     }
                  }

                  if (RequestFlags.HasFlag(DSPExNodeRequestFlags.Name))
                     desc.ResourceName = reader.ReadTinyText();

                  if (RequestFlags.HasFlag(DSPExNodeRequestFlags.Path))
                     desc.ResourcePath = reader.ReadText();

                  responseDescriptors.Add(desc);
               }
               ResponseChildNodes = responseDescriptors.ToArray();
            }
            session.DeregisterLITransactionHandler(this);
            OnCompletion();
         }
      }
   }
}
