using System;

namespace Dargon.IO.DSP
{
   public interface IDSPExFrameTransmitter
   {
      /// <summary>
      /// Begins receiving message frames.
      /// </summary>
      /// <param name="onFrameReceived">
      /// When a message frame is received, this callback is invoked.
      /// </param>
      void BeginReceivingMessageFrames(Action<byte[]> onFrameReceived);

      /// <summary>
      /// Sends a raw frame to the remote endpoint
      /// </summary>
      /// <param name="buffer">
      /// The buffer which contains the data we're sending
      /// </param>
      /// <param name="offset">
      /// The offset in the buffer where our entire frame starts
      /// </param>
      /// <param name="size">
      /// The length of the entire frame in our buffer
      /// </param>
      /// <param name="onFrameSendComplete">
      /// This method is invoked when a frame send completes.
      /// </param>
      void SendRawFrame(byte[] buffer, int offset, int size, Action onFrameSendComplete);
   }
}
