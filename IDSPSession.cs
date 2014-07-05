using System;

namespace Dargon.IO.DSP
{
   public interface IDSPSession : ISendTimeContext
   {
      /// <summary>
      /// Whether or not our session is currently in interactive mode.
      /// </summary>
      bool Interactive { get; set; }
      
      //-------------------------------------------------------------------------------------------
      // Event invokers
      //-------------------------------------------------------------------------------------------
      void OnDSPReadLine(string s);

      //-------------------------------------------------------------------------------------------
      // Read Operations
      //-------------------------------------------------------------------------------------------
      /// <summary>
      /// Reads a (byte)boolean to the DSP output stream.
      /// If val is zero, we return a false
      /// else we return a true
      /// </summary>
      bool ReadBool();

      /// <summary>
      /// Reads a byte from the DSP input stream
      /// </summary>
      /// <returns></returns>
      byte ReadByte();

      /// <summary>
      /// Reads a uint from the DSP input stream
      /// </summary>
      /// <returns></returns>
      UInt32 ReadUInt32();

      /// <summary>
      /// Reads the given number of bytes from the DSP input stream
      /// </summary>
      byte[] ReadBytes(int byteCount);

      /// <summary>
      /// Reads a string from the DSP output.  This is in anticipation of Dargon eventually
      /// supporting unicode.
      /// </summary>
      string ReadString();

      /// <summary>
      /// Reads the length of the bytes, followed by the bytes themselves.
      /// </summary>
      byte[] ReadBlob();

      //-------------------------------------------------------------------------------------------
      // Write Operations
      //-------------------------------------------------------------------------------------------
      /// <summary>
      /// Writes a boolean to the DSP output stream.
      /// If val is true, we send a 1
      /// if val is false, we send a 0
      /// </summary>
      void WriteBool(bool val);

      /// <summary>
      /// Writes a byte to the DSP output stream
      /// </summary>
      /// <returns></returns>
      void WriteByte(byte val);

      /// <summary>
      /// Writes a uint to the DSP output stream
      /// </summary>
      /// <returns></returns>
      void WriteUInt32(uint val);

      /// <summary>
      /// Writes all the given bytes to the DSP output stream, without prefixing length
      /// </summary>
      void WriteBytes(byte[] bytes);

      /// <summary>
      /// Writes a string to the DSP output.  This is in anticipation of Dargon eventually
      /// supporting unicode.
      /// </summary>
      void WriteString(string s);

      /// <summary>
      /// Writes the length of the bytes, followed by the bytes themselves.
      /// </summary>
      /// <param name="bytes"></param>
      void WriteBlob(byte[] bytes);

      /// <summary>
      /// Writes a line of response output.  This is only valid when DSP is in interactive
      /// mode, and the writeline is a response to a ReadLine.  This method should only
      /// be used in DSPManager command line handlers.
      /// 
      /// In a DSP CLI Handler, this method may be called as many times as wanted.
      /// </summary>
      /// <param name="s">The response string</param>
      void WriteLine(string s);
   }
}
