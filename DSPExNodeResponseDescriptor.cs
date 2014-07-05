namespace Dargon.IO.DSP
{
   public class DSPExNodeResponseDescriptor
   {
      /// <summary>
      /// Initializes a new instance of a DSPEx Node Response Descriptor, which is to support the
      /// given requestFlags.
      /// </summary>
      /// <param name="requestFlags"></param>
      public DSPExNodeResponseDescriptor(DSPExNodeRequestFlags requestFlags)
      {
         m_requestFlags = requestFlags;
      }

      /// <summary>
      /// Flags which represent the data stored in this DSPExNodeResponseDescriptor.
      /// </summary>
      public DSPExNodeRequestFlags RequestRequestFlags { get { return m_requestFlags; } }
      private DSPExNodeRequestFlags m_requestFlags;

      public uint ID { get { return m_id; } internal set { m_id = value; } }
      private uint m_id = DSPConstants.kInvalidNodeID;

      public uint ParentID { get { return m_parentId; } internal set { m_parentId = value; } }
      private uint m_parentId = DSPConstants.kInvalidNodeID;

      public DataState FileDataState { get { return m_fileDataState; } internal set { m_fileDataState = value; } }
      private DataState m_fileDataState = DataState.DS_UNKNOWN;

      public long FileOffset { get { return m_fileOffset; } internal set { m_fileOffset = value; } }
      private long m_fileOffset = -1;

      public long FileLength { get { return m_fileLength; } internal set { m_fileLength = value; } }
      private long m_fileLength = -1;

      public string FilePath { get { return m_filePath; } internal set { m_filePath = value; } }
      private string m_filePath = null;

      public long ChildrenCount { get { return m_childrenCount; } internal set { m_childrenCount = value; } }
      private long m_childrenCount = -1;

      public uint[] ChildrenIDs { get { return m_childrenIDs; } internal set { m_childrenIDs = value; } }
      private uint[] m_childrenIDs = null;

      public string ResourceName { get { return m_resourceName; } internal set { m_resourceName = value; } }
      private string m_resourceName = null;

      public string ResourcePath { get { return m_resourcePath; } internal set { m_resourcePath = value; } }
      private string m_resourcePath = null;

      public NodeType NodeType { get { return m_nodeType; } internal set { m_nodeType = value; } }
      private NodeType m_nodeType;
   }
}
