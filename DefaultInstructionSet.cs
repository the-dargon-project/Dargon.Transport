using System;
using Dargon.Transport.ClientImpl;

namespace Dargon.Transport
{
   public class DefaultInstructionSet : IDSPExInstructionSet
   {
      public bool UseConstructionContext { get { return false; } }

      /// <summary>
      /// <see cref="IDSPExInstructionSet.UseConstructionContext"/>
      /// </summary>
      public object ConstructionContext { get { throw new InvalidOperationException("Construction Context unavailable"); } }

      public Type GetRemotelyInitializedTransactionHandlerType(
         byte opcode)
      {
         switch ((DTPEx)opcode)
         {
            case DTPEx.C2S_ECHO: return typeof(EchoRith);
            default: return null;
         }
      }
   }
}
