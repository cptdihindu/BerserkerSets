using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace BerserkerSets.Equipment
{
    internal static class PlayerStateValidator
    {
        private static readonly Dictionary<string, MethodInfo> PlayerChecks = new Dictionary<string, MethodInfo>();
        private static readonly FieldInfo JotunnInputBlockedField = typeof(Jotunn.Managers.GUIManager).GetField(
            "InputBlocked", BindingFlags.Static | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

        public static bool CanSwitch(Player player, out string reason)
        {
            reason = string.Empty;
            if (player == null || player != Player.m_localPlayer)
            {
                reason = "No local player is ready.";
                return false;
            }
            if (player.IsDead())
            {
                reason = "Loadouts cannot be changed while dead.";
                return false;
            }
            if (PlayerBool(player, "InCutscene") || PlayerBool(player, "IsTeleporting") ||
                PlayerBool(player, "IsSleeping") || PlayerBool(player, "IsAttached"))
            {
                reason = "Loadouts cannot be changed in the current player state.";
                return false;
            }
            if (IsInterfaceOpen())
            {
                reason = "Close the current interface before changing loadouts.";
                return false;
            }
            return true;
        }

        private static bool IsInterfaceOpen()
        {
            if (IsJotunnInputBlocked()) return true;
            return InstanceBool("InventoryGui", "instance", "IsVisible") ||
                   InstanceBool("Minimap", "instance", "IsOpen") ||
                   InstanceBool("Menu", "instance", "IsVisible") ||
                   InstanceBool("Console", "instance", "IsVisible") ||
                   InstanceBool("Chat", "instance", "HasFocus") ||
                   StaticBool("TextInput", "IsVisible");
        }

        private static bool IsJotunnInputBlocked()
        {
            try
            {
                if (JotunnInputBlockedField == null || JotunnInputBlockedField.FieldType != typeof(bool)) return false;
                object target = JotunnInputBlockedField.IsStatic ? null : (object)Jotunn.Managers.GUIManager.Instance;
                return target != null || JotunnInputBlockedField.IsStatic
                    ? (bool)JotunnInputBlockedField.GetValue(target)
                    : false;
            }
            catch
            {
                return false;
            }
        }

        private static bool PlayerBool(Player player, string methodName)
        {
            try
            {
                MethodInfo method;
                if (!PlayerChecks.TryGetValue(methodName, out method))
                {
                    method = typeof(Player).GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, Type.EmptyTypes, null);
                    PlayerChecks[methodName] = method;
                }
                return method != null && method.ReturnType == typeof(bool) && (bool)method.Invoke(player, null);
            }
            catch { return true; }
        }

        private static bool InstanceBool(string typeName, string instanceMember, string methodName)
        {
            try
            {
                Type type = typeof(Player).Assembly.GetType(typeName);
                if (type == null) return false;
                object instance = GetStaticMember(type, instanceMember);
                if (instance == null) return false;
                var unityObject = instance as UnityEngine.Object;
                if (unityObject != null && !unityObject) return false;
                MethodInfo method = type.GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, Type.EmptyTypes, null);
                return method != null && method.ReturnType == typeof(bool) && (bool)method.Invoke(instance, null);
            }
            catch { return true; }
        }

        private static bool StaticBool(string typeName, string methodName)
        {
            try
            {
                Type type = typeof(Player).Assembly.GetType(typeName);
                MethodInfo method = type?.GetMethod(methodName, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic, null, Type.EmptyTypes, null);
                return method != null && method.ReturnType == typeof(bool) && (bool)method.Invoke(null, null);
            }
            catch { return true; }
        }

        private static object GetStaticMember(Type type, string name)
        {
            FieldInfo field = type.GetField(name, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            if (field != null) return field.GetValue(null);
            PropertyInfo property = type.GetProperty(name, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            return property?.GetValue(null, null);
        }
    }
}
