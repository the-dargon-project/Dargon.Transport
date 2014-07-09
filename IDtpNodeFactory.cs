using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dargon.Transport
{
   public interface IDtpNodeFactory
   {
      DtpNode CreateNode(bool acceptIncomingConnections, string defaultPipeName = "dargon", IEnumerable<IInstructionSet> instructionSets = null);
   }
}
