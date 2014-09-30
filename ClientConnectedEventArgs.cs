namespace Dargon.Transport
{
   public class ClientConnectedEventArgs
   {
      private readonly DtpNodeSession session;

      public ClientConnectedEventArgs(DtpNodeSession session) { this.session = session; }

      public DtpNodeSession Session { get { return session; } }
   }

   public delegate void ClientConnectedEventHandler(DtpNode sender, ClientConnectedEventArgs e);
}