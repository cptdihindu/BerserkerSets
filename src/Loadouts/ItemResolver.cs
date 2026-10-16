using System;
using System.Collections.Generic;
using BepInEx.Logging;
using BerserkerSets.Models;
using Jotunn.Managers;

namespace BerserkerSets.Loadouts
{
    internal sealed class ItemResolver
    {
        public const string ItemIdKey = "dev.berserkersets.quickloadouts.item-id";

        private readonly ManualLogSource _log;

        public ItemResolver(ManualLogSource log)
        {
            _log = log;
        }

        public LoadoutItemReference Capture(Player player, ItemDrop.ItemData selected)
        {
            if (player == null || selected == null) return null;

            Dictionary<string, string> data = selected.m_customData;
            if (data == null)
            {
                data = new Dictionary<string, string>();
                selected.m_customData = data;
            }

            string id;
            bool assignedNewId = false;
            if (!data.TryGetValue(ItemIdKey, out id) || string.IsNullOrWhiteSpace(id) ||
                IdentifierBelongsToAnotherItem(player, selected, id))
            {
                id = Guid.NewGuid().ToString("N");
                data[ItemIdKey] = id;
                assignedNewId = true;
            }

            var captured = new LoadoutItemReference
            {
                ItemId = id,
                PrefabName = GetPrefabName(selected),
                DisplayName = GetDisplayName(selected),
                TwoHanded = ItemEligibility.IsTwoHanded(selected)
            };
            _log.LogInfo(
                $"Captured item reference: name='{captured.DisplayName}', prefab='{captured.PrefabName}', " +
                $"id='{captured.ItemId}', newId={assignedNewId}.");
            return captured;
        }

        public ItemDrop.ItemData Resolve(Player player, LoadoutItemReference reference, out bool duplicateIdentifier)
        {
            duplicateIdentifier = false;
            if (player == null || reference == null || !reference.IsConfigured) return null;

            ItemDrop.ItemData match = null;
            foreach (ItemDrop.ItemData item in player.GetInventory().GetAllItems())
            {
                if (item?.m_customData == null) continue;
                string id;
                if (!item.m_customData.TryGetValue(ItemIdKey, out id) || id != reference.ItemId) continue;
                if (!string.Equals(GetPrefabName(item), reference.PrefabName, StringComparison.Ordinal)) continue;

                if (match == null) match = item;
                else duplicateIdentifier = true;
            }

            if (duplicateIdentifier)
            {
                _log.LogWarning($"Multiple inventory items have Berserker Sets id '{reference.ItemId}'. Using the first inventory match.");
            }
            if (match == null)
            {
                _log.LogWarning(
                    $"Could not resolve saved item: name='{reference.DisplayName}', " +
                    $"prefab='{reference.PrefabName}', id='{reference.ItemId}'.");
            }
            return match;
        }

        public static string GetPrefabName(ItemDrop.ItemData item)
        {
            if (item?.m_dropPrefab == null) return string.Empty;
            string name = item.m_dropPrefab.name ?? string.Empty;
            return name.EndsWith("(Clone)", StringComparison.Ordinal)
                ? name.Substring(0, name.Length - "(Clone)".Length).TrimEnd()
                : name;
        }

        public static string GetDisplayName(ItemDrop.ItemData item)
        {
            string token = item?.m_shared?.m_name;
            if (string.IsNullOrWhiteSpace(token)) return GetPrefabName(item);
            return LocalizationManager.Instance != null ? LocalizationManager.Instance.TryTranslate(token) : token;
        }

        private static bool IdentifierBelongsToAnotherItem(Player player, ItemDrop.ItemData selected, string id)
        {
            foreach (ItemDrop.ItemData item in player.GetInventory().GetAllItems())
            {
                if (ReferenceEquals(item, selected) || item?.m_customData == null) continue;
                string otherId;
                if (item.m_customData.TryGetValue(ItemIdKey, out otherId) && otherId == id) return true;
            }
            return false;
        }
    }
}
