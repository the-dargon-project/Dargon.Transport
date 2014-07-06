using System;
using System.Text;

namespace Dargon.Transport
{
   /// <summary>
   /// After the initial DSPExInitialMessage message is sent, DSPExMessages are sent.  These
   /// Messages just contain transaction id and FileTree.
   /// </summary>
   public class TransactionMessage
   {
      /// <summary>
      /// The transaction ID of our DSPEx Message
      /// </summary>
      public uint TransactionId { get; private set; }

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
      /// <param name="data">Some buffer</param>
      /// <param name="offset">
      /// The offset in the parameter buffer for our data
      /// </param>
      /// <param name="length">
      /// The length of our data
      /// </param>
      public TransactionMessage(
         uint transactionId,
         byte[] data,
         int offset,
         int length)
      {
         TransactionId = transactionId;
         DataBuffer = data;
         DataOffset = offset;
         DataLength = length;
      }

      public void DumpToConsole()
      {
         Console.WriteLine("Transaction ID: " + this.TransactionId);
         for (int i = 0; i < this.DataLength; i += 16)
         {
            StringBuilder sb = new StringBuilder();
            for (int offset = 0; offset < 16 && i + offset < this.DataLength; offset++)
            {
               sb.Append(this.DataBuffer[this.DataOffset + i + offset].ToString("X").PadLeft(2, '0'));
               if (offset % 2 == 1)
                  sb.Append(" ");
            }
            Console.WriteLine(sb.ToString());
         }
      }
   }
}
