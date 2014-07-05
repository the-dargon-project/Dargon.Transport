namespace Dargon.Transport
{
   /// <summary>
   /// Dargon Service Protocol
   /// </summary>
   public enum DSP : byte
   {
      //-------------------------------------------------------------------------------------------
      // Transaction Flow
      //-------------------------------------------------------------------------------------------
      OK                        = 0x00,
      DONE                      = OK,
      CONTINUE                  = 0x01,
      
      GSM_DONE                  = 0x00,
      GSM_SEND                  = 0x01,
      GSM_SEND_EXPLICIT         = 0x02,
      GSM_SEND_BUFFER           = 0x03,
      GSM_SEND_BUFFER_EXPLICIT  = 0x04,
      GSM_SEND_FILE             = 0x05,
      //DSP_GSM_SEND_SOUND

      WRITELINE                 = 0x10,

      GET_VERSION        = 0x20,
      GET_STATIC_MODIFICATIONS  = 0x40,

      SET_INTERACTIVE           = 0xE0,
      
      DSPEX_INIT                   = 0xFE,

      GOODBYE                   = 0xFF
   }
}
