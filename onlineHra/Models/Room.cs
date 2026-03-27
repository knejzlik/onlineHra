namespace onlineHra.Models;

public class Room
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public Dictionary<string, string> Exits { get; set; } = new();
    public List<string> Items { get; set; } = new();
    public List<string> Npcs { get; set; } = new();
}
