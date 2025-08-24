# Hellscape UI Toolkit Setup Guide

## Overview
This guide covers the setup and testing of the new UI Toolkit system for Hellscape, including Main Menu, HUD, Pause Menu, and damage vignette effects.

## Files Created

### UXML Files
- `Assets/Hellscape/UI/UXML/MainMenu.uxml` - Main menu with host/join functionality
- `Assets/Hellscape/UI/UXML/HUD.uxml` - In-game HUD with scores, health, and inventory
- `Assets/Hellscape/UI/UXML/PauseMenu.uxml` - Pause overlay with resume/exit options

### USS Files
- `Assets/Hellscape/UI/USS/ui-common.uss` - Common styling for all UI components

### C# Scripts
- `Assets/Hellscape/Scripts/Presentation/UI/MainMenuUI.cs` - Main menu controller
- `Assets/Hellscape/Scripts/Presentation/UI/HudUI.cs` - HUD controller
- `Assets/Hellscape/Scripts/Presentation/UI/PauseMenuUI.cs` - Pause menu controller
- `Assets/Hellscape/Scripts/Presentation/UI/VignetteFxUI.cs` - Damage vignette effects
- `Assets/Hellscape/Scripts/Presentation/UI/UIBootstrap.cs` - UI system coordinator

### Tests
- `Assets/Scripts/Tests/UITests.cs` - Basic UI component tests

## Scene Setup

### 1. Create UI Root GameObject
1. In your bootstrap scene, create a new GameObject named `UIRoot`
2. Add the `UIBootstrap` component to this GameObject
3. Set `DontDestroyOnLoad` to make it persistent across scenes

### 2. Create UI Document Children
Create three child GameObjects under `UIRoot`:

#### MainMenuDoc
- Add `UIDocument` component
- Set `Panel Settings` to your UI Panel Settings asset
- Set `Source Asset` to `MainMenu.uxml`
- Add `Style Sheets` reference to `ui-common.uss`
- Add `MainMenuUI` component
- Assign a `RelayBootstrap` reference to the MainMenuUI

#### HudDoc
- Add `UIDocument` component
- Set `Panel Settings` to your UI Panel Settings asset
- Set `Source Asset` to `HUD.uxml`
- Add `Style Sheets` reference to `ui-common.uss`
- Add `HudUI` component
- Add `VignetteFxUI` component

#### PauseDoc
- Add `UIDocument` component
- Set `Panel Settings` to your UI Panel Settings asset
- Set `Source Asset` to `PauseMenu.uxml`
- Add `Style Sheets` reference to `ui-common.uss`
- Add `PauseMenuUI` component

### 3. Configure UIBootstrap
In the `UIBootstrap` component inspector:
- Assign `MainMenuDoc` to the `Main Menu Doc` field
- Assign `HudDoc` to the `HUD Doc` field
- Assign `PauseDoc` to the `Pause Doc` field
- Assign the UI components to their respective fields

## Testing the UI System

### 1. Basic Functionality Test
1. Open the bootstrap scene in Unity Editor
2. Enter Play Mode
3. Verify the Main Menu appears with "HELLSCAPE" title
4. Test the Host button (requires RelayBootstrap setup)
5. Test the Join functionality with a valid join code

### 2. Network State Transitions
1. Start as Host - Main Menu should hide, HUD should show
2. Join as Client - Main Menu should hide, HUD should show
3. Disconnect - HUD should hide, Main Menu should show

### 3. HUD Features
1. Verify Team Score displays (starts at 000000)
2. Verify Personal Score displays (starts at 0000)
3. Verify Health Bar shows 100/100
4. Verify Inventory slots show "Base" + 3 "Empty" slots

### 4. Pause Menu
1. Click the "Menu" button in HUD
2. Verify Pause overlay appears
3. Test "Resume" button - should hide overlay
4. Test "Exit to Main Menu" - should return to main menu

### 5. Health System Test
```csharp
// In Play Mode, you can test health updates via console:
var hudUI = FindObjectOfType<HudUI>();
hudUI.SetLocalHealth(75, 100); // Should update health bar to 75%
```

### 6. Damage Vignette Test
```csharp
// In Play Mode, you can test damage effects via console:
var vignette = FindObjectOfType<VignetteFxUI>();
vignette.Flash(0.5f); // Should show red vignette at 50% intensity
vignette.OnLocalPlayerDamaged(25); // Should flash based on damage amount
```

## API Reference

### MainMenuUI
- `ShowError(string error)` - Display error message in status text

### HudUI
- `SetLocalHealth(int current, int max)` - Update health display
- `SetSlot(int slotIndex, string label, int ammo, bool active)` - Update inventory slot

### VignetteFxUI
- `Flash(float intensity = 1f, float hold = 0.06f, float fade = 0.25f)` - Flash damage vignette
- `OnLocalPlayerDamaged(int amount)` - Flash based on damage amount

### PauseMenuUI
- `Show(bool show)` - Show/hide pause menu and pause game

### UIBootstrap
- `ShowPauseMenu(bool show)` - Control pause menu visibility
- `GetHudUI()` - Get HUD component reference
- `GetVignetteFxUI()` - Get vignette component reference
- `GetMainMenuUI()` - Get main menu component reference

## Integration Notes

### Health System Integration
The health system is currently placeholder-based. To integrate with real health:
1. Find the player's health component
2. Subscribe to damage events
3. Call `HudUI.SetLocalHealth(current, max)`
4. Call `VignetteFxUI.OnLocalPlayerDamaged(amount)`

### Score System Integration
The personal score is currently calculated as team score divided by player count. To implement proper personal scoring:
1. Add personal score tracking to SimGameServer
2. Create a NetworkVariable for personal scores
3. Update HudUI to read the correct personal score

### Inventory System Integration
The inventory slots are currently placeholder. To implement real inventory:
1. Create inventory data structures
2. Add inventory management to the game logic
3. Call `HudUI.SetSlot()` when inventory changes

## Troubleshooting

### UI Not Showing
- Check that UIDocument components have correct UXML assets assigned
- Verify Panel Settings are configured correctly
- Ensure UI components are active in the hierarchy

### Network Integration Issues
- Verify RelayBootstrap is assigned to MainMenuUI
- Check that NetworkManager is properly configured
- Ensure UIBootstrap is listening to network events

### Styling Issues
- Verify `ui-common.uss` is assigned to all UIDocuments
- Check that USS classes match the UXML elements
- Ensure Panel Settings has the correct USS references

## Performance Considerations

- UI updates are minimal and efficient
- Health bar updates use CSS transitions for smooth animation
- Damage vignette uses coroutines for non-blocking effects
- Score polling is done per-frame but can be optimized to event-based updates

## Future Enhancements

1. **Real Health Integration** - Connect to actual player health system
2. **Personal Score Tracking** - Implement proper individual scoring
3. **Inventory System** - Add real inventory management
4. **Settings Menu** - Add audio, graphics, and control settings
5. **Loading Screens** - Add loading indicators for network operations
6. **Tooltips** - Add hover tooltips for UI elements
7. **Accessibility** - Add screen reader support and keyboard navigation
