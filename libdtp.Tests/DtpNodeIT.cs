using Dargon.Transport;
using Dargon.Transport.ClientImpl;
using NMockito;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using Xunit;

namespace libdtp.Tests
{
   public class DtpNodeIT : NMockitoInstance
   {
      private const string pipeName = "DtpNodeIT";
      private IDtpNode m_daemonNode;
      private IDtpNode m_dimNode;

      public DtpNodeIT()
      {
         IDtpNodeFactory dtpNodeFactory = new DefaultDtpNodeFactory();
         m_daemonNode = dtpNodeFactory.CreateNode(NodeRole.Server, pipeName);
         m_dimNode = dtpNodeFactory.CreateNode(NodeRole.Client);
      }

      [Fact]
      public void Run()
      {
         Thread.Sleep(1000);
         var stopwatch = new Stopwatch();
         stopwatch.Start();

         var dimSession = m_dimNode.Connect(pipeName);
         var echoTransactions = new List<EchoLith>();
         for (var i = 0; i < 100000; i++)
         {
            var echoTransaction = new EchoLith(dimSession.TakeLocallyInitializedTransactionId(), Guid.NewGuid().ToByteArray());
            dimSession.RegisterAndInitializeLITransactionHandler(echoTransaction);
            echoTransactions.Add(echoTransaction);
         }
         Console.WriteLine(stopwatch.ElapsedMilliseconds + "ms: started " + echoTransactions.Count + " echo transactions"); 

         //foreach (var echoTransaction in echoTransactions)
         for (var i = 0; i < echoTransactions.Count; i++)
         {
            var echoTransaction = echoTransactions[i];
            echoTransaction.CompletionCountdownEvent.Wait();
         }
         Console.WriteLine(stopwatch.ElapsedMilliseconds + "ms: finished " + echoTransactions.Count + " echo transactions"); 
      }
   }
}
