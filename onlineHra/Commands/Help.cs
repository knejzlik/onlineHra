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

            sb.AppendLine("go <direction>");
            sb.AppendLine("  - move between rooms (north/south/east/west or n/s/e/w)");

            sb.AppendLine("explore");
            sb.AppendLine("  - shows information about the current room including players, NPCs, and items");

            sb.AppendLine("inventory");
            sb.AppendLine("  - displays the contents of your inventory");

            sb.AppendLine("take <item>");
            sb.AppendLine("  - picks up an item from the room");

            sb.AppendLine("drop <item>");
            sb.AppendLine("  - drops an item into the room");

            sb.AppendLine("talk <character|player> [topic]");
            sb.AppendLine("  - talk to NPCs (anywhere in dungeon) or other players");
            sb.AppendLine("  - NPCs show available topics with hints after talking");

            sb.AppendLine("say <message>");
            sb.AppendLine("  - send a message to all players in the same room");

            sb.AppendLine("whisper <player> <message>");
            sb.AppendLine("  - send a private message to one player anywhere in the dungeon");

            sb.AppendLine("broadcast <message>");
            sb.AppendLine("  - send a message to ALL players in the entire dungeon");

            sb.AppendLine("help");
            sb.AppendLine("  - displays this help");
            
            return await Task.FromResult(sb.ToString());
        }
    }
}