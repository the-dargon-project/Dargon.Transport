using System;
using System.Collections.Generic;
using System.IO.Pipes;
using System.Threading;
using ItzWarty.Networking;

using Logger = Dargon.Transport.__DummyLoggerThisIsHorrible;

namespace Dargon.Transport
{
   /// <summary>
   /// A node in a Dargon Service Protocol Extended graph
   /// </summary>
   public class DtpNode
   {
      private readonly bool m_acceptIncomingConnections;
      private readonly string m_defaultPipeName;

      public bool IsAlive { get { return m_isAlive; } set { m_isAlive = value; } }
      private bool m_isAlive = true;

      // : server :
      private Thread m_serverThread;

      // : sessions : 
      private List<DtpNodeSession> m_sessions = new List<DtpNodeSession>();
      private object m_sessionsLock = new object();

      private readonly List<IInstructionSet> m_instructionSets = new List<IInstructionSet>();

      protected DtpNode(bool acceptIncomingConnections, string defaultPipeName, IEnumerable<IInstructionSet> instructionSets)
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
            connection.WaitForConnection();
            Logger.L(LoggerLevel.Info, "DSPEx Node got connection");

            var session = new DtpNodeSession(this, connection, DSPExNodeRole.Server);
            lock (m_sessionsLock)
               m_sessions.Add(session);
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
         lock (m_sessionsLock)
            m_sessions.Add(session);

         return session;
      }

      public Type GetRITOpcodeHandlerType(byte opcode)
      {
         foreach (var instructionSet in m_instructionSets)
         {
            var handlerType = instructionSet.GetRemotelyInitializedTransactionHandlerType(opcode);
            if (handlerType != null)
               return handlerType;
         }
         return null;
      }
   }
}
