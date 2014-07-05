namespace Dargon.Transport
{
   public enum DSPEx : byte
   {
      //-------------------------------------------------------------------------------------------
      // Transaction Flow
      //-------------------------------------------------------------------------------------------
      DSP_EX_OK                                       = 0x00,
      DSP_EX_DONE                                     = DSP_EX_OK,
      DSP_EX_CONTINUE                                 = 0x01,

      //-------------------------------------------------------------------------------------------
      // S2C Event Opcodes
      //-------------------------------------------------------------------------------------------
      DSP_EX_S2C_DIM_RUN_TASKS                        = 0xA1,
      DSP_EX_S2C_EVENT_QUIT                           = 0xE0,
      DSP_EX_S2C_EVENT_RESOURCES_RELOAD               = 0xE1,

      //-------------------------------------------------------------------------------------------
      // C2S Event Opcodes
      //-------------------------------------------------------------------------------------------
      DSP_EX_C2S_META_GET_DARGON_VERSION              = 0x10,

      DSP_EX_C2S_MOD_LS_ROOT                          = 0x50,

      DSP_EX_C2S_CONSOLE_OPEN                         = 0x70,
      DSP_EX_C2S_CONSOLE_WRITELINE                    = 0x71,
      DSP_EX_C2S_CONSOLE_CLOSE                        = 0x79,

      DSP_EX_C2S_REMOTE_LOG                           = 0x80,

      DSP_EX_C2S_ECHO                                 = 0xFE,
      DSP_EX_C2S_EVENT_QUIT                           = 0xFF,
   }
}