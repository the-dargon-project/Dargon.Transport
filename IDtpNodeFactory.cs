using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dargon.Transport
{
   public interface IDtpNodeFactory
   {
      IDtpNode CreateNode(NodeRole role, string pipeName, IEnumerable<IInstructionSet> instructionSets = null);
      IDtpNode CreateNode(NodeRole role, int port = -1, IEnumerable<IInstructionSet> instructionSets = null);
   }
}
