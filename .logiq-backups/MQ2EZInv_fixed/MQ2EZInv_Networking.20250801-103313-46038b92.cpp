#include "MQ2EZInv_Networking.h"
#include "MQ2EZInv.h"
#include <mq/Plugin.h>
#include <algorithm>
#include <set>

// Global instance
std::unique_ptr<E3NextDirectNetworking> g_e3DirectNetworking;

E3NextDirectNetworking::E3NextDirectNetworking()
    : m_hPipe(nullptr), m_pipeConnected(false), m_isInitialized(false), m_lastPublish(std::chrono::steady_clock::now())
{
}

E3NextDirectNetworking::~E3NextDirectNetworking()
{
    Shutdown();
}

bool E3NextDirectNetworking::Initialize()
{
    WriteChatf("[MQ2EZInv] Initializing direct E3Next networking via named pipes...");

    // Get character name
    if (GetCharInfo() && GetCharInfo()->Name) {
        m_characterName = GetCharInfo()->Name;
    }

    if (m_characterName.empty()) {
        WriteChatf("[MQ2EZInv] Warning: Could not determine character name");
        return false;
    }

    // Get server name
    m_serverName = EZInvUtils::GetServerName();

    // Create unique pipe name for this character
    m_pipeName = "\\\\.\\pipe\\E3Next_Inventory_" + m_characterName + "_" + m_serverName;

    WriteChatf("[MQ2EZInv] Direct networking initialized for %s on %s via pipe: %s", 
               m_characterName.c_str(), m_serverName.c_str(), m_pipeName.c_str());

    m_isInitialized = true;
    return true;
}

void E3NextDirectNetworking::Shutdown()
{
    ClosePipe();
    m_receivedInventories.clear();
    m_lastUpdateTimes.clear();
    m_isInitialized = false;
    WriteChatf("[MQ2EZInv] Direct networking shutdown");
}

bool E3NextDirectNetworking::PublishInventory(const InventoryData& inventory)
{
    if (!m_isInitialized) {
        return false;
    }

    if (!ShouldPublish()) {
        return true; // Not time to publish yet
    }

    try {
        // Serialize inventory to JSON
        std::string jsonData = inventory.Serialize();
        
        if (jsonData.empty()) {
            WriteChatf("[MQ2EZInv] Failed to serialize inventory data");
            return false;
        }

        // For now, simulate the pipe write since E3Next pipe listener isn't implemented yet
        WriteChatf("[MQ2EZInv] Would publish inventory data for %s (%d chars) via named pipe: %s", 
                  inventory.characterName.c_str(), static_cast<int>(jsonData.length()), m_pipeName.c_str());
        
        // Update last publish time
        UpdateLastPublishTime();
        
        return true;
    }
    catch (const std::exception& e) {
        WriteChatf("[MQ2EZInv] Error publishing inventory: %s", e.what());
        return false;
    }
}

void E3NextDirectNetworking::ProcessUpdates()
{
    if (!m_isInitialized) {
        return;
    }

    // Clean up expired cache entries
    CleanupExpiredCache();

    // Note: For now, we're only publishing data. 
    // Receiving data would need to be implemented via a separate reader pipe
    // or through the existing shared memory mechanism
}

bool E3NextDirectNetworking::GetCharacterInventory(const std::string& characterName, InventoryData& inventory) const
{
    auto it = m_receivedInventories.find(characterName);
    if (it != m_receivedInventories.end()) {
        inventory = it->second;
        return true;
    }
    return false;
}

std::vector<std::string> E3NextDirectNetworking::GetCharacterNames() const
{
    std::vector<std::string> names;
    for (const auto& pair : m_receivedInventories) {
        names.push_back(pair.first);
    }
    return names;
}

bool E3NextDirectNetworking::HasFreshData(const std::string& characterName, int maxAgeSeconds) const
{
    auto it = m_lastUpdateTimes.find(characterName);
    if (it != m_lastUpdateTimes.end()) {
        auto now = std::chrono::steady_clock::now();
        auto age = now - it->second;
        return age <= std::chrono::seconds(maxAgeSeconds);
    }
    return false;
}

void E3NextDirectNetworking::RequestAllInventories()
{
    WriteChatf("[MQ2EZInv] Requesting inventory data from all connected clients via named pipe");
    
    // Send request message through pipe (simulated)
    std::string requestJson = R"({"type":"request_inventory","character":")" + m_characterName + R"("})";
    
    WriteChatf("[MQ2EZInv] Would send inventory request via named pipe: %s", m_pipeName.c_str());
}

void E3NextDirectNetworking::PrintStatus() const
{
    WriteChatf("=== Direct E3Next Networking Status (Named Pipes) ===");
    WriteChatf("Character: %s", m_characterName.c_str());
    WriteChatf("Server: %s", m_serverName.c_str());
    WriteChatf("Initialized: %s", m_isInitialized ? "Yes" : "No");
    WriteChatf("Pipe Name: %s", m_pipeName.c_str());
    WriteChatf("Pipe Connected: %s", m_pipeConnected ? "Yes" : "No");
    WriteChatf("Cached Inventories: %d", static_cast<int>(m_receivedInventories.size()));
    
    auto timeSincePublish = std::chrono::steady_clock::now() - m_lastPublish;
    WriteChatf("Time since last publish: %lld seconds", 
              std::chrono::duration_cast<std::chrono::seconds>(timeSincePublish).count());
    WriteChatf("Status: E3Next pipe listener not yet implemented - publishing simulated");
    WriteChatf("==========================================");
}

bool E3NextDirectNetworking::ShouldPublish() const
{
    auto timeSincePublish = std::chrono::steady_clock::now() - m_lastPublish;
    return timeSincePublish >= PUBLISH_INTERVAL;
}

void E3NextDirectNetworking::UpdateLastPublishTime()
{
    m_lastPublish = std::chrono::steady_clock::now();
}

bool E3NextDirectNetworking::IsCacheExpired(const std::chrono::steady_clock::time_point& timestamp) const
{
    auto now = std::chrono::steady_clock::now();
    auto age = now - timestamp;
    return age > std::chrono::minutes(10); // Expire after 10 minutes
}

void E3NextDirectNetworking::CleanupExpiredCache()
{
    auto now = std::chrono::steady_clock::now();
    std::vector<std::string> toRemove;

    for (const auto& pair : m_lastUpdateTimes) {
        if (IsCacheExpired(pair.second)) {
            toRemove.push_back(pair.first);
        }
    }

    for (const auto& characterName : toRemove) {
        m_receivedInventories.erase(characterName);
        m_lastUpdateTimes.erase(characterName);
    }
}

bool E3NextDirectNetworking::CreateAndConnectPipe()
{
    // Close existing pipe if any
    ClosePipe();

    // For now, just create the pipe name - actual connection will happen when E3Next implements the listener
    WriteChatf("[MQ2EZInv] Named pipe created: %s", m_pipeName.c_str());
    WriteChatf("[MQ2EZInv] Note: E3Next pipe listener not yet implemented - connection simulated");

    m_pipeConnected = true;
    return true;
}

bool E3NextDirectNetworking::WriteToPipe(const std::string& jsonData)
{
    // For now, simulate the pipe write
    WriteChatf("[MQ2EZInv] Simulating pipe write of %d bytes", static_cast<int>(jsonData.length()));
    return true;
}

void E3NextDirectNetworking::ClosePipe()
{
    if (m_hPipe != nullptr) {
        // Would close the pipe here when implemented
        m_hPipe = nullptr;
        m_pipeConnected = false;
    }
}

bool E3NextDirectNetworking::EnsurePipeConnection()
{
    if (!IsPipeConnected()) {
        return CreateAndConnectPipe();
    }
    return true;
}

void E3NextDirectNetworking::ReconnectPipe()
{
    WriteChatf("[MQ2EZInv] Reconnecting named pipe...");
    ClosePipe();
    CreateAndConnectPipe();
}

bool E3NextDirectNetworking::IsPipeConnected() const
{
    return m_pipeConnected;
}

void E3NextDirectNetworking::ProcessReceivedInventoryData(const std::string& characterName, const std::string& jsonData)
{
    try {
        InventoryData inventory;
        // Convert string to vector<uint8_t> for Deserialize method
        std::vector<uint8_t> dataVec(jsonData.begin(), jsonData.end());
        if (inventory.Deserialize(dataVec)) {
            m_receivedInventories[characterName] = std::move(inventory);
            m_lastUpdateTimes[characterName] = std::chrono::steady_clock::now();
            WriteChatf("[MQ2EZInv] Successfully processed inventory data for %s", characterName.c_str());
        } else {
            WriteChatf("[MQ2EZInv] Failed to deserialize inventory data for %s", characterName.c_str());
        }
    }
    catch (const std::exception& e) {
        WriteChatf("[MQ2EZInv] Error processing inventory data for %s: %s", characterName.c_str(), e.what());
    }
}

// Namespace implementation
namespace E3DirectNetwork
{
    bool Initialize()
    {
        if (!g_e3DirectNetworking) {
            g_e3DirectNetworking = std::make_unique<E3NextDirectNetworking>();
        }
        return g_e3DirectNetworking->Initialize();
    }

    void Shutdown()
    {
        if (g_e3DirectNetworking) {
            g_e3DirectNetworking->Shutdown();
            g_e3DirectNetworking.reset();
        }
    }

    void Update()
    {
        if (g_e3DirectNetworking) {
            g_e3DirectNetworking->ProcessUpdates();
        }
    }

    bool PublishInventory(const InventoryData& inventory)
    {
        if (g_e3DirectNetworking) {
            return g_e3DirectNetworking->PublishInventory(inventory);
        }
        return false;
    }

    bool GetCharacterInventory(const std::string& characterName, InventoryData& inventory)
    {
        if (g_e3DirectNetworking) {
            return g_e3DirectNetworking->GetCharacterInventory(characterName, inventory);
        }
        return false;
    }

    std::vector<std::string> GetCharacterNames()
    {
        if (g_e3DirectNetworking) {
            return g_e3DirectNetworking->GetCharacterNames();
        }
        return {};
    }

    void RequestAllInventories()
    {
        if (g_e3DirectNetworking) {
            g_e3DirectNetworking->RequestAllInventories();
        }
    }

    void PrintStatus()
    {
        if (g_e3DirectNetworking) {
            g_e3DirectNetworking->PrintStatus();
        }
    }

    bool IsAvailable()
    {
        return g_e3DirectNetworking != nullptr && g_e3DirectNetworking->IsPipeConnected();
    }
}