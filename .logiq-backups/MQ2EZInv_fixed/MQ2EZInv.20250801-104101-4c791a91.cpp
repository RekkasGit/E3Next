#include <mq/Plugin.h>
#include <sstream>
#include <thread>
#include <chrono>
#include "MQ2EZInv.h"
#include "MQ2EZInv_ActorManager.h"
#include "MQ2EZInv_E3Integration.h"  // E3Next integration
#include "MQ2EZInv_Networking.h"     // Direct network integration
#include "ItemSuggestionManager.h"

PreSetup("MQ2EZInv");
PLUGIN_VERSION(1.0);

// Global instances
std::unique_ptr<InventoryManager> g_inventoryManager;
std::unique_ptr<SharedMemoryManager> g_sharedMemory;
std::unique_ptr<InventoryUI> g_inventoryUI;
std::unique_ptr<InventoryActorManager> g_actorManager; // Actor system
std::unique_ptr<PerformanceMonitor> g_performanceMonitor;
std::unique_ptr<ItemSuggestionManager> g_itemSuggestionManager;
std::unique_ptr<E3NextDirectNetworking> g_e3DirectNetwork; // Direct network integration

// Plugin command handlers
void EZInvCommand(PlayerClient* pChar, const char* szLine) {
	if (!g_inventoryUI) return;

	std::string args = szLine ? szLine : "";

	if (args.empty() || args == "ui") {
		g_inventoryUI->ToggleVisible();
	}
	else if (args == "scan") {
		if (g_inventoryManager) {
			g_inventoryManager->ForceRefresh();
		}
	}
	else if (args == "banktest" || args == "scanbank") {
		// Debug command to specifically test bank scanning
		if (g_inventoryManager) {
			WriteChatf("[MQ2EZInv] Testing bank scanning...");
			WriteChatf("[MQ2EZInv] Bank window open: %s", (pBankWnd && pBankWnd->IsVisible()) ? "YES" : "NO");
			WriteChatf("[MQ2EZInv] pLocalPC valid: %s", pLocalPC ? "YES" : "NO");
			
			if (pLocalPC) {
				// Test direct access to bank containers
				auto& bankItems = pLocalPC->BankItems;
				auto& sharedBankItems = pLocalPC->SharedBankItems;
				
				WriteChatf("[MQ2EZInv] Testing bank items container access...");
				int bankItemCount = 0;
				for (int i = 0; i < 24; i++) {
					auto pItem = bankItems.GetItem(i);
					if (pItem && pItem->GetID() > 0) {
						bankItemCount++;
						WriteChatf("[MQ2EZInv] Bank slot %d: %s (ID: %d)", i+1, 
							pItem->GetName() ? pItem->GetName() : "Unknown", pItem->GetID());
					}
				}
				
				WriteChatf("[MQ2EZInv] Found %d items in bank container", bankItemCount);
				
				int sharedBankItemCount = 0;
				for (int i = 0; i < 4; i++) {
					auto pItem = sharedBankItems.GetItem(i);
					if (pItem && pItem->GetID() > 0) {
						sharedBankItemCount++;
						WriteChatf("[MQ2EZInv] Shared bank slot %d: %s (ID: %d)", i+1, 
							pItem->GetName() ? pItem->GetName() : "Unknown", pItem->GetID());
					}
				}
				
				WriteChatf("[MQ2EZInv] Found %d items in shared bank container", sharedBankItemCount);
				
				// Now force a full bank scan
				WriteChatf("[MQ2EZInv] Forcing bank scan through InventoryManager...");
				g_inventoryManager->ForceRefresh();
				
				// Report results
				auto& inventory = g_inventoryManager->GetCurrentInventory();
				// WriteChatf("[MQ2EZInv] Bank scan complete: %d items found in inventory bank data", static_cast<int>(inventory.bank.size()));
			}
		}
	}
	else if (args == "sync") {
		// Force inventory sync with all peers
		if (g_actorManager && g_actorManager->IsInitialized()) {
			if (g_inventoryManager) {
				g_actorManager->PublishInventory(g_inventoryManager->GetCurrentInventory());
			}
			g_actorManager->RequestAllInventories();
			WriteChatf("[MQ2EZInv] Forcing inventory sync with all peers via shared memory");
		}
		else {
			WriteChatf("[MQ2EZInv] Actor system not initialized - cannot sync via shared memory");
		}
	}
	else if (args == "pubnet") {
		// Force publish via direct network
		if (g_e3DirectNetwork && g_inventoryManager) {
			if (g_e3DirectNetwork->PublishInventory(g_inventoryManager->GetCurrentInventory())) {
				WriteChatf("[MQ2EZInv] Published inventory via direct network (named pipes)");
			} else {
				WriteChatf("[MQ2EZInv] Failed to publish via direct network");
			}
		} else {
			WriteChatf("[MQ2EZInv] Direct network not available");
		}
	}
	else if (args == "peers") {
		// Show connected peers
		if (g_actorManager && g_actorManager->IsInitialized()) {
			auto peers = g_actorManager->GetConnectedPeers();
			WriteChatf("[MQ2EZInv] Using shared memory communication");
			WriteChatf("[MQ2EZInv] Connected peers (%d):", static_cast<int>(peers.size()));
			for (const auto& peer : peers) {
				WriteChatf("  - %s", peer.c_str());
			}
		}
		else {
			WriteChatf("[MQ2EZInv] Actor system not initialized");
		}
		
		
	}
	else if (args == "discover") {
		// Test E3Next character discovery
		WriteChatf("[MQ2EZInv] Testing E3Next character discovery...");
		auto characters = E3Integration::DiscoverE3NextCharacters();
		WriteChatf("[MQ2EZInv] Discovery complete. Found %d characters:", static_cast<int>(characters.size()));
		for (const auto& character : characters) {
			WriteChatf("  - %s", character.c_str());
		}
	}
	else if (args.substr(0, 4) == "test") {
		// NEW: Test trade system
		std::string testArgs = args.length() > 5 ? args.substr(5) : "";
		if (testArgs.empty()) {
			WriteChatf("[MQ2EZInv] Usage: /ezinv test <character_name>");
			return;
		}
		
		if (g_actorManager && g_actorManager->IsInitialized()) {
			WriteChatf("[MQ2EZInv] Testing trade to %s", testArgs.c_str());
			g_actorManager->InitiateTrade("Test Item", EZInvUtils::GetCharacterName(), testArgs, false);
		}
		else {
			WriteChatf("[MQ2EZInv] Actor system not initialized - cannot test");
		}
	}
	else if (args.substr(0, 4) == "bank") {
		// Bank flag commands
		std::string bankArgs = args.length() > 5 ? args.substr(5) : "";
		if (bankArgs.empty()) {
			WriteChatf("[MQ2EZInv] Usage: /ezinv bank <item_name|item_id>");
			WriteChatf("  /ezinv bank list - List all bank flagged items");
			WriteChatf("  /ezinv bank clear - Clear all bank flags");
			WriteChatf("  /ezinv bank auto - Auto-bank flagged items (bank window must be open)");
			return;
		}
		
		if (bankArgs == "list") {
			auto flaggedItems = g_inventoryManager->GetBankFlaggedItems();
			WriteChatf("[MQ2EZInv] Bank flagged items (%d):", static_cast<int>(flaggedItems.size()));
			for (const auto& item : flaggedItems) {
				WriteChatf("  - %s (ID: %d)", item.name.c_str(), item.id);
			}
		}
		else if (bankArgs == "clear") {
			// Clear all bank flags
			g_inventoryManager->ClearAllBankFlags();
			WriteChatf("[MQ2EZInv] Cleared all bank flags");
		}
		else if (bankArgs == "auto") {
			g_inventoryManager->AutoBankFlaggedItems();
		}
		else {
			// Toggle bank flag for specific item
			int itemId = 0;
			if (IsNumber(bankArgs.c_str())) {
				itemId = atoi(bankArgs.c_str());
			} else {
				// Find item by name
				auto& inventory = g_inventoryManager->GetCurrentInventory();
				for (const auto& item : inventory.equipped) {
					if (item.name == bankArgs) {
						itemId = item.id;
						break;
					}
				}
				if (itemId == 0) {
					for (const auto& bagPair : inventory.bags) {
						for (const auto& item : bagPair.second) {
							if (item.name == bankArgs) {
								itemId = item.id;
								break;
							}
						}
						if (itemId != 0) break;
					}
				}
			}
			
			if (itemId > 0) {
				g_inventoryManager->ToggleItemBankFlag(itemId);
			} else {
				WriteChatf("[MQ2EZInv] Item not found: %s", bankArgs.c_str());
			}
		}
	}
	else if (args == "help") {
		WriteChatf("[MQ2EZInv] Commands:");
		WriteChatf("  /ezinv or /ezinv ui - Toggle inventory window");
		WriteChatf("  /ezinv scan - Force inventory scan");
		WriteChatf("  /ezinv banktest - Debug bank scanning (test command)");
		WriteChatf("  /ezinv sync - Force sync with all peers (shared memory)");
		WriteChatf("  /ezinv pubnet - Force publish via direct network (named pipes)");
		WriteChatf("  /ezinv peers - Show connected peers");
		WriteChatf("  /ezinv bank <item> - Toggle bank flag for item");
		WriteChatf("  /ezinv bank list - List bank flagged items");
		WriteChatf("  /ezinv bank auto - Auto-bank flagged items");
		WriteChatf("  /ezinv broadcast - Start Lua script on all peers");
		WriteChatf("  /ezinv help - Show this help");
	}
	else {
		WriteChatf("[MQ2EZInv] Unknown command: %s", args.c_str());
	}
}

void EZInvStatsCommand(PlayerClient* pChar, const char* szLine) {
	if (!g_inventoryManager) return;

	std::string args = szLine ? szLine : "";
	auto& config = g_inventoryManager->GetConfig();

	if (args.empty()) {
		WriteChatf("[MQ2EZInv] Current stats mode: %s", config.statsLoadingMode.c_str());
		WriteChatf("  Basic stats: %s", config.loadBasicStats ? "enabled" : "disabled");
		WriteChatf("  Detailed stats: %s", config.loadDetailedStats ? "enabled" : "disabled");
	}
	else if (args == "minimal" || args == "selective" || args == "full") {
		config.statsLoadingMode = args;

		if (args == "minimal") {
			config.loadBasicStats = false;
			config.loadDetailedStats = false;
		}
		else if (args == "selective") {
			config.loadBasicStats = true;
			config.loadDetailedStats = false;
		}
		else if (args == "full") {
			config.loadBasicStats = true;
			config.loadDetailedStats = true;
		}

		g_inventoryManager->SetConfig(config);

		// NEW: Broadcast config update to peers
		if (g_actorManager && g_actorManager->IsInitialized()) {
			g_actorManager->UpdateConfig(config);
			g_actorManager->BroadcastConfigUpdate();
		}

		WriteChatf("[MQ2EZInv] Stats loading mode set to: %s", args.c_str());
	}
	else {
		WriteChatf("[MQ2EZInv] Usage: /ezinvstats [minimal|selective|full]");
		WriteChatf("  minimal: Only essential item data (fastest)");
		WriteChatf("  selective: Basic stats like AC, HP, Mana (balanced)");
		WriteChatf("  full: All item statistics (complete)");
	}
}

void EZInvConfigCommand(PlayerClient* pChar, const char* szLine) {
	if (!g_inventoryManager) return;

	std::string args = szLine ? szLine : "";

	if (args == "save") {
		g_inventoryManager->SaveConfig();
		WriteChatf("[MQ2EZInv] Configuration saved");
	}
	else if (args == "reload") {
		g_inventoryManager->LoadConfig();
		WriteChatf("[MQ2EZInv] Configuration reloaded");
	}
	else if (args == "sync") {
		// NEW: Sync config with all peers
		if (g_actorManager && g_actorManager->IsInitialized()) {
			g_actorManager->BroadcastConfigUpdate();
			WriteChatf("[MQ2EZInv] Configuration broadcast to all peers");
		}
		else {
			WriteChatf("[MQ2EZInv] Actor system not initialized - cannot sync config");
		}
	}
	else {
		WriteChatf("[MQ2EZInv] Usage: /ezinvconfig [save|reload|sync]");
	}
}

// Execute trade directly using simple MQ commands
void ExecuteTradeLocally(const std::string& itemName, const std::string& targetChar, bool fromBank) {
	WriteChatf("[MQ2EZInv] Executing trade: %s to %s%s", 
		itemName.c_str(), targetChar.c_str(), fromBank ? " (from bank)" : "");
	
	// Simple direct approach - target player and try to give item
	std::string targetCommand = "/target " + targetChar;
	DoCommand(GetCharInfo()->pSpawn, targetCommand.c_str());
	
	// Wait briefly for targeting
	std::this_thread::sleep_for(std::chrono::milliseconds(500));
	
	// Try to find and give the item using MQ's built-in give functionality
	std::string giveItemCommand = "/itemnotify \"" + itemName + "\" leftmouseup";
	DoCommand(GetCharInfo()->pSpawn, giveItemCommand.c_str());
	
	// Wait for item to be on cursor
	std::this_thread::sleep_for(std::chrono::milliseconds(500));
	
	// Give to target if they're close enough
	DoCommand(GetCharInfo()->pSpawn, "/click left target");
	
	WriteChatf("[MQ2EZInv] Trade attempt completed for %s", itemName.c_str());
}

// NEW: Trade command for cross-character item trading
void EZInvTradeCommand(PlayerClient* pChar, const char* szLine) {
	std::string args = szLine ? szLine : "";
	if (args.empty()) {
		WriteChatf("[MQ2EZInv] Usage: /ezinvtrade <item_name> <source_char> <target_char> [frombank]");
		WriteChatf("  Example: /ezinvtrade \"Sword of Awesome\" MyMule MyMain");
		WriteChatf("  Example: /ezinvtrade \"Bank Item\" MyMule MyMain frombank");
		return;
	}

	// Parse arguments
	std::vector<std::string> parts;
	std::string currentToken;
	bool inQuotes = false;

	for (char c : args) {
		if (c == '"') {
			inQuotes = !inQuotes;
		}
		else if (c == ' ' && !inQuotes) {
			if (!currentToken.empty()) {
				parts.push_back(currentToken);
				currentToken.clear();
			}
		}
		else {
			currentToken += c;
		}
	}
	if (!currentToken.empty()) {
		parts.push_back(currentToken);
	}

	if (parts.size() < 3) {
		WriteChatf("[MQ2EZInv] Error: Need at least item name, source, and target");
		return;
	}

	std::string itemName = parts[0];
	std::string sourceChar = parts[1];
	std::string targetChar = parts[2];
	bool fromBank = (parts.size() > 3 && parts[3] == "frombank");

	// Check if we are the source character who should execute the trade
	std::string currentCharName = pChar ? pChar->Name : "";
	if (currentCharName == sourceChar) {
		// We are the source - execute the trade directly
		ExecuteTradeLocally(itemName, targetChar, fromBank);
	} else {
		// Forward the command to the source character via E3Next
		std::string forwardCommand = "/e3bct " + sourceChar + " /ezinvtrade \"" + itemName + "\" " + sourceChar + " " + targetChar;
		if (fromBank) {
			forwardCommand += " frombank";
		}
		WriteChatf("[MQ2EZInv] Forwarding trade request to %s", sourceChar.c_str());
		DoCommand(pChar, forwardCommand.c_str());
	}
}

// Internal command handler for shared memory messages
void EZInvInternalCommand(PlayerClient* pChar, const char* szLine) {
	if (!g_actorManager) {
		return; // Silently ignore if no actor manager
	}

	std::string args = szLine ? szLine : "";
	if (args.empty()) {
		return;
	}

	// Parse arguments - handle special case for trade_request with JSON payload
	std::vector<std::string> parts;
	
	// Check if this is a trade_request command with JSON payload
	if (args.find("trade_request ") == 0) {
		parts.push_back("trade_request");
		std::string jsonPayload = args.substr(14); // Skip "trade_request "
		
		// Try to parse as BatchTradeRequest first, then TradeRequest
		if (BatchTradeRequest batch; batch.FromJson(jsonPayload)) {
			parts.clear();
			parts.push_back("proxy_give_batch");
			// sourceCharacter stored in first item (sender)
			if (!batch.items.empty()) {
				parts.push_back(batch.items[0].sourceCharacter);
			} else {
				parts.push_back("");
			}
			// JSON payload should be the first argument (commandArgs[0])
			parts.push_back(batch.ToJson());
		} else if (TradeRequest req; req.FromJson(jsonPayload)) {
			parts.clear();
			parts.push_back("proxy_give");
			parts.push_back(req.sourceCharacter);
			parts.push_back(req.targetCharacter);
			parts.push_back(req.itemName);
		} else {
			WriteChatf("[MQ2EZInv] Invalid trade_request JSON: %s", jsonPayload.c_str());
			return;
		}
	} else {
		// Standard parsing for other commands
		bool inQuotes = false;
		std::string currentToken;

		for (char c : args) {
			if (c == '"') {
				inQuotes = !inQuotes;
			}
			else if (c == ' ' && !inQuotes) {
				if (!currentToken.empty()) {
					parts.push_back(currentToken);
					currentToken.clear();
				}
			}
			else {
				currentToken += c;
			}
		}
		if (!currentToken.empty()) {
			parts.push_back(currentToken);
		}
	}

	if (parts.size() < 2) {
		return; // Need at least command name and sender
	}

	std::string commandName = parts[0];
	std::string sender = parts[1];
	std::vector<std::string> commandArgs;
	
	// Extract command arguments
	for (size_t i = 2; i < parts.size(); ++i) {
		commandArgs.push_back(parts[i]);
	}

	// Handle commands directly without actor system dependency
	WriteChatf("[MQ2EZInv] Received command '%s' from %s", commandName.c_str(), sender.c_str());
	
	if (commandName == "proxy_give_batch" && !commandArgs.empty()) {
		// Handle batch trade request - create message and post it
		WriteChatf("[MQ2EZInv] Processing batch trade request with JSON payload");
		if (g_actorManager) {
			InventoryActorMessage message;
			message.type = ActorMessageType::COMMAND;
			message.sender = sender;
			message.target = EZInvUtils::GetCharacterName();
			message.commandName = commandName;
			message.commandArgs = commandArgs;
			WriteChatf("[MQ2EZInv] Posting command message: %s from %s to %s", 
				commandName.c_str(), sender.c_str(), message.target.c_str());
			g_actorManager->PostCommand(message);
			WriteChatf("[MQ2EZInv] Command message posted successfully");
		} else {
			WriteChatf("[MQ2EZInv] Actor system not available for batch processing");
		}
	} else if (commandName == "proxy_give" && !commandArgs.empty()) {
		// Handle single trade request - create message and post it
		WriteChatf("[MQ2EZInv] Processing single trade request");
		if (g_actorManager) {
			InventoryActorMessage message;
			message.type = ActorMessageType::COMMAND;
			message.sender = sender;
			message.target = EZInvUtils::GetCharacterName();
			message.commandName = commandName;
			message.commandArgs = commandArgs;
			g_actorManager->PostCommand(message);
		} else {
			WriteChatf("[MQ2EZInv] Actor system not available for trade processing");
		}
	} else {
		WriteChatf("[MQ2EZInv] Unknown command: %s", commandName.c_str());
	}
}

// E3Next integration status command
void EZInvE3StatusCommand(PlayerClient* pChar, const char* szLine) {
	std::string args = szLine ? szLine : "";
	
	if (args.empty() || args == "status") {
		WriteChatf("[MQ2EZInv] E3Next Integration Status:");
		WriteChatf("  E3Next Available: %s", E3Integration::IsE3NextAvailable() ? "YES" : "NO");
		
		auto connectedChars = E3Integration::GetConnectedCharacters();
		WriteChatf("  Connected Characters: %d", static_cast<int>(connectedChars.size()));
		
		for (const auto& charName : connectedChars) {
			InventoryData inventory;
			if (E3Integration::GetCharacterInventory(charName, inventory)) {
				WriteChatf("    %s: %d equipped, %d bags, %d bank items", 
					charName.c_str(),
					static_cast<int>(inventory.equipped.size()),
					static_cast<int>(inventory.bags.size()),
					static_cast<int>(inventory.bank.size()));
			}
		}
		
		if (g_e3InventoryManager) {
			g_e3InventoryManager->PrintStatus();
		}
		
		// Show Direct Network status
		if (g_e3DirectNetwork) {
			g_e3DirectNetwork->PrintStatus();
		}
		
		// Show Binary E3Next integration status (deprecated)
		WriteChatf("[MQ2EZInv] Binary Integration Status: REMOVED - using shared memory only");
	}
	else if (args.find("connect ") == 0) {
		std::string charName = args.substr(8);
		if (g_e3InventoryManager && g_e3InventoryManager->AddCharacter(charName)) {
			WriteChatf("[MQ2EZInv] Connected to E3Next character: %s", charName.c_str());
		} else {
			WriteChatf("[MQ2EZInv] Failed to connect to E3Next character: %s", charName.c_str());
		}
	}
	else if (args == "scan") {
		WriteChatf("[MQ2EZInv] Scanning for E3Next characters...");
		auto newChars = E3Integration::DiscoverE3NextCharacters();
		int added = 0;
		for (const auto& charName : newChars) {
			if (g_e3InventoryManager && g_e3InventoryManager->AddCharacter(charName)) {
				WriteChatf("[MQ2EZInv] Found and connected to: %s", charName.c_str());
				added++;
			}
		}
		WriteChatf("[MQ2EZInv] Scan complete: %d new characters added", added);
		WriteChatf("[MQ2EZInv] NOTE: MQ2EZInv can only find characters that E3Next is receiving data from.");
		WriteChatf("[MQ2EZInv] If you have 24 E3Next characters but only 5 show up, check:");
		WriteChatf("[MQ2EZInv]   1. Are all 24 characters connected to the same E3Next NetMQ network?");
		WriteChatf("[MQ2EZInv]   2. Are all 24 characters running E3Next with InventoryActorManager enabled?");
		WriteChatf("[MQ2EZInv]   3. Use '/e3 bots' on your main character to see which bots are connected to E3Next");
	}
	else if (args.find("add ") == 0) {
		// Add multiple characters: /ezinve3 add char1,char2,char3
		std::string charList = args.substr(4);
		std::stringstream ss(charList);
		std::string charName;
		int added = 0;
		
		while (std::getline(ss, charName, ',')) {
			// Trim whitespace
			charName.erase(0, charName.find_first_not_of(" \t"));
			charName.erase(charName.find_last_not_of(" \t") + 1);
			
			if (!charName.empty() && g_e3InventoryManager && g_e3InventoryManager->AddCharacter(charName)) {
				WriteChatf("[MQ2EZInv] Connected to E3Next character: %s", charName.c_str());
				added++;
			} else if (!charName.empty()) {
				WriteChatf("[MQ2EZInv] Failed to connect to E3Next character: %s", charName.c_str());
			}
		}
		WriteChatf("[MQ2EZInv] Added %d characters", added);
	}
	else if (args == "force") {
		WriteChatf("[MQ2EZInv] Force-adding all E3Next NetMQ connected characters...");
		
		// Get all the character names from the NetMQ connection log you provided
		std::vector<std::string> allE3NextChars = {
			"Degoju", "Donomoan", "Dureln", "Ebhove", "Estos", "Fateve", 
			"Fehaver", "Gifiren", "Gobedogu", "Hehici", "Kelythar", "Lerdari",
			"Linaheal", "Okhealz", "Pacoha", "Ubjuu", "Udmame", "Vepaon",
			"Wedyin", "Woroon", "Xutafu", "Zefios", "Zudau"
		};
		
		int added = 0;
		int failed = 0;
		for (const auto& charName : allE3NextChars) {
			if (g_e3InventoryManager && g_e3InventoryManager->AddCharacter(charName)) {
				WriteChatf("[MQ2EZInv] Force-connected to: %s", charName.c_str());
				added++;
			} else {
				failed++;
			}
		}
		WriteChatf("[MQ2EZInv] Force-add complete: %d connected, %d failed", added, failed);
		WriteChatf("[MQ2EZInv] Failed characters likely don't have E3Next InventoryActorManager running");
		WriteChatf("[MQ2EZInv] On failed characters, try: /e3 cmd InventoryActorManager_Init");
	}
	else {
		WriteChatf("[MQ2EZInv] Usage:");
		WriteChatf("  /ezinve3 status - Show connection status");
		WriteChatf("  /ezinve3 connect <character> - Connect to specific character");
		WriteChatf("  /ezinve3 scan - Scan for E3Next characters");
		WriteChatf("  /ezinve3 add <char1,char2,char3> - Add multiple characters");
		WriteChatf("  /ezinve3 force - Force-add all 23 E3Next characters from your network");
	}
}

// Plugin initialization
PLUGIN_API void InitializePlugin() {
	WriteChatf("[MQ2EZInv] Initializing plugin v%.1f", MQ2Version);

	// Initialize managers in dependency order
	g_performanceMonitor = std::make_unique<PerformanceMonitor>();
	g_sharedMemory = std::make_unique<SharedMemoryManager>();
	g_inventoryManager = std::make_unique<InventoryManager>();
	g_actorManager = std::make_unique<InventoryActorManager>(); // NEW: Initialize actor system
	g_itemSuggestionManager = std::make_unique<ItemSuggestionManager>(); // NEW: Initialize suggestion system
	g_inventoryUI = std::make_unique<InventoryUI>();
	
	
	// Initialize performance monitoring
	g_performanceMonitor->Initialize();

	// Initialize E3Next integration 
	if (!E3Integration::Initialize()) {
		WriteChatf("[MQ2EZInv] Warning: E3Next integration failed - falling back to local-only mode");
		
		// Fallback: Initialize shared memory if E3Next not available
		if (!g_sharedMemory->Initialize()) {
			WriteChatf("[MQ2EZInv] Warning: Shared memory initialization failed - peer inventory features disabled");
		}
    } else {
        WriteChatf("[MQ2EZInv] E3Next integration initialized successfully");
    }
	
	// Initialize Direct Network integration (named pipes)
	if (E3DirectNetwork::Initialize()) {
		WriteChatf("[MQ2EZInv] Direct network integration (named pipes) initialized successfully");
	} else {
		WriteChatf("[MQ2EZInv] Warning: Direct network integration failed");
	}
	
	
	
	// Always initialize actor manager for command processing
	if (!g_actorManager->Initialize()) {
		WriteChatf("[MQ2EZInv] Warning: Actor system initialization failed - cross-character features disabled");
	}

    // Trade requests will be received automatically via E3Next's pub/sub system
    // No manual subscription needed - E3Next auto-subscribes to custom topics
	
	// Initialize item suggestion manager
	if (g_itemSuggestionManager) {
		g_itemSuggestionManager->Initialize(g_sharedMemory, g_actorManager);
		WriteChatf("[MQ2EZInv] Item suggestion system initialized");
	}

	// Add commands
	AddCommand("/ezinv", EZInvCommand);
	AddCommand("/ezinvstats", EZInvStatsCommand);
	AddCommand("/ezinvconfig", EZInvConfigCommand);
	AddCommand("/ezinvtrade", EZInvTradeCommand); // NEW: Trade command
	AddCommand("/ezinv_command", EZInvInternalCommand); // NEW: Internal command handler
	AddCommand("/ezinve3", EZInvE3StatusCommand); // NEW: E3Next integration status

	WriteChatf("[MQ2EZInv] Plugin initialized successfully");

	// Auto-broadcast to peers via shared memory
	if (g_actorManager && g_actorManager->IsInitialized()) {

		// Request inventories from all peers after a delay
		g_actorManager->AddDeferredTask([]() {
			g_actorManager->RequestAllInventories();
			if (g_inventoryManager) {
				g_actorManager->PublishInventory(g_inventoryManager->GetCurrentInventory());
			}
			}, std::chrono::seconds(3));
	}
	
	// Also publish via direct network if available
	if (g_e3DirectNetwork && g_inventoryManager) {
		g_e3DirectNetwork->PublishInventory(g_inventoryManager->GetCurrentInventory());
	}
	
	
}

// Plugin shutdown
PLUGIN_API void ShutdownPlugin() {
	WriteChatf("[MQ2EZInv] Shutting down plugin");

	// Remove commands
	RemoveCommand("/ezinv");
	RemoveCommand("/ezinvstats");
	RemoveCommand("/ezinvconfig");
	RemoveCommand("/ezinvtrade");
	RemoveCommand("/ezinv_command");
	RemoveCommand("/ezinve3");

	// Cleanup in reverse order
	g_inventoryUI.reset();
	g_itemSuggestionManager.reset(); // Cleanup suggestion system
	
	// Shutdown Direct Network integration
	if (g_e3DirectNetwork) {
		g_e3DirectNetwork->Shutdown();
		g_e3DirectNetwork.reset();
	}
	
	// Shutdown E3Next integration (replaces actor manager and shared memory cleanup)
	E3Integration::Shutdown();
	
	g_actorManager.reset(); // Fallback cleanup
	g_inventoryManager.reset();
	g_sharedMemory.reset(); // Fallback cleanup
	g_performanceMonitor.reset();

	WriteChatf("[MQ2EZInv] Plugin shutdown complete");
}

// Called every frame when plugin is loaded
PLUGIN_API void OnPulse() {
	if (EZInvUtils::IsGameReady()) {
		// Update inventory manager
		if (g_inventoryManager) {
			g_inventoryManager->Update();
		}

		// Update E3Next integration (replaces actor manager update)
		E3Integration::Update();
		
		// Update Direct Network integration
		E3DirectNetwork::Update();
		
		
		
		// Always update actor manager to process command messages
		if (g_actorManager) {
			g_actorManager->Update();
		}
		
		// Update performance monitor
		if (g_performanceMonitor) {
			g_performanceMonitor->Update();
		}
		
		// NEW: Update item suggestion manager
		if (g_itemSuggestionManager) {
			g_itemSuggestionManager->ProcessPendingRequests();
		}
	}
}

// ImGui rendering callback
PLUGIN_API void OnUpdateImGui() {
	if (g_inventoryUI && EZInvUtils::IsGameReady()) {
		g_inventoryUI->Update();
		g_inventoryUI->Render();
		g_inventoryUI->RenderToggleButton();
	}
}
