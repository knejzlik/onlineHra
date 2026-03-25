using System.Net.Sockets;
using System.Text;

namespace onlineHra.Commands
{
    internal class HelpCommand : ICommand
    {
        public async Task<string> Execute(TcpClient client)
        {
            var sb = new StringBuilder();

            sb.AppendLine("=== HELP ===");
            sb.AppendLine("List of commands:");
            sb.AppendLine();

            sb.AppendLine("go <direction/room>");
            sb.AppendLine("  - move between rooms");

            sb.AppendLine("explore");
            sb.AppendLine("  - shows information about the current room");

            sb.AppendLine("inventory");
            sb.AppendLine("  - displays the contents of your inventory");

            sb.AppendLine("take <item>");
            sb.AppendLine("  - picks up an item from the room");

            sb.AppendLine("drop <item>");
            sb.AppendLine("  - drops an item into the room");

            sb.AppendLine("talk <name>");
            sb.AppendLine("  - interact with an NPC character");

            sb.AppendLine("help");
            sb.AppendLine("  - displays this help");
            
            return sb.ToString();
        }
    }
}