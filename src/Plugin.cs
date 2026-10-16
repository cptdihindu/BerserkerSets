using System;
using BepInEx;
using BepInEx.Logging;
using BerserkerSets.Configuration;
using BerserkerSets.Equipment;
using BerserkerSets.Input;
using BerserkerSets.Loadouts;
using BerserkerSets.UI;
using HarmonyLib;

namespace BerserkerSets
{
    [BepInPlugin(PluginGuid, PluginName, PluginVersion)]
    [BepInDependency("com.jotunn.jotunn", BepInDependency.DependencyFlags.HardDependency)]
    public sealed class Plugin : BaseUnityPlugin
    {
        public const string PluginGuid = "dev.berserkersets.quickloadouts";
        public const string PluginName = "Berserker Sets";
        public const string PluginVersion = "0.2.0";

        private Harmony _harmony;
        private ModConfig _modConfig;
        private InputController _input;

        internal static Plugin Instance { get; private set; }
        internal LoadoutManager Manager { get; private set; }
        internal LoadoutPanel Panel { get; private set; }
        internal LoadoutBanner Banner { get; private set; }
        internal ManualLogSource LogSource => Logger;

        private void Awake()
        {
            Instance = this;
            try
            {
                _modConfig = new ModConfig(Config);
                Manager = new LoadoutManager(Logger);
                _input = new InputController(_modConfig, Logger);
                Panel = new LoadoutPanel(Manager, _modConfig, _input);
                Banner = new LoadoutBanner();
                _harmony = new Harmony(PluginGuid);
                _harmony.PatchAll(typeof(Plugin).Assembly);
                Logger.LogInfo($"{PluginName} {PluginVersion} initialized.");
            }
            catch (Exception ex)
            {
                Logger.LogError($"{PluginName} initialization failed: {ex}");
                enabled = false;
            }
        }

        private void Update()
        {
            if (!_modConfig.EnableMod.Value) return;
            try
            {
                Manager.TickCharacter();
                Panel.Tick();
                // LB/RB and D-pad belong to inventory navigation while the panel is visible.
                int cycleDirection = Panel.IsVisible ? 0 : _input.GetCycleDirection();
                if (cycleDirection != 0)
                {
                    Logger.LogInfo($"Cycle request received; direction={(cycleDirection > 0 ? "forward" : "backward")}.");
                    SwitchResult result = Manager.Cycle(cycleDirection);
                    Logger.LogInfo($"Cycle result: processed={result.Processed}, message='{result.Message ?? string.Empty}'.");
                    if (!string.IsNullOrWhiteSpace(result.Message)) ShowSwitchHud(result);
                }
            }
            catch (Exception ex)
            {
                Logger.LogError($"Unexpected update error: {ex}");
            }
        }

        private void OnDestroy()
        {
            try
            {
                Manager?.SaveAndReset();
                Panel?.Dispose();
                Banner?.Dispose();
                _harmony?.UnpatchSelf();
            }
            catch (Exception ex)
            {
                Logger.LogError($"Cleanup failed: {ex}");
            }
            finally
            {
                Instance = null;
            }
        }

        internal void ShowHud(string message)
        {
            if (string.IsNullOrWhiteSpace(message) || _modConfig == null || !_modConfig.ShowHudNotifications.Value) return;
            if (MessageHud.instance != null) MessageHud.instance.ShowMessage(MessageHud.MessageType.TopLeft, message);
        }

        private void ShowSwitchHud(SwitchResult result)
        {
            string message = result?.Message;
            if (string.IsNullOrWhiteSpace(message) || _modConfig == null || !_modConfig.ShowHudNotifications.Value) return;
            if (Banner == null || !Banner.Show(message, result.MainIcon, result.OffIcon)) ShowHud(message);
        }

        internal void DebugLog(string message)
        {
            if (_modConfig != null && _modConfig.EnableDebugLogging.Value) Logger.LogDebug(message);
        }
    }
}
