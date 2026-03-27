using System.Text.Json;
using onlineHra.Models;

namespace onlineHra.Services;

public class WorldService
{
    private Dictionary<string, Room> _rooms = new();
    private Dictionary<string, Item> _items = new();
    private Dictionary<string, Npc> _npcs = new();
    private readonly string _dataPath;

    public WorldService(string dataPath = "Data")
    {
        _dataPath = dataPath;
        LoadWorld();
    }

    private void LoadWorld()
    {
        var roomsFile = Path.Combine(_dataPath, "rooms.json");
        var itemsFile = Path.Combine(_dataPath, "items.json");
        var npcsFile = Path.Combine(_dataPath, "npcs.json");

        if (File.Exists(roomsFile))
        {
            var json = File.ReadAllText(roomsFile);
            var rooms = JsonSerializer.Deserialize<List<Room>>(json);
            if (rooms != null)
            {
                foreach (var room in rooms)
                {
                    _rooms[room.Id] = room;
                }
            }
        }

        if (File.Exists(itemsFile))
        {
            var json = File.ReadAllText(itemsFile);
            var items = JsonSerializer.Deserialize<List<Item>>(json);
            if (items != null)
            {
                foreach (var item in items)
                {
                    _items[item.Id] = item;
                }
            }
        }

        if (File.Exists(npcsFile))
        {
            var json = File.ReadAllText(npcsFile);
            var npcs = JsonSerializer.Deserialize<List<Npc>>(json);
            if (npcs != null)
            {
                foreach (var npc in npcs)
                {
                    _npcs[npc.Id] = npc;
                }
            }
        }
    }

    public Room? GetRoom(string roomId)
    {
        return _rooms.TryGetValue(roomId, out var room) ? room : null;
    }

    public Item? GetItem(string itemId)
    {
        return _items.TryGetValue(itemId, out var item) ? item : null;
    }

    public Npc? GetNpc(string npcId)
    {
        return _npcs.TryGetValue(npcId, out var npc) ? npc : null;
    }

    public IEnumerable<Room> GetAllRooms() => _rooms.Values;
}
