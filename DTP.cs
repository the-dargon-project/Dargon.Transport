namespace Dargon.Transport
{
   public enum DTP : byte
   {
      // TODO: Remove this.
      DSPEX_INIT                                = 0xFE,


      //-------------------------------------------------------------------------------------------
      // Transaction Flow
      //-------------------------------------------------------------------------------------------
      OK                                        = 0x00,
      DONE                                      = OK,
      CONTINUE                                  = 0x01,

      USER_RESERVED_BEGIN                       = 0x00,
      USER_RESERVED_END                         = 0x7F,

      SYSTEM_RESERVED_BEGIN                     = 0x80,
      GET_VERSION                               = 0x80,
      TERMINAL_OPEN                             = 0x90,
      TERMINAL_WRITELINE                        = 0x91,
      TERMINAL_CLOSE                            = 0x9F,

      ECHO                                      = 0xFE,
      EVENT_QUIT                                = 0xFF,
      SYSTEM_RESERVED_END                       = 0xFF
   }
}