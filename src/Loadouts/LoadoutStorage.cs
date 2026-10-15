using System;
using System.IO;
using BepInEx;
using BepInEx.Logging;
using BerserkerSets.Models;
using Newtonsoft.Json;

namespace BerserkerSets.Loadouts
{
    internal sealed class LoadoutStorage
    {
        private readonly string _directory;
        private readonly ManualLogSource _log;

        public LoadoutStorage(ManualLogSource log)
        {
            _log = log;
            _directory = Path.Combine(Paths.ConfigPath, "BerserkerSets", "characters");
        }

        public CharacterLoadoutData Load(string characterId)
        {
            string path = GetPath(characterId);
            try
            {
                if (!File.Exists(path))
                {
                    _log.LogInfo($"No loadout file exists for character {characterId}; creating an empty in-memory configuration at '{path}'.");
                    return CharacterLoadoutData.Create(characterId);
                }
                var data = JsonConvert.DeserializeObject<CharacterLoadoutData>(File.ReadAllText(path));
                if (data == null) throw new JsonException("The save document was empty.");
                if (data.SchemaVersion > CharacterLoadoutData.CurrentSchemaVersion)
                {
                    _log.LogWarning($"Loadout save '{path}' uses newer schema {data.SchemaVersion}; loading known fields conservatively.");
                }
                data.CharacterId = characterId;
                data.Normalize();
                if (CountConfiguredSlots(data) == 0 && !data.ExplicitlyEmpty)
                {
                    CharacterLoadoutData recovered = TryLoadBackup(characterId, path + ".lastgood.json");
                    if (recovered == null || CountConfiguredSlots(recovered) == 0)
                    {
                        recovered = TryLoadBackup(characterId, path + ".bak");
                    }
                    if (recovered != null && CountConfiguredSlots(recovered) > 0)
                    {
                        _log.LogWarning(
                            $"Primary loadout file '{path}' was unexpectedly empty; recovered " +
                            $"{CountConfiguredSlots(recovered)} configured slot(s) from its backup.");
                        data = recovered;
                    }
                }
                LoadSlotRecords(data);
                PersistConfiguredSlotRecords(data);
                _log.LogInfo($"Read loadout file '{path}' with {CountConfiguredSlots(data)} configured slot(s).");
                return data;
            }
            catch (Exception ex)
            {
                _log.LogError($"Could not load character loadouts from '{path}': {ex}");
                return CharacterLoadoutData.Create(characterId);
            }
        }

        public bool Save(CharacterLoadoutData data)
        {
            if (data == null || string.IsNullOrWhiteSpace(data.CharacterId)) return false;
            string path = GetPath(data.CharacterId);
            string temporary = path + ".tmp";
            string backup = path + ".bak";
            try
            {
                Directory.CreateDirectory(_directory);
                data.Normalize();
                int configuredSlots = CountConfiguredSlots(data);
                string serialized = JsonConvert.SerializeObject(data, Formatting.Indented);
                File.WriteAllText(temporary, serialized);
                if (File.Exists(path)) File.Replace(temporary, path, backup, true);
                else File.Move(temporary, path);
                if (configuredSlots > 0)
                {
                    // This snapshot is deliberately never replaced by an empty autosave.
                    File.WriteAllText(path + ".lastgood.tmp", serialized);
                    if (File.Exists(path + ".lastgood.json"))
                    {
                        File.Replace(path + ".lastgood.tmp", path + ".lastgood.json", null, true);
                    }
                    else File.Move(path + ".lastgood.tmp", path + ".lastgood.json");
                }
                _log.LogInfo(
                    $"Saved {configuredSlots} configured loadout slot(s) for character " +
                    $"{data.CharacterId} to '{path}'.");
                return true;
            }
            catch (Exception ex)
            {
                _log.LogError($"Could not save character loadouts to '{path}': {ex}");
                try { if (File.Exists(temporary)) File.Delete(temporary); }
                catch (Exception cleanupError) { _log.LogWarning($"Could not remove temporary loadout file: {cleanupError.Message}"); }
                try { if (File.Exists(path + ".lastgood.tmp")) File.Delete(path + ".lastgood.tmp"); }
                catch (Exception cleanupError) { _log.LogWarning($"Could not remove temporary recovery file: {cleanupError.Message}"); }
                return false;
            }
        }

        public bool SaveSlot(string characterId, int setIndex, LoadoutSlot slot, LoadoutItemReference reference)
        {
            string path = GetSlotPath(characterId, setIndex, slot);
            string temporary = path + ".tmp";
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(path));
                if (reference == null)
                {
                    if (File.Exists(path)) File.Delete(path);
                    _log.LogInfo($"Removed durable slot record '{path}'.");
                    return true;
                }

                File.WriteAllText(temporary, JsonConvert.SerializeObject(reference, Formatting.Indented));
                if (File.Exists(path)) File.Replace(temporary, path, null, true);
                else File.Move(temporary, path);
                _log.LogInfo($"Saved durable slot record '{path}'.");
                return true;
            }
            catch (Exception ex)
            {
                _log.LogError($"Could not save durable slot record '{path}': {ex}");
                try { if (File.Exists(temporary)) File.Delete(temporary); }
                catch { }
                return false;
            }
        }

        private string GetPath(string characterId)
        {
            return Path.Combine(_directory, SanitizeCharacterId(characterId) + ".json");
        }

        private string GetSlotPath(string characterId, int setIndex, LoadoutSlot slot)
        {
            string name = slot == LoadoutSlot.MainHand ? "main" : "off";
            return Path.Combine(
                _directory,
                "slots",
                SanitizeCharacterId(characterId),
                $"set-{setIndex + 1}-{name}.json");
        }

        private static string SanitizeCharacterId(string characterId)
        {
            foreach (char invalid in Path.GetInvalidFileNameChars()) characterId = characterId.Replace(invalid, '_');
            return characterId;
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

        private CharacterLoadoutData TryLoadBackup(string characterId, string backupPath)
        {
            try
            {
                if (!File.Exists(backupPath)) return null;
                var backup = JsonConvert.DeserializeObject<CharacterLoadoutData>(File.ReadAllText(backupPath));
                if (backup == null) return null;
                backup.CharacterId = characterId;
                backup.Normalize();
                return backup;
            }
            catch (Exception ex)
            {
                _log.LogWarning($"Could not read loadout backup '{backupPath}': {ex.Message}");
                return null;
            }
        }

        private void LoadSlotRecords(CharacterLoadoutData data)
        {
            for (int setIndex = 0; setIndex < CharacterLoadoutData.SetCount; setIndex++)
            {
                LoadSlotRecord(data, setIndex, LoadoutSlot.MainHand);
                LoadSlotRecord(data, setIndex, LoadoutSlot.OffHand);
            }
        }

        private void LoadSlotRecord(CharacterLoadoutData data, int setIndex, LoadoutSlot slot)
        {
            string path = GetSlotPath(data.CharacterId, setIndex, slot);
            if (!File.Exists(path)) return;
            try
            {
                var reference = JsonConvert.DeserializeObject<LoadoutItemReference>(File.ReadAllText(path));
                if (reference == null || !reference.IsConfigured) return;
                if (slot == LoadoutSlot.MainHand) data.Sets[setIndex].MainHand = reference;
                else data.Sets[setIndex].OffHand = reference;
                data.ExplicitlyEmpty = false;
                _log.LogInfo($"Loaded durable slot record '{path}'.");
            }
            catch (Exception ex)
            {
                _log.LogWarning($"Could not read durable slot record '{path}': {ex.Message}");
            }
        }

        private void PersistConfiguredSlotRecords(CharacterLoadoutData data)
        {
            for (int setIndex = 0; setIndex < CharacterLoadoutData.SetCount; setIndex++)
            {
                LoadoutSet set = data.Sets[setIndex];
                if (set.MainHand != null) SaveSlot(data.CharacterId, setIndex, LoadoutSlot.MainHand, set.MainHand);
                if (set.OffHand != null) SaveSlot(data.CharacterId, setIndex, LoadoutSlot.OffHand, set.OffHand);
            }
        }
    }
}
