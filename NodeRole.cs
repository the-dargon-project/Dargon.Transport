using System;

namespace Dargon.Transport
{
   [Flags]
   public enum NodeRole : byte
   {
      /// <summary>
      /// The node is supposed to listen for connections
      /// </summary>
      Server = 0x01,

      /// <summary>
      /// The node will initiate DSPEx sessions by connecting to other nodes.
      /// </summary>
      Client = 0x02,

      /// <summary>
      /// Valid for DtpNode creation - the node will become a server if none exists;
      /// else, the node will become a client.
      /// </summary>
      ServerOrClient = 0x04
   }
}