using System;
using System.IO;
using System.Text;
using Dargon.Games;

namespace Dargon.IO.DSP
{
   public static class DSPExtensions
   {
      /// <summary>
      /// Writes the Dargon Game data type with the given binary writer.
      /// </summary>
      /// <param name="writer"></param>
      /// <param name="gameType">
      /// The Dargon Game Type which we are transmitting.  This is transmitted as one byte at the 
      /// moment, though the data type's size may change in the future.
      /// </param>
      public static void Write(this BinaryWriter writer, DargonGame gameType)
      {
         writer.Write((byte)gameType);
      }

      /// <summary>
      /// Writes a Dargon Service Protocol Node request flag set to the given binary writer.
      /// </summary>
      /// <param name="writer"></param>
      /// <param name="flags">
      /// The node request flags (this is usually sent from the DSP Client to the DSP server).
      /// </param>
      public static void Write(this BinaryWriter writer, DSPExNodeRequestFlags flags)
      {
         writer.Write((byte)flags);
      }

      /// <summary>
      /// Writes the compression algorithm to the given binary writer
      /// </summary>
      /// <param name="writer"></param>
      /// <param name="algo"></param>
      public static void Write(this BinaryWriter writer, DataState algo)
      {
         writer.Write((byte)algo);
      }

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
      /// Reads the Dargon Game data type with the given binary reader.
      /// </summary>
      /// <param name="reader"></param>
      /// <returns>
      /// The Dargon Game Type which we are transmitting.  This is transmitted as one byte at the 
      /// moment, though the data type's size may change in the future.
      /// </returns>
      public static DargonGame ReadDargonGame(this BinaryReader reader)
      {
         return (DargonGame)reader.ReadByte();
      }

      /// <summary>
      /// Reads a Dargon Service Protocol Node request flag set from the given binary reader.
      /// </summary>
      /// <param name="reader"></param>
      /// <returns>
      /// The node request flags (this is usually sent from the DSP Client to the DSP server).
      /// </returns>
      public static DSPExNodeRequestFlags ReadNodeRequestFlags(this BinaryReader reader)
      {
         return (DSPExNodeRequestFlags)reader.ReadByte();
      }

      /// <summary>
      /// Reads a compression algorithm from the given binary reader.
      /// </summary>
      /// <param name="reader"></param>
      /// <returns></returns>
      public static DataState ReadCompressionAlgorithm(this BinaryReader reader)
      {
         return (DataState)reader.ReadByte();
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

      /// <summary>
      /// Reads a Dargon Service Protocol node descriptor, filling in the given 
      /// DSPExNodeRequestFlags.  Warning: If requestFlags is incorrect, then you'll read corrupt
      /// data.
      /// </summary>
      /// <param name="reader">
      /// Provides a source of data for the read method to read data from.  
      /// </param>
      /// <param name="requestFlags">
      /// Request flags describing the data which is being read from the Binary Reader's underlying
      /// stream.
      /// </param>
      /// <returns></returns>
      public static DSPExNodeResponseDescriptor ReadNodeResponseDescriptor(this BinaryReader reader, DSPExNodeRequestFlags requestFlags)
      {
         // This is all described in Documentation/Dargon Service Protocol Extended.txt
         var result = new DSPExNodeResponseDescriptor(requestFlags);
         if (requestFlags.HasFlag(DSPExNodeRequestFlags.ID))
            result.ID = reader.ReadUInt32();

         if (requestFlags.HasFlag(DSPExNodeRequestFlags.ParentID))
            result.ParentID = reader.ReadUInt32();

         if (requestFlags.HasFlag(DSPExNodeRequestFlags.FileDescriptor))
         {
            result.FileDataState = reader.ReadCompressionAlgorithm();
            if (result.FileDataState == DataState.DS_NO_DATA) ;
            else
            {
               result.FileOffset = checked((int)reader.ReadUInt32());
               result.FileLength = checked((int)reader.ReadUInt32());
               result.FilePath = reader.ReadText();
            }
         }

         if (requestFlags.HasFlag(DSPExNodeRequestFlags.ChildrenCount))
         {
            result.ChildrenCount = checked((int)reader.ReadUInt32());
            if (requestFlags.HasFlag(DSPExNodeRequestFlags.Children))
            {
               uint[] childrenIDs = new uint[result.ChildrenCount];
               for (int j = 0; j < result.ChildrenCount; j++)
                  childrenIDs[j] = reader.ReadUInt32();
               result.ChildrenIDs = childrenIDs;
            }
         }

         if (requestFlags.HasFlag(DSPExNodeRequestFlags.Name))
            result.ResourceName = reader.ReadTinyText();

         if (requestFlags.HasFlag(DSPExNodeRequestFlags.Path))
            result.ResourcePath = reader.ReadText();

         return result;
      }
   }
}
