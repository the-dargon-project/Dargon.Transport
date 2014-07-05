using System;

namespace Dargon.Transport
{
   public interface IDSPExInstructionSet
   {
      /// <summary>
      /// If set to true, any DSPExRITHandlers constructed with types returned from this class
      /// will have ConstructionContext passed as a second argument to their constructors.
      /// </summary>
      bool UseConstructionContext { get; }

      /// <summary>
      /// <see cref="UseConstructionContext"/>
      /// </summary>
      object ConstructionContext { get; }

      Type GetRemotelyInitializedTransactionHandlerType(byte opcode);
   }
}
