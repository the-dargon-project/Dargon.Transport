namespace Dargon.Transport
{
   public interface IDtpNode
   {
      event ClientConnectedEventHandler ClientConnected;
      bool IsAlive { get; set; }

      /// <summary>
      /// Connect to another DSPEx node
      /// </summary>
      /// <param name="pipeName">
      /// If null, connects to the default DSPEx pipe ("dargon" aka dargon daemon)
      /// </param>
      /// <returns></returns>
      IDSPExSession Connect(string pipeName = null);

      void Shutdown();
   }
}