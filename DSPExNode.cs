using System;
using System.Collections.Generic;
using System.IO.Pipes;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Dargon.Games;
using Dargon.Util;
using Dargon.Util.NamedPipeServer;

namespace Dargon.IO.DSP
{
   /// <summary>
   /// A node in a Dargon Service Protocol Extended graph
   /// </summary>
   public class DSPExNode
   {
      private readonly DSPExNodeRole m_role;
      private readonly string m_defaultPipeName;

      public bool IsAlive { get { return m_isAlive; } set { m_isAlive = value; } }
      private bool m_isAlive = true;

      // : server :
      private Thread m_serverThread;

      // : sessions : 
      private List<DSPExNodeSession> m_sessions = new List<DSPExNodeSession>();
      private object m_sessionsLock = new object();

      private readonly List<IDSPExInstructionSet> m_instructionSets = new List<IDSPExInstructionSet>();

      private DSPExNode(DSPExNodeRole role, string defaultPipeName, IEnumerable<IDSPExInstructionSet> instructionSets)
      {
         m_role = role;
         m_defaultPipeName = defaultPipeName;

         if (role.HasFlag(DSPExNodeRole.Server))
         {
            m_serverThread = new Thread(ServerThreadStart);
            m_serverThread.Start();
         }

         m_instructionSets.Add(new DefaultDSPExInstructionSet());
         if (instructionSets != null)
         {
            foreach (var instructionSet in instructionSets)
               m_instructionSets.Add(instructionSet);
         }
      }
      
      private void ServerThreadStart()
      {
         while (m_isAlive)
         {
            var pipeHandle = LowIntegrityPipeFactory.CreateLowIntegrityNamedPipe(m_defaultPipeName);
            var connection = new NamedPipeServerStream(PipeDirection.InOut, true, false, pipeHandle);
            connection.WaitForConnection();
            Console.WriteLine("DSPEx Node got connection");

            var session = new DSPExNodeSession(this, connection, DSPExNodeRole.Server);
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
         var connection = new NamedPipeClientStream(".", pipeName, PipeDirection.InOut, PipeOptions.Asynchronous | PipeOptions.WriteThrough);
         connection.Connect();

         var session = new DSPExNodeSession(this, connection, DSPExNodeRole.Client);
         lock (m_sessionsLock)
            m_sessions.Add(session);

         return session;
      }

      public Type GetRITOpcodeHandlerType(byte opcode, DargonGame game)
      {
         foreach (var instructionSet in m_instructionSets)
         {
            var handlerType = instructionSet.GetRemotelyInitializedTransactionHandlerType(opcode, game);
            if (handlerType != null)
               return handlerType;
         }
         return null;
      }

      // - Static Factory Methods -----------------------------------------------------------------
      // wtf?
      public static DSPExNode CreateNode(
         DSPExNodeRole role, 
         string defaultPipeName = "dargon",
         IEnumerable<IDSPExInstructionSet> instructionSets = null)
      {
         return new DSPExNode(role, defaultPipeName, instructionSets);
      }
   }
}
