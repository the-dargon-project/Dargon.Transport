using System;
using System.Collections.Generic;
using System.IO.Pipes;
using System.Threading;
using System.Threading.Tasks;
using ItzWarty.Collections;
using ItzWarty.Networking;

using Logger = Dargon.Transport.__DummyLoggerThisIsHorrible;

namespace Dargon.Transport
{
   /// <summary>
   /// A node in a Dargon Service Protocol Extended graph
   /// </summary>
   public class DtpNode : IDtpNode
   {
      private readonly bool m_acceptIncomingConnections;
      private readonly string m_defaultPipeName;

      private bool m_isAlive = true;
      private readonly Thread m_serverThread;
      private readonly ConcurrentSet<DtpNodeSession> m_sessions = new ConcurrentSet<DtpNodeSession>();
      private readonly List<IInstructionSet> m_instructionSets = new List<IInstructionSet>();

      private readonly Semaphore acceptThreadSynchronization = new Semaphore(0, int.MaxValue);

      public event ClientConnectedEventHandler ClientConnected;

      protected internal DtpNode(bool acceptIncomingConnections, string defaultPipeName, IEnumerable<IInstructionSet> instructionSets)
      {
         m_acceptIncomingConnections = acceptIncomingConnections;
         m_defaultPipeName = defaultPipeName;

         if (acceptIncomingConnections)
         {
            var signalledOnWaitingForConnection = new CountdownEvent(1);
            m_serverThread = new Thread(delegate() { ServerThreadStart(signalledOnWaitingForConnection); });
            m_serverThread.Start();
            signalledOnWaitingForConnection.Wait();
         }

         m_instructionSets.Add(new DefaultInstructionSet());
         if (instructionSets != null)
         {
            foreach (var instructionSet in instructionSets)
               m_instructionSets.Add(instructionSet);
         }
      }
      
      private void ServerThreadStart(CountdownEvent signalledOnWaitingForConnection)
      {
         while (m_isAlive)
         {
            var pipeHandle = LowIntegrityPipeFactory.CreateLowIntegrityNamedPipe(m_defaultPipeName);
            var connection = new NamedPipeServerStream(PipeDirection.InOut, true, false, pipeHandle);
            if (signalledOnWaitingForConnection != null)
            {
               signalledOnWaitingForConnection.Signal();
               signalledOnWaitingForConnection = null;
            }
            var asyncResult = connection.BeginWaitForConnection(ar => acceptThreadSynchronization.Release(1), null);
            acceptThreadSynchronization.WaitOne();
            Console.WriteLine("Past acceptThreadSynchronization!");

            if (asyncResult.IsCompleted) {
               connection.EndWaitForConnection(asyncResult);

               Logger.L(LoggerLevel.Info, "DSPEx Node got connection");

               var session = new DtpNodeSession(this, connection, DSPExNodeRole.Server);
               m_sessions.TryAdd(session);

               OnClientConnected(new ClientConnectedEventArgs(session));
            } else {
               connection.Dispose();
               pipeHandle.Dispose();
            }
         }
      }

      /// <summary>
      /// Connect to another DSPEx node
      /// </summary>
      /// <param name="pipeName">
      /// If null, connects to the default DSPEx pipe ("dargon" aka dargon daemon)
      /// </param>
      /// <returns></returns>
      public IDSPExSession Connect(string pipeName = null)
      {
         var connection = new NamedPipeClientStream(".", pipeName ?? m_defaultPipeName, PipeDirection.InOut, PipeOptions.Asynchronous | PipeOptions.WriteThrough);
         connection.Connect();

         var session = new DtpNodeSession(this, connection, DSPExNodeRole.Client);
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
         acceptThreadSynchronization.Release();
      }
   }
}
