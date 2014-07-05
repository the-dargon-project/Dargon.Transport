using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dargon.Games;
using Dargon.IO.DSP.ClientImpl;

namespace Dargon.IO.DSP
{
   public class DefaultDSPExInstructionSet : IDSPExInstructionSet
   {
      public bool UseConstructionContext { get { return false; } }

      /// <summary>
      /// <see cref="IDSPExInstructionSet.UseConstructionContext"/>
      /// </summary>
      public object ConstructionContext { get { throw new InvalidOperationException("Construction Context unavailable"); } }

      public Type GetRemotelyInitializedTransactionHandlerType(
         byte opcode, 
         DargonGame game)
      {
         switch ((DSPEx)opcode)
         {
            case DSPEx.DSP_EX_C2S_ECHO: return typeof(DSPExRITEchoHandler);
            default: return null;
         }
      }
   }
}
