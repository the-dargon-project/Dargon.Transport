using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using ItzWarty;

namespace Dargon.Transport
{
   public class DefaultDtpNodeFactory : IDtpNodeFactory
   {
      private static readonly IInstructionSet[] kNoInstructionSets = new IInstructionSet[0];

      public IDtpNode CreateNode(NodeRole role, string pipeName, IEnumerable<IInstructionSet> instructionSets = null)
      {
         if (role == NodeRole.ServerOrClient) {
            throw new ArgumentException("Cannot use " + role + " with named pipe constructor.");
         } else if (role == NodeRole.Client) {
            if (pipeName != null) {
               throw new InvalidOperationException("Use node.Connect, not CreateNode constructor!");
            }
            return new DtpNode(new NullClientSource(), instructionSets);
         } else {
            return new DtpNode(new NamedPipeClientSource(pipeName), instructionSets);
         }
      }

      public IDtpNode CreateNode(NodeRole role, int port = -1, IEnumerable<IInstructionSet> instructionSets = null)
      {
         instructionSets = instructionSets == null ? kNoInstructionSets : instructionSets.ToArray();
         if (role == NodeRole.ServerOrClient) {
            IDtpNode result = null;
            while (result == null) {
               try {
                  result = CreateNode(NodeRole.Server, port, instructionSets);
               } catch {
                  try {
                     result = CreateNode(NodeRole.Client, -1, instructionSets).With(n => { n.Connect(port); });
                  } catch {
                     // gotta catch em all
                  }
               }
            }
            return result;
         } else if (role == NodeRole.Client) {
            if (port != -1) {
               throw new InvalidOperationException("Use node.Connect, not CreateNode constructor to connect to server!");
            }
            return new DtpNode(new NullClientSource(), instructionSets);
         } else {
            return new DtpNode(new SocketClientSource(port), instructionSets);
         }
      }
   }
}