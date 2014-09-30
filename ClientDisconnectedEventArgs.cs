namespace Dargon.Transport
{
   public class ClientDisconnectedEventArgs
   {
      private readonly DtpNodeSession session;

      public ClientDisconnectedEventArgs(DtpNodeSession session) { this.session = session; }

      public DtpNodeSession Session { get { return session; } }
   }

   public delegate void ClientDisconnectedEventHandler(DtpNodeSession sender, ClientDisconnectedEventArgs e);
}