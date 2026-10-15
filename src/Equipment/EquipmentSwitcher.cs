using System;
using System.Collections.Generic;
using BepInEx.Logging;
using BerserkerSets.Loadouts;
using BerserkerSets.Models;
using UnityEngine;

namespace BerserkerSets.Equipment
{
    internal sealed class SwitchResult
    {
        public bool Processed { get; private set; }
        public string Message { get; private set; }

        public static SwitchResult Done(string message) => new SwitchResult { Processed = true, Message = message };
        public static SwitchResult Blocked(string message) => new SwitchResult { Processed = false, Message = message };
    }

    internal sealed class EquipmentSwitcher
    {
        private const float MinimumSwitchInterval = 0.2f;
        private readonly ManualLogSource _log;
        private readonly ItemResolver _resolver;
        private float _lastSwitchTime = -10f;

        public EquipmentSwitcher(ManualLogSource log, ItemResolver resolver)
        {
            _log = log;
            _resolver = resolver;
        }

        public SwitchResult Apply(Player player, LoadoutSet set, int setIndex)
        {
            string reason;
            if (!PlayerStateValidator.CanSwitch(player, out reason))
            {
                _log.LogWarning($"Set {setIndex + 1} switch blocked by player state: {reason}");
                return SwitchResult.Blocked(reason);
            }
            if (Time.unscaledTime - _lastSwitchTime < MinimumSwitchInterval)
            {
                _log.LogInfo($"Set {setIndex + 1} switch ignored by the {MinimumSwitchInterval:0.00}s debounce.");
                return SwitchResult.Blocked(string.Empty);
            }
            if (set == null)
            {
                _log.LogWarning($"Set {setIndex + 1} switch failed because the loadout record is null.");
                return SwitchResult.Blocked("The selected loadout is unavailable.");
            }

            _lastSwitchTime = Time.unscaledTime;
            _log.LogInfo(
                $"Applying Set {setIndex + 1}: main={Describe(set.MainHand)}, off={Describe(set.OffHand)}.");
            bool mainMissing;
            bool offMissing;
            ItemDrop.ItemData main = Resolve(player, set.MainHand, LoadoutSlot.MainHand, out mainMissing);
            ItemDrop.ItemData off = Resolve(player, set.OffHand, LoadoutSlot.OffHand, out offMissing);
            ItemDrop.ItemData previousRight = player.RightItem;
            ItemDrop.ItemData previousLeft = player.LeftItem;

            bool suppressOff = main != null && ItemEligibility.IsTwoHanded(main);
            if (mainMissing && previousRight != null && ItemEligibility.IsTwoHanded(previousRight) && off != null)
            {
                suppressOff = true;
            }

            try
            {
                if (main != null)
                {
                    UnequipDifferent(player, previousRight, main);
                    if (suppressOff) UnequipDifferent(player, player.LeftItem, main);
                    if (!IsEquipped(player, main) && !player.EquipItem(main, true))
                    {
                        Restore(player, previousRight, previousLeft);
                        return SwitchResult.Blocked($"Set {setIndex + 1}: could not equip {ItemResolver.GetDisplayName(main)}.");
                    }
                }
                else if (set.MainHand == null)
                {
                    Unequip(player, player.RightItem);
                }

                if (!suppressOff)
                {
                    if (off != null)
                    {
                        UnequipDifferent(player, player.LeftItem, off);
                        if (!IsEquipped(player, off) && !player.EquipItem(off, true))
                        {
                            Restore(player, previousRight, previousLeft);
                            return SwitchResult.Blocked($"Set {setIndex + 1}: could not equip {ItemResolver.GetDisplayName(off)}.");
                        }
                    }
                    else if (set.OffHand == null)
                    {
                        Unequip(player, player.LeftItem);
                    }
                }

                string message = BuildMessage(set, setIndex, main, off, mainMissing, offMissing, suppressOff);
                _log.LogInfo(
                    $"Set {setIndex + 1} applied: mainResolved={main != null}, offResolved={off != null}, " +
                    $"mainMissing={mainMissing}, offMissing={offMissing}, offSuppressed={suppressOff}.");
                return SwitchResult.Done(message);
            }
            catch (Exception ex)
            {
                _log.LogError($"Equipment switch for Set {setIndex + 1} failed: {ex}");
                Restore(player, previousRight, previousLeft);
                return SwitchResult.Blocked($"Set {setIndex + 1}: equipment change failed; see the BepInEx log.");
            }
        }

        private ItemDrop.ItemData Resolve(Player player, LoadoutItemReference reference, LoadoutSlot slot, out bool missing)
        {
            missing = false;
            if (reference == null) return null;
            bool duplicate;
            ItemDrop.ItemData item = _resolver.Resolve(player, reference, out duplicate);
            if (item == null || !ItemEligibility.IsEligible(item, slot) || !ItemEligibility.IsUsable(item))
            {
                missing = true;
                return null;
            }
            return item;
        }

        private static void UnequipDifferent(Player player, ItemDrop.ItemData current, ItemDrop.ItemData desired)
        {
            if (current != null && !ReferenceEquals(current, desired)) player.UnequipItem(current, true);
        }

        private static void Unequip(Player player, ItemDrop.ItemData item)
        {
            if (item != null) player.UnequipItem(item, true);
        }

        private static bool IsEquipped(Player player, ItemDrop.ItemData item)
        {
            return ReferenceEquals(player.RightItem, item) || ReferenceEquals(player.LeftItem, item);
        }

        private void Restore(Player player, ItemDrop.ItemData right, ItemDrop.ItemData left)
        {
            try
            {
                if (right != null && InventoryContains(player, right) && !IsEquipped(player, right)) player.EquipItem(right, true);
                if (left != null && InventoryContains(player, left) && !IsEquipped(player, left)) player.EquipItem(left, true);
            }
            catch (Exception ex)
            {
                _log.LogWarning($"Could not fully restore the previous equipment after a rejected switch: {ex.Message}");
            }
        }

        private static bool InventoryContains(Player player, ItemDrop.ItemData wanted)
        {
            foreach (ItemDrop.ItemData item in player.GetInventory().GetAllItems())
            {
                if (ReferenceEquals(item, wanted)) return true;
            }
            return false;
        }

        private static string BuildMessage(LoadoutSet set, int index, ItemDrop.ItemData main, ItemDrop.ItemData off,
            bool mainMissing, bool offMissing, bool suppressOff)
        {
            var parts = new List<string>(2);
            if (main != null) parts.Add(ItemResolver.GetDisplayName(main));
            else if (mainMissing) parts.Add("Missing main-hand item");

            if (suppressOff && set.OffHand != null) parts.Add("off-hand incompatible");
            else if (off != null) parts.Add(ItemResolver.GetDisplayName(off));
            else if (offMissing) parts.Add("Missing off-hand item");

            if (parts.Count == 0) return $"Set {index + 1} (Empty)";
            return $"Equipped Set {index + 1}: {string.Join(" + ", parts)}";
        }

        private static string Describe(LoadoutItemReference reference)
        {
            if (reference == null) return "<empty>";
            return $"'{reference.DisplayName}' [prefab={reference.PrefabName}, id={reference.ItemId}]";
        }
    }
}
