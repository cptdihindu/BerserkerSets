using BepInEx.Configuration;
using BepInEx.Logging;
using BerserkerSets.Configuration;
using Jotunn.Configs;
using Jotunn.Managers;

namespace BerserkerSets.Input
{
    internal sealed class InputController
    {
        private const string KeyboardAction = "CycleBerserkerSet";
        private const string ModifierAction = "BerserkerSetModifier";
        private const string ForwardAction = "CycleBerserkerSetForward";
        private const string BackwardAction = "CycleBerserkerSetBackward";

        private readonly ModConfig _config;
        private readonly ManualLogSource _log;
        private readonly ButtonConfig _keyboardButton;
        private readonly ButtonConfig _modifierButton;
        private readonly ButtonConfig _forwardButton;
        private readonly ButtonConfig _backwardButton;

        public InputController(ModConfig config, ManualLogSource log)
        {
            _config = config;
            _log = log;
            _keyboardButton = RegisterKeyboard(KeyboardAction, config);
            _modifierButton = RegisterGamepad(ModifierAction, config.ControllerComboModifier);
            _forwardButton = RegisterGamepad(ForwardAction, config.ControllerForwardButton);
            _backwardButton = RegisterGamepad(BackwardAction, config.ControllerBackwardButton);

            _log.LogInfo(
                $"Inputs registered: keyboard '{_keyboardButton.Name}'={config.KeyboardCycleKey.Value}; " +
                $"controller modifier={config.ControllerComboModifier.Value}, " +
                $"backward={config.ControllerBackwardButton.Value}, forward={config.ControllerForwardButton.Value}, " +
                $"enabled={config.EnableControllerInput.Value}.");
        }

        // Returns -1 for backward, +1 for forward, and 0 when no command fired.
        public int GetCycleDirection()
        {
            if (ZInput.instance == null) return 0;
            if (ZInput.GetButtonDown(_keyboardButton.Name))
            {
                _log.LogInfo($"Keyboard forward-cycle input detected ({_config.KeyboardCycleKey.Value}).");
                return 1;
            }

            if (!_config.EnableControllerInput.Value || !ZInput.GetButton(_modifierButton.Name)) return 0;

            if (ZInput.GetButtonDown(_backwardButton.Name))
            {
                _log.LogInfo(
                    $"Controller backward-cycle combo detected " +
                    $"({_config.ControllerComboModifier.Value} + {_config.ControllerBackwardButton.Value}).");
                return -1;
            }

            if (ZInput.GetButtonDown(_forwardButton.Name))
            {
                _log.LogInfo(
                    $"Controller forward-cycle combo detected " +
                    $"({_config.ControllerComboModifier.Value} + {_config.ControllerForwardButton.Value}).");
                return 1;
            }

            return 0;
        }

        private static ButtonConfig RegisterKeyboard(string name, ModConfig config)
        {
            var button = new ButtonConfig
            {
                Name = name,
                Config = config.KeyboardCycleKey,
                ActiveInGUI = false,
                ActiveInCustomGUI = false,
                BlockOtherInputs = false
            };
            InputManager.Instance.AddButton(Plugin.PluginGuid, button);
            return button;
        }

        private static ButtonConfig RegisterGamepad(
            string name,
            ConfigEntry<InputManager.GamepadButton> config)
        {
            var button = new ButtonConfig
            {
                Name = name,
                GamepadConfig = config,
                ActiveInGUI = false,
                ActiveInCustomGUI = false,
                BlockOtherInputs = false
            };
            InputManager.Instance.AddButton(Plugin.PluginGuid, button);
            return button;
        }
    }
}
