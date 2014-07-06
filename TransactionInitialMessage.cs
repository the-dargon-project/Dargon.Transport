using System;
using System.Text;

namespace Dargon.Transport
{
   /// <summary>
   /// DSP Interactions begin with a DSPExInitialMessage message being sent.  This message will
   /// contain opcode information, allowing the DSP Server or Client to use the appropriate
   /// interaction handler.
   /// 
   /// This class intentionally does not implement DSPExMessage, so that method overloading can
   /// be used.
   /// </summary>
   public class TransactionInitialMessage
   {
      /// <summary>
      /// The transaction ID of our DSPEx Message
      /// </summary>
      public uint TransactionId { get; private set; }

      /// <summary>
      /// The opcode which initiated our DSPEx interaction
      /// </summary>
      public byte Opcode { get; private set; }

      /// <summary>
      /// The data contained within our DSPEx message
      /// </summary>
      public byte[] DataBuffer { get; private set; }

      /// <summary>
      /// The offset in our data buffer for our data
      /// </summary>
      public int DataOffset { get; private set; }

      /// <summary>
      /// The length of our data in the data buffer
      /// </summary>
      public int DataLength { get; private set; }

      /// <summary>
      /// Creates a new instance of a DSPEx message
      /// </summary>
      /// <param name="transactionId">The transaction ID of our DSPEx message</param>
      /// <param name="opcode">The opcode of our DSPEx message</param>
      /// <param name="data">Some buffer</param>
      /// <param name="offset">
      /// The offset in the parameter buffer for our data
      /// </param>
      /// <param name="length">
      /// The length of our data
      /// </param>
      public TransactionInitialMessage(
         uint transactionId,
         byte opcode,
         byte[] data,
         int offset,
         int length)
      {
         TransactionId = transactionId;
         Opcode = opcode;
         DataBuffer = data ?? new byte[0];
         DataOffset = offset;
         DataLength = length;
      }

      /// <summary>
      /// Creates a new instance of a DSPEx message
      /// </summary>
      /// <param name="transactionId">The transaction ID of our DSPEx message</param>
      /// <param name="opcode">The opcode of our DSPEx message</param>
      public TransactionInitialMessage(
         uint transactionId,
         byte opcode)
      {
         TransactionId = transactionId;
         Opcode = opcode;
         DataBuffer = new byte[0];
         DataOffset = 0;
         DataLength = 0;
      }

      public void DumpToConsole()
      {
         Console.WriteLine("Transaction ID: " + this.TransactionId + " opcode " + this.Opcode);
         for (int i = 0; i < this.DataLength; i += 16)
         {
            StringBuilder sb = new StringBuilder();
            for (int offset = 0; offset < 16 && i + offset < this.DataLength; offset++)
            {
               sb.Append(this.DataBuffer[this.DataOffset + i + offset].ToString("X").PadLeft(2, '0'));
               if (offset != 0 && offset % 2 == 0)
                  sb.Append(" ");
            }
            Console.WriteLine(sb.ToString());
         }
      }
   }
}
