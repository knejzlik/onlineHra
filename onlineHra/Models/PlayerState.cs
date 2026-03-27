namespace onlineHra.Models;

public class PlayerState
{
    public string Username { get; set; } = "";
    public string PasswordHash { get; set; } = "";
    public string CurrentRoomId { get; set; } = "";
    public List<string> Inventory { get; set; } = new();
    public int MaxInventoryCapacity { get; set; } = 10;
    public bool GameCompleted { get; set; } = false;
    public Dictionary<string, string> Quests { get; set; } = new();
}
