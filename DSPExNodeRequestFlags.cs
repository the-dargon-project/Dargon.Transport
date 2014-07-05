using System;

namespace Dargon.IO.DSP
{
   [Flags]
   public enum DSPExNodeRequestFlags : byte
   {
      ID                      = 0x01,
      ParentID                = 0x02,
      FileDescriptor          = 0x04,
      ChildrenCount           = 0x08,
      Children                = 0x18,

      Name                    = 0x20,
      Path                    = 0x40,

      NodeType                = 0x80,

      None                    = 0x00,
      All                     = 0xFF
   }
}
