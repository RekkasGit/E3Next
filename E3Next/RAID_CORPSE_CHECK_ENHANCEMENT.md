# Enhanced Hunt Mode Raid Member Corpse Check

## Overview
The hunt mode in E3Next has been enhanced with robust raid member corpse checking to prevent pulling when raid members are dead. This addresses the issue where hunt mode would continue pulling even when raid members were in corpse form.

## Features

### 1. Comprehensive Raid Member Detection
The system checks for dead raid members using multiple methods:
- **Direct Raid.Member.State checks** - Uses MQ raid member state queries
- **Spawn-based detection** - Looks for corpse spawns and dead/invisible states  
- **Bot Network Integration** - Queries connected bots for their death status
- **Zone-aware checking** - Only considers members currently in your zone

### 2. Configurable Timeout System
- **Timeout Protection**: Prevents indefinite waiting for dead members
- **Default**: 5 minutes (300 seconds) 
- **Minimum**: 30 seconds
- **Per-member tracking**: Each dead member has individual timeout tracking

### 3. Smart State Management
- **Automatic cleanup**: Removes timers when members are resurrected
- **Logging**: Detailed logs for debugging and monitoring
- **Status reporting**: Clear hunt status messages indicating which members are dead

## Configuration Options

### Hunt Settings
- `RaidCorpseCheckEnabled` (bool): Enable/disable raid corpse checking (default: true)
- `RaidCorpseCheckTimeoutSec` (int): Timeout in seconds before ignoring dead members (default: 300)

## Commands

### Basic Control
```
/hunt raidcorpse on         # Enable raid corpse checking
/hunt raidcorpse off        # Disable raid corpse checking  
/hunt raidcorpse toggle     # Toggle raid corpse checking
```

### Timeout Configuration
```
/hunt raidcorpsetimeout 600    # Set timeout to 10 minutes
/hunt raidcorpsetimeout 120    # Set timeout to 2 minutes
```

## Implementation Details

### New Functions Added

#### e3util.cs
- `GetDeadRaidMembers()`: Comprehensive raid member death detection

#### Hunt.cs
- Enhanced `DetermineTargetState()`: Integrated raid corpse checking logic
- New configuration properties: `RaidCorpseCheckEnabled`, `RaidCorpseCheckTimeoutSec` 
- Raid member corpse timestamp tracking
- Command handlers for new functionality

### Detection Logic Flow
1. **Check if in raid**: Only runs if `${Raid.Members} > 0`
2. **Get dead members**: Calls `e3util.GetDeadRaidMembers()` 
3. **Track timestamps**: Records when each member first detected as dead
4. **Apply timeouts**: Removes members who exceed timeout duration
5. **Pause hunting**: Transitions to `HuntState.Paused` if any members still waiting for resurrection

### Status Messages
- Single member: "Waiting for raid member [Name] to resurrect"
- Multiple members: "Waiting for raid members [Name1, Name2] to resurrect"

## Usage Examples

### Raiding Scenario
When raiding with your guild/group:
```
/hunt on                    # Enable hunt mode
/hunt raidcorpse on        # Enable raid corpse checking (default)
/hunt raidcorpsetimeout 300 # Wait up to 5 minutes for resurrections
/hunt go                   # Start hunting
```

### Long Raids with Frequent Deaths
For raids where deaths are common and you want shorter waits:
```  
/hunt raidcorpsetimeout 120  # Only wait 2 minutes for resurrections
```

### Solo/Group Play (No Raid Corpse Checking)
When not raiding or want to disable the feature:
```
/hunt raidcorpse off        # Disable raid corpse checking
```

## Benefits

1. **Prevents Overaggro**: No more pulling when key raid members are down
2. **Raid Safety**: Ensures group cohesion during raid encounters
3. **Flexible Configuration**: Adjustable timeout prevents indefinite waiting
4. **Zero False Positives**: Only affects actual raid scenarios
5. **Backward Compatible**: Doesn't change existing non-raid functionality

## Error Handling

The system includes comprehensive error handling:
- MQ query failures are caught and logged
- Bot network failures don't break functionality  
- Invalid timeout values are corrected to minimum safe values
- Missing raid data gracefully falls back to normal operation

This enhancement significantly improves hunt mode safety in raid scenarios while maintaining full compatibility with existing solo and group hunting workflows.