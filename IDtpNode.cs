namespace Dargon.Transport
{
   public interface IDtpNode
   {
      event ClientConnectedEventHandler ClientConnected;
      bool IsAlive { get; set; }
      IDSPExSession Connect(string pipeName);
      IDSPExSession Connect(int port);
      void Shutdown();
   }
}