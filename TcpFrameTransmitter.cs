using System;
using System.IO;
using System.Net.Sockets;

namespace Dargon.Transport
{
   public class TcpFrameTransmitter : IFrameTransmitter
   {
      /// <summary>
      /// The socket which is used for our DSP connection
      /// </summary>
      private readonly Socket m_socket;

      /// <summary>
      /// The network stream associated with our socket
      /// </summary>
      private readonly NetworkStream m_networkStream;

      /// <summary>
      /// The binary writer which is used to write from to network stream
      /// </summary>
      private readonly BinaryWriter m_writer;

      /// <summary>
      /// All input is stored into this input buffer.
      /// </summary>
      private readonly byte[] m_inputBuffer = new byte[DTPConstants.kMaxMessageSize];

      /// <summary>
      /// Initializes a new instance of a TCP Frame Transmitter for DSPEx
      /// </summary>
      /// <param name="host">
      /// The hostname which we are connecting to.
      /// </param>
      /// <param name="port">
      /// The port which we are connecting to.
      /// </param>
      public TcpFrameTransmitter(string host, int port)
      {
         m_socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
         m_socket.Connect(host, port);
         m_networkStream = new NetworkStream(m_socket);
         m_writer = new BinaryWriter(m_networkStream);

         // Elevate to DSPEx - this blocks until the byte has been written to the underlying stream.
         m_writer.Write((byte)DTP.DSPEX_INIT);
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
            buffer = m_inputBuffer,
            bytesRead = 0
         };
         ContinueReceiveMessage(so, onFrameReceived);
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
      private void ContinueReceiveMessage(StateObject so, Action<byte[]> onFrameReceived)
      {
         bool readingLength = so.bytesRead < 4;
         if (readingLength)
         {
            m_networkStream.BeginRead(
               so.buffer,
               so.bytesRead, 4 - so.bytesRead, //Read bytes of index [0, 3]
               (asyncResult) =>
               {
                  int bytesRead = m_networkStream.EndRead(asyncResult);
                  so.bytesRead += bytesRead;

                  // When we've read four bytes, we're done.
                  if (so.bytesRead == 4)
                  {
                     so.bytesTotal = (int)BitConverter.ToUInt32(so.buffer, 0);
                  }

                  ContinueReceiveMessage(so, onFrameReceived);
               },
               null
            );
         }
         else
         {
            m_networkStream.BeginRead(
               so.buffer,
               so.bytesRead, so.bytesTotal - so.bytesRead,
               (eSecond) =>
               {
                  int bytesRead = m_networkStream.EndRead(eSecond);
                  so.bytesRead += bytesRead;

                  if (so.bytesRead == so.bytesTotal)
                  {
                     onFrameReceived(so.buffer);
                     BeginReceiveMessage(onFrameReceived);
                  }
                  else
                  {
                     ContinueReceiveMessage(so, onFrameReceived);
                  }
               },
               null
            );
         }
      }

      public void SendRawFrame(byte[] buffer, int offset, int size, Action onFrameSendComplete)
      {
         m_networkStream.BeginWrite(
            buffer,
            offset,
            (int)size,
            (s) => {
               m_networkStream.EndWrite(s);
               onFrameSendComplete();
            },
            m_networkStream
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
