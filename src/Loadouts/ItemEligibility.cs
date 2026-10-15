namespace BerserkerSets.Loadouts
{
    internal enum LoadoutSlot
    {
        MainHand,
        OffHand
    }

    internal static class ItemEligibility
    {
        public static bool IsEligible(ItemDrop.ItemData item, LoadoutSlot slot)
        {
            if (item?.m_shared == null) return false;

            ItemDrop.ItemData.ItemType type = item.m_shared.m_itemType;
            if (slot == LoadoutSlot.OffHand)
            {
                return type == ItemDrop.ItemData.ItemType.Shield ||
                       type == ItemDrop.ItemData.ItemType.Torch;
            }

            return type == ItemDrop.ItemData.ItemType.OneHandedWeapon ||
                   type == ItemDrop.ItemData.ItemType.TwoHandedWeapon ||
                   type == ItemDrop.ItemData.ItemType.Bow ||
                   type == ItemDrop.ItemData.ItemType.Tool ||
                   type == ItemDrop.ItemData.ItemType.Torch;
        }

        public static bool IsTwoHanded(ItemDrop.ItemData item)
        {
            if (item?.m_shared == null) return false;
            ItemDrop.ItemData.ItemType type = item.m_shared.m_itemType;
            return type == ItemDrop.ItemData.ItemType.TwoHandedWeapon ||
                   type == ItemDrop.ItemData.ItemType.Bow;
        }

        public static bool IsUsable(ItemDrop.ItemData item)
        {
            return item?.m_shared != null &&
                   (!item.m_shared.m_useDurability || item.m_durability > 0f);
        }
    }
}
