using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using libdtp.Tests;

namespace libdtp.DipNodeStandaloneIT
{
   class Program
   {
      static void Main(string[] args)
      {
         var test = new DtpNodeIT();
         test.Setup();
         test.Run();
         Environment.Exit(0);
      }
   }
}
