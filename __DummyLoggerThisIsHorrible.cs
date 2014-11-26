using System;

namespace Dargon.Transport
{
   public static class __DummyLoggerThisIsHorrible
   {
      public static void L(LoggerLevel ll, params object[] herps) {
//         if (ll == LoggerLevel.Error)
//            Console.ForegroundColor = ConsoleColor.Red;
//         else if (ll == LoggerLevel.Info)
//            Console.ForegroundColor = ConsoleColor.Yellow;
//
//         foreach (var i in herps)
//            Console.WriteLine(herps);
      }
   }

   public enum LoggerLevel
   {
      Info,
      Error
   }
}
