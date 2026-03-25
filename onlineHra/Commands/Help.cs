using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace onlineHra.Commands
{
    internal class HelpCommand : ICommand
    {
        public string Execute(TcpClient client)
        {
            var sb = new StringBuilder();

            sb.AppendLine("=== NÁPOVĚDA ===");
            sb.AppendLine("Seznam příkazů:");
            sb.AppendLine();

            sb.AppendLine("jdi <směr/místnost>");
            sb.AppendLine("  - pohyb mezi místnostmi");

            sb.AppendLine("prozkoumej");
            sb.AppendLine("  - zobrazí informace o aktuální místnosti");

            sb.AppendLine("inventar");
            sb.AppendLine("  - zobrazí obsah inventáře");

            sb.AppendLine("vezmi <předmět>");
            sb.AppendLine("  - vezme předmět z místnosti");

            sb.AppendLine("odloz <předmět>");
            sb.AppendLine("  - odloží předmět do místnosti");

            sb.AppendLine("mluv <jméno>");
            sb.AppendLine("  - interakce s NPC postavou");

            sb.AppendLine("pomoc");
            sb.AppendLine("  - zobrazí tuto nápovědu");

            return sb.ToString();
        }
    }
}