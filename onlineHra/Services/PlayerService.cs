using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using onlineHra.Models;

namespace onlineHra.Services;

public class PlayerService
{
    private readonly string _filePath;
    private readonly Dictionary<string, PlayerState> _players = new();

    public PlayerService(string filePath = "Data/players.json")
    {
        _filePath = filePath;
        LoadPlayers();
    }

    private void LoadPlayers()
    {
        if (File.Exists(_filePath))
        {
            var json = File.ReadAllText(_filePath);
            var players = JsonSerializer.Deserialize<List<PlayerState>>(json);
            if (players != null)
            {
                foreach (var player in players)
                {
                    _players[player.Username.ToLower()] = player;
                }
            }
        }
    }

    private void SavePlayers()
    {
        var directory = Path.GetDirectoryName(_filePath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var json = JsonSerializer.Serialize(_players.Values, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(_filePath, json);
    }

    public string HashPassword(string password)
    {
        using var sha256 = SHA256.Create();
        var bytes = Encoding.UTF8.GetBytes(password);
        var hash = sha256.ComputeHash(bytes);
        return Convert.ToBase64String(hash);
    }

    public bool Register(string username, string password)
    {
        if (_players.ContainsKey(username.ToLower()))
        {
            return false;
        }

        var playerState = new PlayerState
        {
            Username = username,
            PasswordHash = HashPassword(password),
            CurrentRoomId = "start"
        };

        _players[username.ToLower()] = playerState;
        SavePlayers();
        return true;
    }

    public PlayerState? Login(string username, string password)
    {
        if (!_players.TryGetValue(username.ToLower(), out var player))
        {
            return null;
        }

        var hashedPassword = HashPassword(password);
        if (player.PasswordHash != hashedPassword)
        {
            return null;
        }

        return player;
    }

    public void SavePlayer(PlayerState player)
    {
        _players[player.Username.ToLower()] = player;
        SavePlayers();
    }

    public PlayerState? GetPlayer(string username)
    {
        return _players.TryGetValue(username.ToLower(), out var player) ? player : null;
    }
}
