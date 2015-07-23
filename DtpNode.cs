using ItzWarty.Collections;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipes;
using System.Net;
using System.Net.Sockets;
using ItzWarty;


namespace Dargon.Transport
{
   /// <summary>
   /// A node in a Dargon Service Protocol Extended graph
   /// </summary>
   public class DtpNode : IDtpNode
   {
      private readonly IClientSource clientSource;
      private readonly ConcurrentSet<DtpNodeSession> m_sessions = new ConcurrentSet<DtpNodeSession>();
      private readonly List<IInstructionSet> m_instructionSets = new List<IInstructionSet>();
      private bool m_isAlive = true;

      public event ClientConnectedEventHandler ClientConnected;

      protected internal DtpNode(IClientSource clientSource, IEnumerable<IInstructionSet> instructionSets)
      {
         this.clientSource = clientSource;
         clientSource.SetAcceptCallback(AcceptCallback);
         m_instructionSets.Add(new DefaultInstructionSet());
         if (instructionSets != null)
         {
            foreach (var instructionSet in instructionSets)
               m_instructionSets.Add(instructionSet);
         }
      }

      private void AcceptCallback(Stream stream)
      {
         var session = new DtpNodeSession(this, stream, NodeRole.Server);
         m_sessions.TryAdd(session);
         OnClientConnected(new ClientConnectedEventArgs(session));
      }

      /// <summary>
      /// Connect to another DSPEx node
      /// </summary>
      /// <param name="pipeName">
      /// If null, connects to the default DSPEx pipe ("dargon" aka dargon daemon)
      /// </param>
      /// <returns></returns>
      public IDSPExSession Connect(string pipeName)
      {
         var connection = new NamedPipeClientStream(".", pipeName, PipeDirection.InOut, PipeOptions.Asynchronous | PipeOptions.WriteThrough);
         connection.Connect();

         var session = new DtpNodeSession(this, connection, NodeRole.Client);
         m_sessions.TryAdd(session);

         return session;
      }

      public IDSPExSession Connect(int port)
      {
         var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
         socket.Connect(new IPEndPoint(IPAddress.Loopback, port));

         var session = new DtpNodeSession(this, new NetworkStream(socket), NodeRole.Client);
         m_sessions.TryAdd(session);

         return session;
      }

      public bool TryCreateRemotelyInitializedTransactionHandler(byte opcode, uint transactionId, out RemotelyInitializedTransactionHandler handler)
      {
         foreach (var instructionSet in m_instructionSets) {
            if (instructionSet.TryCreateRemotelyInitializedTransactionHandler(opcode, transactionId, out handler))
               return true;
         }
         handler = null;
         return false;
      }

      public bool IsAlive { get { return m_isAlive; } set { m_isAlive = value; } }

      protected virtual void OnClientConnected(ClientConnectedEventArgs e)
      {
         ClientConnectedEventHandler handler = ClientConnected;
         if (handler != null) handler(this, e);
      }

      public void Shutdown()
      {
         m_isAlive = false;
         clientSource.Shutdown();
         m_sessions.ForEach(s => s.Dispose());
      }
   }
}
