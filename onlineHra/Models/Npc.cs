namespace onlineHra.Models;

public class Npc
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public List<DialogResponse> Dialogs { get; set; } = new();
    public int Hp { get; set; } = 0;
    public int Attack { get; set; } = 0;
    public bool IsDead { get; set; } = false;
    public string BlocksExit { get; set; } = "";
}

public class DialogResponse
{
    public string Trigger { get; set; } = "";
    public string Response { get; set; } = "";
}