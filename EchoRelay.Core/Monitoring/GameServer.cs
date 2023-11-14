﻿using Newtonsoft.Json;

namespace EchoRelay.Core.Monitoring;

public class GameServer
{
    private readonly ApiClient _apiClient;

    public GameServer(ApiClient apiClient)
    {
        _apiClient = apiClient;
    }

    public async Task DeleteGameServerAsync(string sessionId)
    {
        // Define the endpoint for deleting a game server with the sessionID
        string endpoint = $"deleteGameServer/{sessionId}";

        try
        {
            await _apiClient.DeleteMonitoringData(endpoint);
            Console.WriteLine($"Game server with session ID {sessionId} deleted successfully from monitoring.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error deleting the game server from monitoring: {ex.Message}");
        }
    }

    public async Task AddGameServerAsync(GameServerObject jsonObject)
    {
        // Create a StringContent with the JSON data and set the content type
        string jsonData = JsonConvert.SerializeObject(jsonObject);
        string endpoint = "addGameServer/";

        try
        {
            await _apiClient.PostMonitoringData(endpoint, jsonData);
            Console.WriteLine($"Game server successfully added to monitoring.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error adding the game server from monitoring: {ex.Message}");
        }
    }
    
    public async Task EditGameServer(GameServerObject jsonObject, string gameServerID)
    {
        // Create a StringContent with the JSON data and set the content type
        string jsonData = JsonConvert.SerializeObject(jsonObject);
        string endpoint = $"updateGameServer/{gameServerID}";

        try
        {
            await _apiClient.PostMonitoringData(endpoint, jsonData);
            Console.WriteLine($"Game server successfully edited in monitoring.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error editing the game server in monitoring: {ex.Message}");
        }
    }
}