using System;
using Dargon.Transport.ClientImpl;

namespace Dargon.Transport
{
   public class DefaultInstructionSet : IInstructionSet
   {
      public bool UseConstructionContext { get { return false; } }

      /// <summary>
      /// <see cref="IInstructionSet.UseConstructionContext"/>
      /// </summary>
      public object ConstructionContext { get { throw new InvalidOperationException("Construction Context unavailable"); } }

      public bool TryCreateRemotelyInitializedTransactionHandler(byte opcode, uint transactionId, out RemotelyInitializedTransactionHandler handler)
      {
         handler = null;
         switch ((DTP)opcode)
         {
            case DTP.ECHO: 
               handler = new EchoRith(transactionId);
               break;
         }
         return handler != null;
      }
   }
}
