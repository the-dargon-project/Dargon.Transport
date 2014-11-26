using Dargon.Networking;
using System;
using System.IO;
using System.IO.Pipes;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using Logger = Dargon.Transport.__DummyLoggerThisIsHorrible;

namespace Dargon.Transport
{
   public interface IClientSource
   {
      void SetAcceptCallback(Action<Stream> acceptCallback);
      void Shutdown();
   }

   public abstract class ClientSourceBase : IClientSource
   {
      private readonly CancellationTokenSource cancellationTokenSource;
      private readonly CancellationToken cancellationToken;
      private readonly Thread acceptThread;
      private readonly CountdownEvent acceptCallbackIsSetSignal = new CountdownEvent(1);
      private Action<Stream> acceptCallback = null;
      private CountdownEvent readySignal;

      protected ClientSourceBase()
      {
         this.cancellationTokenSource = new CancellationTokenSource();
         this.cancellationToken = cancellationTokenSource.Token;

         readySignal = new CountdownEvent(1);
         this.acceptThread = new Thread(() => ThreadStart(readySignal));
      }

      protected void StartAccepting()
      {
         acceptThread.Start();
         readySignal.Wait(cancellationToken);
      }

      protected CancellationToken CancellationToken { get { return cancellationToken; } }

      public void SetAcceptCallback(Action<Stream> acceptCallback)
      {
         this.acceptCallback = acceptCallback;
         acceptCallbackIsSetSignal.Signal();
      }

      protected abstract void ThreadStart(CountdownEvent readySignal);
      protected abstract void ShutdownHelper();

      protected void HandleAccept(Stream stream)
      {
         acceptCallbackIsSetSignal.Wait(cancellationToken);
         acceptCallback(stream);
      }

      public void Shutdown()
      {
         this.cancellationTokenSource.Cancel();
         ShutdownHelper();
         acceptThread.Join();
      }
   }

   public sealed class SocketClientSource : ClientSourceBase
   {
      private readonly int port;
      private readonly Socket listener;
      private readonly Semaphore acceptThreadSynchronization = new Semaphore(0, int.MaxValue);

      public SocketClientSource(int port)
      {
         this.port = port;
         
         listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
         listener.Bind(new IPEndPoint(IPAddress.Any, port));
         listener.Listen(1000);

         StartAccepting();
      }

      protected override void ThreadStart(CountdownEvent readySignal)
      {
         try {
            while (!CancellationToken.IsCancellationRequested) {
               if (readySignal != null) {
                  readySignal.Signal();
                  readySignal = null;
               }
               var asyncResult = listener.BeginAccept(ar => acceptThreadSynchronization.Release(1), null);
               acceptThreadSynchronization.WaitOne();

               if (asyncResult.IsCompleted) {
                  var socket = listener.EndAccept(asyncResult);
                  HandleAccept(new NetworkStream(socket, true));
               } else {
                  listener.Dispose();
               }
            }
         } finally {
            readySignal.Signal();
         }
      }

      protected override void ShutdownHelper() { acceptThreadSynchronization.Release(1); }
   }

   public sealed class NullClientSource : IClientSource
   {
      public void SetAcceptCallback(Action<Stream> acceptCallback) { }
      public void Shutdown() { }
   }

   public sealed class NamedPipeClientSource : ClientSourceBase
   {
      private readonly string pipeName;
      private readonly Semaphore acceptThreadSynchronization = new Semaphore(0, int.MaxValue);

      public NamedPipeClientSource(string pipeName)
      {
         this.pipeName = pipeName;

         StartAccepting();
      }

      protected override void ThreadStart(CountdownEvent readySignal)
      {
         try {
            while (!CancellationToken.IsCancellationRequested) {
               var pipeHandle = LowIntegrityPipeFactory.CreateLowIntegrityNamedPipe(pipeName);
               var connection = new NamedPipeServerStream(PipeDirection.InOut, true, false, pipeHandle);
               if (readySignal != null) {
                  readySignal.Signal();
                  readySignal = null;
               }
               var asyncResult = connection.BeginWaitForConnection(ar => acceptThreadSynchronization.Release(1), null);
               acceptThreadSynchronization.WaitOne();
               Console.WriteLine("Past acceptThreadSynchronization!");

               if (asyncResult.IsCompleted) {
                  connection.EndWaitForConnection(asyncResult);

                  Logger.L(LoggerLevel.Info, "DSPEx Node got connection");
                  
                  HandleAccept(connection);
               } else {
                  connection.Dispose();
                  pipeHandle.Dispose();
               }
            }
         } finally {
            if (readySignal != null) {
               readySignal.Signal();
            }
         }
      }

      protected override void ShutdownHelper() { acceptThreadSynchronization.Release(1); }
   }
}
