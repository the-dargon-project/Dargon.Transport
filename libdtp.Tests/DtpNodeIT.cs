using System;
using System.Collections.Generic;
using System.Diagnostics;
using Dargon.Transport;
using Dargon.Transport.ClientImpl;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace libdtp.Tests
{
   [TestClass]
   public class DtpNodeIT
   {
      private const string pipeName = "DtpNodeIT";
      private DtpNode m_daemonNode;
      private DtpNode m_dimNode;

      [TestInitialize]
      public void Setup()
      {
         m_daemonNode = DtpNode.CreateNode(true, pipeName);
         m_dimNode = DtpNode.CreateNode(false, pipeName);
      }

      [TestMethod]
      public void Run()
      {
         var stopwatch = new Stopwatch();
         stopwatch.Start();

         var dimSession = m_dimNode.Connect();
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
