using System;
using System.IO;
using System.IO.Pipes;
using System.ServiceModel.Channels;

namespace Dargon.Transport
{
   public class DSPExNamedPipeFrameTransmitter : IDSPExFrameTransmitter
   {
      /// <summary>
      /// The named pipe client stream associated with this frame transmitter.
      /// </summary>
      private readonly NamedPipeClientStream m_stream;

      /// <summary>
      /// The binary writer which is used to write from to network stream
      /// </summary>
      private readonly BinaryWriter m_writer;

      /// <summary>
      /// All input is stored into this input buffer.
      /// </summary>
      private readonly byte[] m_inputBuffer = new byte[DSPConstants.kMaxMessageSize];

      /// <summary>
      /// The buffer pool, which provides us input buffers for reading in messages.
      /// </summary>
      private readonly BufferManager m_bufferPool;

      /// <summary>
      /// Initializes a new instance of a TCP Frame Transmitter for DSPEx
      /// </summary>
      /// <param name="pipeName">
      /// The name of the pipe which we are writing to
      /// </param>
      public DSPExNamedPipeFrameTransmitter(string pipeName)
      {
         m_stream = new NamedPipeClientStream(".", pipeName, PipeDirection.InOut, PipeOptions.Asynchronous);
         m_stream.Connect();
         m_writer = new BinaryWriter(m_stream);

         // Elevate to DSPEx - this blocks until the byte has been written to the underlying stream.
         m_writer.Write((byte)DSP.DSP_EX_INIT);

         m_bufferPool = BufferManager.CreateBufferManager(100000, DSPConstants.kMaxMessageSize);
         for (int i = 0; i < 100; i++)
            m_bufferPool.ReturnBuffer(new byte[DSPConstants.kMaxMessageSize]);
      }

      /// <summary>
      /// Begins receiving message frames.
      /// </summary>
      /// <param name="onFrameReceived">
      /// When a message frame is received, this callback is invoked.
      /// </param>
      public void BeginReceivingMessageFrames(Action<byte[]> onFrameReceived)
      {
         BeginReceiveMessage(onFrameReceived);
      }

      /// <summary>
      /// Begins receiving a DSPEx message.
      /// In reality, this starts async read to get the UINT messageLength of an incoming message.
      /// After that is received, another async read begins to get the transaction id of the message.
      /// </summary>
      /// <param name="onFrameReceived">
      /// This callback is invoked when a frame is received.
      /// </param>
      private void BeginReceiveMessage(Action<byte[]> onFrameReceived)
      {
         StateObject so = new StateObject()
         {
            buffer = m_bufferPool.TakeBuffer(DSPConstants.kMaxMessageSize),
            bytesRead = 0
         };
         ContinueReceiveLength(so, onFrameReceived);
      }

      /// <summary>
      /// Continues to receive a DSPEx message.
      /// The first block of this method runs an async loop which reads the first four bytes of our
      /// DSPEx message, which tells us the length of our message.
      /// The second block of the method runs an async loop which reads the remainder of the message.
      /// </summary>
      /// <param name="so"></param>
      /// <param name="onFrameReceived">
      /// This callback is invoked when a frame is received.
      /// </param>
      private void ContinueReceiveLength(StateObject so, Action<byte[]> onFrameReceived)
      {

         m_stream.BeginRead(
            so.buffer,
            so.bytesRead, 4 - so.bytesRead, //Read bytes of index [0, 3]
            (asyncResult) => {
               int bytesRead = m_stream.EndRead(asyncResult);
               so.bytesRead += bytesRead;

               // When we've read four bytes, we're done.
               if (so.bytesRead == 4)
               {
                  so.bytesTotal = (int)BitConverter.ToUInt32(so.buffer, 0);
                  ContinueReceivePostLength(so, onFrameReceived);
               }
               else
               {
                  ContinueReceiveLength(so, onFrameReceived);
               }
            },
            null
         );
      }
      private void ContinueReceivePostLength(StateObject so, Action<byte[]> onFrameReceived)
      {
         m_stream.BeginRead(
            so.buffer,
            so.bytesRead, so.bytesTotal - so.bytesRead,
            (eSecond) =>
            {
               int bytesRead = m_stream.EndRead(eSecond);
               so.bytesRead += bytesRead;
         
               if (so.bytesRead == so.bytesTotal)
               {
                  BeginReceiveMessage(onFrameReceived);
                  onFrameReceived(so.buffer);
                  m_bufferPool.ReturnBuffer(so.buffer);
               }
               else
               {
                  ContinueReceivePostLength(so, onFrameReceived);
               }
            },
            null
         );
      }

      public void SendRawFrame(byte[] buffer, int offset, int size, Action onFrameSendComplete)
      {
         m_stream.BeginWrite(
            buffer,
            offset,
            (int)size,
            (s) => {
               m_stream.EndWrite(s);
               onFrameSendComplete();
            },
            m_stream
         );
      }


      /// <summary>
      /// State object for DSPEx asynchronous receiving.
      /// </summary>
      public class StateObject
      {
         /// <summary>
         /// The total number of bytes which we have read so far 
         /// </summary>
         public int bytesRead = 0;

         /// <summary>
         /// The total number of bytes in the DSP Message which we are reading
         /// </summary>
         public int bytesTotal = 0;

         /// <summary>
         /// The buffer which we are storing stuff in.
         /// </summary>
         public byte[] buffer; //Contains the entire message frame, including header
      }
   }
}
