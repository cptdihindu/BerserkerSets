using BepInEx.Configuration;
using BepInEx.Logging;
using BerserkerSets.Configuration;
using Jotunn.Configs;
using Jotunn.Managers;
using UnityEngine;

namespace BerserkerSets.Input
{
    internal enum UiCommand
    {
        None,
        Up,
        Down,
        Left,
        Right,
        Accept,
        Cancel,
        PreviousGroup,
        NextGroup
    }

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
        private readonly ButtonConfig _uiUp;
        private readonly ButtonConfig _uiDown;
        private readonly ButtonConfig _uiLeft;
        private readonly ButtonConfig _uiRight;
        private readonly ButtonConfig _uiAccept;
        private readonly ButtonConfig _uiCancel;
        private readonly ButtonConfig _uiPreviousGroup;
        private readonly ButtonConfig _uiNextGroup;
        private UiCommand _heldStickCommand;
        private float _nextStickRepeatAt;

        public InputController(ModConfig config, ManualLogSource log)
        {
            _config = config;
            _log = log;
            _keyboardButton = RegisterKeyboard(KeyboardAction, config);
            _modifierButton = RegisterGamepad(ModifierAction, config.ControllerComboModifier);
            _forwardButton = RegisterGamepad(ForwardAction, config.ControllerForwardButton);
            _backwardButton = RegisterGamepad(BackwardAction, config.ControllerBackwardButton);
            _uiUp = RegisterUiGamepad("BerserkerUiUp", InputManager.GamepadButton.DPadUp);
            _uiDown = RegisterUiGamepad("BerserkerUiDown", InputManager.GamepadButton.DPadDown);
            _uiLeft = RegisterUiGamepad("BerserkerUiLeft", InputManager.GamepadButton.DPadLeft);
            _uiRight = RegisterUiGamepad("BerserkerUiRight", InputManager.GamepadButton.DPadRight);
            _uiAccept = RegisterUiGamepad("BerserkerUiAccept", InputManager.GamepadButton.ButtonSouth);
            _uiCancel = RegisterUiGamepad("BerserkerUiCancel", InputManager.GamepadButton.ButtonEast);
            _uiPreviousGroup = RegisterUiGamepad("BerserkerUiPreviousGroup", InputManager.GamepadButton.LeftShoulder);
            _uiNextGroup = RegisterUiGamepad("BerserkerUiNextGroup", InputManager.GamepadButton.RightShoulder);

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

        public UiCommand GetUiCommand()
        {
            if (ZInput.instance == null || !_config.EnableControllerInput.Value) return UiCommand.None;
            if (ZInput.GetButtonDown(_uiPreviousGroup.Name)) return UiCommand.PreviousGroup;
            if (ZInput.GetButtonDown(_uiNextGroup.Name)) return UiCommand.NextGroup;
            if (ZInput.GetButtonDown(_uiUp.Name)) return UiCommand.Up;
            if (ZInput.GetButtonDown(_uiDown.Name)) return UiCommand.Down;
            if (ZInput.GetButtonDown(_uiLeft.Name)) return UiCommand.Left;
            if (ZInput.GetButtonDown(_uiRight.Name)) return UiCommand.Right;
            if (ZInput.GetButtonDown(_uiAccept.Name)) return UiCommand.Accept;
            if (ZInput.GetButtonDown(_uiCancel.Name)) return UiCommand.Cancel;
            return GetLeftStickCommand();
        }

        public bool IsGamepadActive => ZInput.instance != null && ZInput.IsGamepadActive();

        public bool IsUiCancelDown()
        {
            return ZInput.instance != null &&
                   _config.EnableControllerInput.Value &&
                   ZInput.GetButtonDown(_uiCancel.Name);
        }

        private UiCommand GetLeftStickCommand()
        {
            float x = ZInput.GetJoyLeftStickX(false);
            float y = ZInput.GetJoyLeftStickY(false);
            const float deadZone = 0.55f;
            UiCommand command = UiCommand.None;

            if (Mathf.Abs(x) >= deadZone || Mathf.Abs(y) >= deadZone)
            {
                if (Mathf.Abs(x) > Mathf.Abs(y)) command = x < 0f ? UiCommand.Left : UiCommand.Right;
                // Valheim's exposed left-stick Y value is negative when pushed up.
                else command = y < 0f ? UiCommand.Up : UiCommand.Down;
            }

            if (command == UiCommand.None)
            {
                _heldStickCommand = UiCommand.None;
                _nextStickRepeatAt = 0f;
                return UiCommand.None;
            }

            if (command != _heldStickCommand)
            {
                _heldStickCommand = command;
                _nextStickRepeatAt = Time.unscaledTime + 0.38f;
                return command;
            }

            if (Time.unscaledTime < _nextStickRepeatAt) return UiCommand.None;
            _nextStickRepeatAt = Time.unscaledTime + 0.14f;
            return command;
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

        private static ButtonConfig RegisterUiGamepad(string name, InputManager.GamepadButton gamepadButton)
        {
            var button = new ButtonConfig
            {
                Name = name,
                GamepadButton = gamepadButton,
                ActiveInGUI = true,
                ActiveInCustomGUI = true,
                BlockOtherInputs = false
            };
            InputManager.Instance.AddButton(Plugin.PluginGuid, button);
            return button;
        }
    }
}
