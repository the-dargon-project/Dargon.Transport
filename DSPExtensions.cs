using System;
using System.IO;
using System.Text;

namespace Dargon.Transport
{
   public static class DSPExtensions
   {

      /// <summary>
      /// Writes the given tiny text to the binary writer.
      /// TinyText is written as a pascal string with length encoded in 8-bits.
      /// </summary>
      /// <param name="writer"></param>
      /// <param name="s"></param>
      public static void WriteTinyText(this BinaryWriter writer, string s)
      {
         if (s.Length > 0xFF)
            throw new Exception("Couldn't write the string " + s + " as tinytext, as it was too long");
         else
         {
            var content = Encoding.ASCII.GetBytes(s);
            writer.Write((byte)s.Length);
            writer.Write(content, 0, s.Length);
         }
      }

      /// <summary>
      /// Writes the given text to the binary writer.
      /// Text is written as a pascal string with length encoded in 16-bits.
      /// </summary>
      /// <param name="writer"></param>
      /// <param name="s"></param>
      public static void WriteText(this BinaryWriter writer, string s)
      {
         if (s.Length > 0xFFFF)
            throw new Exception("Couldn't write the string " + s + " as text, as it was too long");
         else
         {
            var content = Encoding.ASCII.GetBytes(s);
            writer.Write((ushort)s.Length);
            writer.Write(content, 0, s.Length);
         }
      }

      /// <summary>
      /// Writes the given text to the binary writer.
      /// LongText is written as a pascal string with length encoded in 32-bits.
      /// </summary>
      /// <param name="writer"></param>
      /// <param name="s"></param>
      public static void WriteLongText(this BinaryWriter writer, string s)
      {
         // We don't do any range checking, as string.length is a signed integer value,
         // and thusly cannot surpass 2^32 - 1
         var content = Encoding.ASCII.GetBytes(s);
         writer.Write((uint)s.Length);
         writer.Write(content, 0, s.Length);
      }

      /// <summary>
      /// Reads tiny text from the given binary reader.
      /// TinyText is written as a pascal string with length encoded in 8-bits.
      /// </summary>
      /// <param name="reader"></param>
      /// <returns></returns>
      public static string ReadTinyText(this BinaryReader reader)
      {
         var length = reader.ReadByte();
         return Encoding.ASCII.GetString(reader.ReadBytes(length));
      }

      /// <summary>
      /// Reads text from the given binary reader.
      /// Text is written as a pascal string with length encoded in 16-bits.
      /// </summary>
      /// <param name="reader"></param>
      /// <returns></returns>
      public static string ReadText(this BinaryReader reader)
      {
         var length = reader.ReadUInt16();
         return Encoding.ASCII.GetString(reader.ReadBytes(length));
      }

      /// <summary>
      /// Reads long text from the given binary reader.
      /// LongText is written as a pascal string with length encoded in 32-bits.
      /// </summary>
      /// <param name="reader"></param>
      /// <returns></returns>
      public static string ReadLongText(this BinaryReader reader)
      {
         var length = reader.ReadUInt32();

         if (length > Int32.MaxValue)
            throw new Exception("Attempted to read a string longer than permitted by .net");
         else
            return Encoding.ASCII.GetString(reader.ReadBytes((int)length));
      }
   }
}
