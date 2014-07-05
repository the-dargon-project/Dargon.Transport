namespace Dargon.Transport
{
   /// <summary>
   /// Dargon Service Protocol Extended allocates a certain opcode range [DSPEx.DSP_EX_GAME_OP_LOW,
   /// DSPEx.DSP_EX_GAME_OP_HIGH] for game implementation operations.
   /// </summary>
   public interface IDSPExGameContext
   {
      /// <summary>
      /// Creates a remotely initialized transaction handler for the given opcode
      /// </summary>
      /// <param name="transactionId">
      ///    Unique identifier associated with the transaction.
      /// </param>
      /// <param name="opcode">
      ///    The opcode associated with the given transaction
      /// </param>
      /// <returns>
      /// The transaction handler, or null if such a transaction handler doesn't exist
      /// </returns>
      DSPExRITransactionHandler CreateAndRegisterRITransactionHandler(uint transactionId, byte opcode);
   }
}
