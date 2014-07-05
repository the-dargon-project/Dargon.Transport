using System.IO;

namespace Dargon.IO.DSP.ClientImpl
{
   public class DSPExLITGetBreadcrumbInfoHandler : DSPExLITransactionHandler
   {
      /// <summary>
      /// The node which will start our breadcrumb chain
      /// </summary>
      public uint RequestStartNodeID { get; private set; }

      /// <summary>
      /// The node which will end our breadcrumb chain
      /// </summary>
      public uint RequestEndNodeID { get; private set; }

      /// <summary>
      /// The request requestFlags transmitted in our initial request, which specify the data we will
      /// be receiving.
      /// </summary>
      public DSPExNodeRequestFlags RequestFlags { get; private set; }

      /// <summary>
      /// An array of response descriptors.  This is used over a IDargonNode 
      /// </summary>
      public DSPExNodeResponseDescriptor[] ResponseNodePath { get; private set; }

      /// <summary>
      /// Initializes a new instance of a DSPEx Locally Initialized Get Resource Tree UID handler.
      /// </summary>
      /// <param name="transactionId">
      /// The id assigned to this DSPEx transaction
      /// </param>
      /// <param name="startNodeId">
      /// The id of the node which will start our breadcrumb chain
      /// </param>
      /// <param name="endNodeId">
      /// The id of the node which will end our breadcrumb chain
      /// </param>
      /// <param name="requestFlags">
      /// Request requestFlags describing the type of data we want to get from the remote endpoint.
      /// </param>
      public DSPExLITGetBreadcrumbInfoHandler(uint transactionId, uint startNodeId, uint endNodeId, DSPExNodeRequestFlags requestFlags)
         : base(transactionId)
      {
         RequestStartNodeID = startNodeId;
         RequestEndNodeID = endNodeId;
         RequestFlags = requestFlags;
      }

      /// <summary>
      /// Initializes our Locally Initialized transaction handler.
      /// </summary>
      /// <param name="session"></param>
      public override void InitializeInteraction(IDSPExSession session)
      {
         using (var ms = new MemoryStream(sizeof(uint) + sizeof(uint) + sizeof(DSPExNodeRequestFlags)) { Position = 0 })
         using (var writer = new BinaryWriter(ms))
         {
            writer.Write((uint)RequestStartNodeID);
            writer.Write((uint)RequestEndNodeID);
            writer.Write(RequestFlags);

            session.SendMessage(
               new DSPExInitialMessage(
                  TransactionId,
                  (byte)DSPEx.DSP_EX_C2S_IO_GET_BREADCRUMBS_INFO,
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
            uint breadcrumbCount = reader.ReadUInt32();
            var breadcrumbs = new DSPExNodeResponseDescriptor[breadcrumbCount];
            for (uint i = 0; i < breadcrumbCount; i++)
               breadcrumbs[i] = reader.ReadNodeResponseDescriptor(RequestFlags);
            ResponseNodePath = breadcrumbs;
            session.DeregisterLITransactionHandler(this);
            OnCompletion();
         }
      }
   }
}
