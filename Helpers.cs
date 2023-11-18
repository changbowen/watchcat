using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace watchcat
{
    internal class Helpers
    {
        public static void ConsoleWrite(string message, ConsoleColor? fg = null, ConsoleColor? bg = null)
        {
            ConsoleColor? oldFg = null;
            ConsoleColor? oldBg = null;
            if (fg != null) {
                oldFg = Console.ForegroundColor;
                Console.ForegroundColor = fg.Value;
            }
            if (bg != null) {
                oldBg = Console.BackgroundColor;
                Console.BackgroundColor = bg.Value;
            }
            
            Console.WriteLine(message);

            if (oldFg != null) Console.ForegroundColor = oldFg.Value;
            if (oldBg != null) Console.BackgroundColor = oldBg.Value;
        }
    }
}
