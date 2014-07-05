namespace Dargon.IO.DSP
{
   /// <summary>
   /// Dargon Service Protocol
   /// </summary>
   public enum DSP : byte
   {
      //-------------------------------------------------------------------------------------------
      // Transaction Flow
      //-------------------------------------------------------------------------------------------
      DSP_OK                        = 0x00,
      DSP_DONE                      = DSP_OK,
      DSP_CONTINUE                  = 0x01,
      
      DSP_GSM_DONE                  = 0x00,
      DSP_GSM_SEND                  = 0x01,
      DSP_GSM_SEND_EXPLICIT         = 0x02,
      DSP_GSM_SEND_BUFFER           = 0x03,
      DSP_GSM_SEND_BUFFER_EXPLICIT  = 0x04,
      DSP_GSM_SEND_FILE             = 0x05,
      //DSP_GSM_SEND_SOUND

      DSP_WRITELINE                 = 0x10,

      DSP_GET_DARGON_VERSION        = 0x20,
      DSP_GET_STATIC_MODIFICATIONS  = 0x40,

      DSP_SET_INTERACTIVE           = 0xE0,
      
      DSP_EX_INIT                   = 0xFE,

      DSP_GOODBYE                   = 0xFF
   }
}
