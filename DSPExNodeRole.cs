using System;

namespace Dargon.Transport
{
   [Flags]
   public enum DSPExNodeRole : byte
   {
      /// <summary>
      /// The node is supposed to listen for connections
      /// </summary>
      Server = 0x01,

      /// <summary>
      /// The node will initiate DSPEx sessions by connecting to other nodes.
      /// </summary>
      Client = 0x02
   }
}