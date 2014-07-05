namespace Dargon.Transport
{
   public static class DSPConstants
   {
      /// <summary>
      /// The maximum size of a DSP message.
      /// </summary>
      public const int kMaxMessageSize = 20000;

      /// <summary>
      /// The size of a DSPEx Message's overhead.
      /// </summary>
      public const int kMessageFrameOverhead = 4 + 4 + 1;

      /// <summary>
      /// The node id reserved for invalid node IDs.  For example, the parent node of a root node.
      /// </summary>
      public const uint kInvalidNodeID = 0xFFFFFFFFU;
   }
}
