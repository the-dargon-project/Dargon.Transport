namespace Dargon.IO.DSP
{
   /// <summary>
   /// After the initial DSPExInitialMessage message is sent, DSPExMessages are sent.  These
   /// Messages just contain transaction id and FileTree.
   /// </summary>
   public class DSPExMessage
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
      public DSPExMessage(
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
   }
}
