using HarmonyLib;

namespace BerserkerSets.UI
{
    [HarmonyPatch]
    internal static class InventoryGuiPatches
    {
        [HarmonyPatch(typeof(InventoryGui), "Show")]
        [HarmonyPostfix]
        private static void ShowPostfix(InventoryGui __instance)
        {
            Plugin.Instance?.Panel?.Show(__instance);
        }

        [HarmonyPatch(typeof(InventoryGui), "Hide")]
        [HarmonyPrefix]
        private static void HidePrefix()
        {
            Plugin.Instance?.Panel?.Hide();
        }

        [HarmonyPatch(typeof(InventoryGui), "OnDestroy")]
        [HarmonyPrefix]
        private static void DestroyPrefix()
        {
            Plugin.Instance?.Panel?.DestroyView();
        }

        [HarmonyPatch(typeof(InventoryGui), "OnSelectedItem")]
        [HarmonyPrefix]
        private static bool SelectedItemPrefix(object[] __args)
        {
            if (__args == null || Plugin.Instance?.Panel == null) return true;
            foreach (object argument in __args)
            {
                var item = argument as ItemDrop.ItemData;
                if (item != null && Plugin.Instance.Panel.HandleSelectedInventoryItem(item)) return false;
            }
            return true;
        }
    }
}
