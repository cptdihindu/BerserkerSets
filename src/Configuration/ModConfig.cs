using BepInEx.Configuration;
using Jotunn.Managers;
using UnityEngine;

namespace BerserkerSets.Configuration
{
    internal sealed class ModConfig
    {
        public ConfigEntry<bool> EnableMod { get; }
        public ConfigEntry<KeyCode> KeyboardCycleKey { get; }
        public ConfigEntry<bool> EnableControllerInput { get; }
        public ConfigEntry<InputManager.GamepadButton> ControllerComboModifier { get; }
        public ConfigEntry<InputManager.GamepadButton> ControllerForwardButton { get; }
        public ConfigEntry<InputManager.GamepadButton> ControllerBackwardButton { get; }
        public ConfigEntry<bool> ShowHudNotifications { get; }
        public ConfigEntry<bool> EnableInventoryPanel { get; }
        public ConfigEntry<float> PanelPositionX { get; }
        public ConfigEntry<float> PanelPositionY { get; }
        public ConfigEntry<float> PanelScale { get; }
        public ConfigEntry<bool> EnableDebugLogging { get; }

        public ModConfig(ConfigFile config)
        {
            EnableMod = config.Bind("General", "EnableMod", true, "Enable Berserker Sets.");
            KeyboardCycleKey = config.Bind("Input", "KeyboardCycleKey", KeyCode.F6, "Keyboard key used to cycle loadouts.");
            EnableControllerInput = config.Bind("Input", "EnableControllerInput", true, "Enable controller loadout cycling.");
            ControllerComboModifier = config.Bind("Input", "ControllerComboModifier", InputManager.GamepadButton.LeftShoulder, "Controller modifier held while pressing a cycle direction.");
            ControllerForwardButton = config.Bind("Input", "ControllerForwardButton", InputManager.GamepadButton.DPadRight, "Controller button pressed with the modifier to cycle forward.");
            ControllerBackwardButton = config.Bind("Input", "ControllerBackwardButton", InputManager.GamepadButton.DPadLeft, "Controller button pressed with the modifier to cycle backward.");
            ShowHudNotifications = config.Bind("Feedback", "ShowHudNotifications", true, "Show top-left loadout messages.");
            EnableInventoryPanel = config.Bind("UI", "EnableInventoryPanel", true, "Show the five-set editor beside the player inventory.");
            PanelPositionX = config.Bind("UI", "PanelPositionX", 0f, "Horizontal offset from the mod's default panel position. Positive values move right; negative values move left.");
            PanelPositionY = config.Bind("UI", "PanelPositionY", 0f, "Vertical offset from the mod's default panel position. Positive values move up; negative values move down.");
            PanelScale = config.Bind("UI", "PanelScale", 0.9f, "Preferred loadout panel scale. Auto-placement may reduce it to remain on screen.");
            EnableDebugLogging = config.Bind("Diagnostics", "EnableDebugLogging", false, "Log additional diagnostics.");

        }
    }
}
