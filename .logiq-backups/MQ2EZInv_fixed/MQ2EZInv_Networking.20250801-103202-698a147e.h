#pragma once

#include <string>
#include <vector>
#include <memory>
#include <chrono>
#include <unordered_map>

// Forward declarations
struct InventoryData;
struct ItemData;

/// <summary>
/// Direct networking integration with E3Next's NetMQ system using named pipes
/// This replaces file-based communication with direct pipe messaging
/// </summary>
class E3NextDirectNetworking
{
private:
    // Pipe handle and state - must come before m_characterName for proper initialization
    HANDLE m_hPipe;
    std::string m_pipeName;
    bool m_pipeConnected;
    
    std::string m_characterName;
    std::string m_serverName;
    bool m_isInitialized;
    std::chrono::steady_clock::time_point m_lastPublish;
    static constexpr auto PUBLISH_INTERVAL = std::chrono::seconds(5);

    // Cache for received inventory data
    std::unordered_map<std::string, InventoryData> m_receivedInventories;
    std::unordered_map<std::string, std::chrono::steady_clock::time_point> m_lastUpdateTimes;

public:
    // Getter for pipe connection status
    bool IsPipeConnected() const { return m_pipeConnected; }

public:
    E3NextDirectNetworking();
    ~E3NextDirectNetworking();

    // Initialize the networking system
    bool Initialize();

    // Shutdown the networking system
    void Shutdown();

    // Publish local inventory data to E3Next network via named pipe
    bool PublishInventory(const InventoryData& inventory);

    // Process network messages (call from main loop)
    void ProcessUpdates();

    // Get cached inventory data for a character
    bool GetCharacterInventory(const std::string& characterName, InventoryData& inventory) const;

    // Get list of characters with cached inventory data
    std::vector<std::string> GetCharacterNames() const;

    // Check if we have fresh data for a character
    bool HasFreshData(const std::string& characterName, int maxAgeSeconds = 60) const;

    // Request inventory data from all connected clients
    void RequestAllInventories();

    // Status information
    void PrintStatus() const;

private:
    // Helper methods
    bool ShouldPublish() const;
    void UpdateLastPublishTime();
    bool IsCacheExpired(const std::chrono::steady_clock::time_point& timestamp) const;
    void CleanupExpiredCache();
    
    // Named pipe communication helpers
    bool CreateAndConnectPipe();
    bool WriteToPipe(const std::string& jsonData);
    void ClosePipe();
    
    // Data processing
    void ProcessReceivedInventoryData(const std::string& characterName, const std::string& jsonData);
    
    // Pipe connection management
    bool EnsurePipeConnection();
    void ReconnectPipe();
};

// Global instance
extern std::unique_ptr<E3NextDirectNetworking> g_e3DirectNetworking;

// Namespace functions for easy integration
namespace E3DirectNetwork
{
    // Initialize the direct networking system
    bool Initialize();

    // Shutdown the networking system
    void Shutdown();

    // Update processing (call from main loop)
    void Update();

    // Publish inventory data
    bool PublishInventory(const InventoryData& inventory);

    // Get cached inventory data
    bool GetCharacterInventory(const std::string& characterName, InventoryData& inventory);

    // Get list of characters with cached data
    std::vector<std::string> GetCharacterNames();

    // Request all inventories
    void RequestAllInventories();

    // Print status
    void PrintStatus();

    // Check if direct networking is available
    bool IsAvailable();
}