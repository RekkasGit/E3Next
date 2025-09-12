# WARP.md

This file provides guidance to WARP (warp.dev) when working with code in this repository.

## Project Overview

E3Next is an automation software for EverQuest using the MQ2Mono plugin, written in C#. It was inspired by the original E3 macro and serves as both a proof of concept for MQ2Mono and a full-featured automation system.

**Dependencies**: Requires MQ2Mono plugin (https://github.com/RekkasGit/MQ2Mono)

## Common Development Commands

### Building the Project
```bash
# Build the entire solution
msbuild MQ2MonoSharp.sln /p:Configuration=Debug

# Build release version
msbuild MQ2MonoSharp.sln /p:Configuration=Release

# Build specific project (example: E3Next core)
msbuild E3Next\E3Next.csproj /p:Configuration=Debug

# Alternative using dotnet CLI (if applicable)
dotnet build MQ2MonoSharp.sln
```

### Development Environment Setup
```bash
# Set environment variable for build destination (optional)
$env:E3BuildDest = "C:\path\to\your\eq\folder"

# The PostBuildEvent will automatically copy files if E3BuildDest is set
# Files copied: *.dll, *.exe, *.exe.config
```

### Testing/Running
```bash
# In EverQuest with MQ2Mono loaded, start E3Next:
# /mono e3
```

### Package Management
```bash
# Restore NuGet packages
nuget restore MQ2MonoSharp.sln

# Or using dotnet
dotnet restore MQ2MonoSharp.sln
```

## Architecture Overview

### Multi-Project Solution Structure
The solution contains several interconnected C# projects and one C++ test project:

**Core Projects:**
- **E3Next**: Main automation engine with class-specific logic and processors
- **E3NextUI**: User interface components  
- **E3Discord**: Discord integration functionality
- **E3NextProxy**: Proxy/communication layer
- **E3NextConfigEditor**: Configuration management tools
- **E3Display**: Display/overlay components

**Supporting Libraries:**
- **ApiLibrary**: Shared API components
- **IniFileParser**: Configuration file parsing
- **RemoteDebuggerServer/Client**: Remote debugging infrastructure
- **TestCore**: Core testing utilities
- **Template**: Project template/scaffold

**Test Projects:**
- **MQ2MonoTester**: C++ test project for MQ2Mono integration
- **RemoteDebugServerTester**: Debug server testing tools

### Core Processing Architecture

**Main Processing Loop** (`E3.Process()`):
1. **State Updates**: Character state, caches, and external commands
2. **Pre-Processing**: Life support checks, nowcast/backoff commands, burns
3. **Advanced Settings**: Dynamic method invocation based on class and configuration
4. **Class-Specific Methods**: Individual class behavior processing
5. **Final Cleanup**: Post-processing actions

**Event Processing System**:
- **EventProcessor**: Multi-threaded regex-based event matching and queuing
- **Command Processing**: Queue-based command handling with rate limiting
- **Thread Safety**: Concurrent collections and proper synchronization

**Class-Based Architecture**:
- **Base Class System**: All EQ classes inherit from common base with shared functionality
- **Class-Specific Logic**: Individual files for each EQ class (Bard, Cleric, Warrior, etc.)
- **Processor Pattern**: Modular processors for specific functionality (Heals, Nukes, Movement, etc.)

### Key Components

**Settings System**:
- **BaseSettings/CharacterSettings**: Character-specific configuration
- **AdvancedSettings**: Dynamic method mapping and configuration
- **FeatureSettings**: Individual feature configurations (Loot, Inventory, etc.)

**Communication Layer**:
- **NetMQ Integration**: ZeroMQ-based messaging for multi-client coordination
- **PubSub Pattern**: Publisher/subscriber for real-time communication
- **Router/Dealer**: Request/response patterns for command handling

**Data Management**:
- **Spell System**: Comprehensive spell data with protobuf serialization
- **Character State**: Buffs, timers, resist counters
- **Zone/Location**: Spatial awareness and navigation

## Development Guidelines

### Code Organization
- Each EQ class has its own file in `/Classes/`
- Processors are modular and located in `/Processors/`
- Settings follow a hierarchical structure in `/Settings/`
- UI components are separated in dedicated projects

### Thread Safety
- Main processing runs on MQ2Mono's managed thread
- Event processing uses separate background thread with concurrent collections
- UI updates are queued and applied safely on main thread

### Performance Considerations
- Processing loop runs every EQ frame (~60Hz)
- ActionTaken flag prevents multiple actions per frame
- Method lookup caches for dynamic invocation
- Garbage collection monitoring with 5-minute intervals

### Integration Points
- **MQ2Mono Interface**: Core.cs provides the bridge between C++ and C#
- **Configuration**: INI-based settings with live reload capability
- **External Communication**: NetMQ for multi-client coordination
- **UI Integration**: ImGui-based overlays and controls

## Key Files for Understanding

- `Core.cs`: MQ2Mono integration layer and main processing thread
- `E3.cs`: Main processing loop and orchestration
- `Classes/[ClassName].cs`: Class-specific automation logic  
- `Processors/`: Modular functionality (Heals, Nukes, Movement, etc.)
- `Settings/`: Configuration management system
- `Server/`: NetMQ communication infrastructure

## Installation Notes

From README.md: Copy the mono folder from root and E3 INI folders in /config to your EverQuest directory. Start with `/mono e3` command in-game.