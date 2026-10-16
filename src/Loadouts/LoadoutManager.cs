using System;
using System.Globalization;
using BepInEx.Logging;
using BerserkerSets.Equipment;
using BerserkerSets.Models;

namespace BerserkerSets.Loadouts
{
    internal sealed class LoadoutManager
    {
        private readonly ManualLogSource _log;
        private readonly LoadoutStorage _storage;
        private readonly ItemResolver _resolver;
        private readonly EquipmentSwitcher _switcher;
        private Player _player;
        private CharacterLoadoutData _data;
        private string _characterId;

        public event Action Changed;

        public Player Player => _player;
        public CharacterLoadoutData Data => _data;
        public bool IsReady => _player != null && _data != null;

        public LoadoutManager(ManualLogSource log)
        {
            _log = log;
            _storage = new LoadoutStorage(log);
            _resolver = new ItemResolver(log);
            _switcher = new EquipmentSwitcher(log, _resolver);
        }

        public void TickCharacter()
        {
            Player current = Player.m_localPlayer;
            if (current == null)
            {
                if (_player != null) Unbind(true);
                return;
            }

            string id = GetCharacterId();
            if (string.IsNullOrEmpty(id)) return;
            if (_player == current && _characterId == id) return;

            if (_player != null) Unbind(true);
            _player = current;
            _characterId = id;
            _data = _storage.Load(id);
            MigrateTwoHandedFlags();
            int configuredSlots = 0;
            foreach (LoadoutSet set in _data.Sets)
            {
                if (set.MainHand != null) configuredSlots++;
                if (set.OffHand != null) configuredSlots++;
            }

            _log.LogInfo(
                $"Loaded five Berserker Sets loadouts for character id {id}: " +
                $"{configuredSlots} configured slot(s), last active set index {_data.LastActiveSetIndex}.");
            Changed?.Invoke();
        }

        public SwitchResult Cycle(int direction)
        {
            if (!IsReady)
            {
                _log.LogWarning("Cycle was requested before character loadouts were ready.");
                return SwitchResult.Blocked("Character loadouts are not ready.");
            }
            int current = _data.LastActiveSetIndex;
            int step = direction < 0 ? -1 : 1;
            int target = current < 0
                ? (step < 0 ? CharacterLoadoutData.SetCount - 1 : 0)
                : (current + step + CharacterLoadoutData.SetCount) % CharacterLoadoutData.SetCount;
            _log.LogInfo(
                $"Cycling {(step > 0 ? "forward" : "backward")} from active index {current} to Set {target + 1}.");
            SwitchResult result = _switcher.Apply(_player, _data.Sets[target], target);
            if (result.Processed)
            {
                _data.LastActiveSetIndex = target;
                _storage.Save(_data);
                Changed?.Invoke();
            }
            return result;
        }

        public bool Assign(int setIndex, LoadoutSlot slot, ItemDrop.ItemData item, out string message)
        {
            message = string.Empty;
            if (!IsReady || setIndex < 0 || setIndex >= CharacterLoadoutData.SetCount || item == null) return false;
            if (!ItemEligibility.IsEligible(item, slot))
            {
                message = slot == LoadoutSlot.MainHand
                    ? "That item cannot be assigned to a main-hand slot."
                    : "Only shields and torches can be assigned off-hand.";
                return false;
            }

            LoadoutSet set = _data.Sets[setIndex];
            if (slot == LoadoutSlot.MainHand && ItemEligibility.IsTwoHanded(item) && set.OffHand != null)
            {
                message = $"Set {setIndex + 1}: clear the off-hand slot before assigning a two-handed item.";
                _log.LogWarning(message);
                return false;
            }
            if (slot == LoadoutSlot.OffHand && set.MainHand != null)
            {
                bool duplicate;
                ItemDrop.ItemData savedMain = _resolver.Resolve(_player, set.MainHand, out duplicate);
                if (set.MainHand.TwoHanded || (savedMain != null && ItemEligibility.IsTwoHanded(savedMain)))
                {
                    message = $"Set {setIndex + 1}: an off-hand item cannot be paired with the saved two-handed item.";
                    _log.LogWarning(message);
                    return false;
                }
            }

            LoadoutItemReference captured = _resolver.Capture(_player, item);
            if (captured == null || string.IsNullOrEmpty(captured.PrefabName))
            {
                message = "This item has no stable prefab identity and cannot be assigned.";
                return false;
            }

            LoadoutItemReference other = slot == LoadoutSlot.MainHand ? set.OffHand : set.MainHand;
            if (other != null && other.ItemId == captured.ItemId)
            {
                message = "The same physical item cannot occupy both slots in one set.";
                return false;
            }

            if (slot == LoadoutSlot.MainHand) set.MainHand = captured;
            else set.OffHand = captured;
            _data.ExplicitlyEmpty = false;
            _storage.SaveSlot(_data.CharacterId, setIndex, slot, captured);
            _storage.Save(_data);
            _log.LogInfo(
                $"Assigned {slot} in Set {setIndex + 1}: name='{captured.DisplayName}', " +
                $"prefab='{captured.PrefabName}', id='{captured.ItemId}'.");
            Changed?.Invoke();
            message = $"Assigned {captured.DisplayName} to Set {setIndex + 1}.";
            return true;
        }

        public void Clear(int setIndex, LoadoutSlot slot)
        {
            if (!IsReady || setIndex < 0 || setIndex >= CharacterLoadoutData.SetCount) return;
            if (slot == LoadoutSlot.MainHand) _data.Sets[setIndex].MainHand = null;
            else _data.Sets[setIndex].OffHand = null;
            _data.ExplicitlyEmpty = CountConfiguredSlots(_data) == 0;
            _storage.SaveSlot(_data.CharacterId, setIndex, slot, null);
            _storage.Save(_data);
            _log.LogInfo($"Cleared {slot} in Set {setIndex + 1}.");
            Changed?.Invoke();
        }

        public ItemDrop.ItemData Resolve(LoadoutItemReference reference)
        {
            bool duplicate;
            return IsReady ? _resolver.Resolve(_player, reference, out duplicate) : null;
        }

        public void SaveAndReset()
        {
            Unbind(true);
        }

        private void Unbind(bool save)
        {
            if (save && _data != null) _storage.Save(_data);
            _player = null;
            _data = null;
            _characterId = null;
            Changed?.Invoke();
        }

        private static string GetCharacterId()
        {
            try
            {
                PlayerProfile profile = Game.instance?.GetPlayerProfile();
                if (profile == null) return null;
                long playerId = profile.GetPlayerID();
                return playerId > 0 ? playerId.ToString(CultureInfo.InvariantCulture) : null;
            }
            catch
            {
                return null;
            }
        }

        private static int CountConfiguredSlots(CharacterLoadoutData data)
        {
            int count = 0;
            if (data?.Sets == null) return count;
            foreach (LoadoutSet set in data.Sets)
            {
                if (set?.MainHand != null) count++;
                if (set?.OffHand != null) count++;
            }
            return count;
        }

        private void MigrateTwoHandedFlags()
        {
            bool changed = false;
            for (int index = 0; index < _data.Sets.Count; index++)
            {
                LoadoutItemReference reference = _data.Sets[index].MainHand;
                if (reference == null || reference.TwoHanded) continue;
                bool duplicate;
                ItemDrop.ItemData item = _resolver.Resolve(_player, reference, out duplicate);
                if (item == null || !ItemEligibility.IsTwoHanded(item)) continue;
                reference.TwoHanded = true;
                _storage.SaveSlot(_data.CharacterId, index, LoadoutSlot.MainHand, reference);
                changed = true;
            }
            if (changed) _storage.Save(_data);
        }
    }
}
