using System.Collections.Generic;
using Newtonsoft.Json;

namespace BerserkerSets.Models
{
    public sealed class CharacterLoadoutData
    {
        public const int CurrentSchemaVersion = 4;
        public const int SetCount = 5;

        [JsonProperty("schemaVersion")]
        public int SchemaVersion { get; set; } = CurrentSchemaVersion;

        [JsonProperty("characterId")]
        public string CharacterId { get; set; } = string.Empty;

        [JsonProperty("sets")]
        public List<LoadoutSet> Sets { get; set; } = CreateEmptySets();

        [JsonProperty("lastActiveSetIndex")]
        public int LastActiveSetIndex { get; set; } = -1;

        [JsonProperty("explicitlyEmpty")]
        public bool ExplicitlyEmpty { get; set; }

        public static CharacterLoadoutData Create(string characterId)
        {
            return new CharacterLoadoutData { CharacterId = characterId, ExplicitlyEmpty = true };
        }

        public void Normalize()
        {
            SchemaVersion = CurrentSchemaVersion;
            Sets = Sets ?? new List<LoadoutSet>();
            while (Sets.Count < SetCount) Sets.Add(new LoadoutSet());
            if (Sets.Count > SetCount) Sets.RemoveRange(SetCount, Sets.Count - SetCount);
            for (int i = 0; i < Sets.Count; ++i) Sets[i] = Sets[i] ?? new LoadoutSet();
            if (LastActiveSetIndex < -1 || LastActiveSetIndex >= SetCount) LastActiveSetIndex = -1;
        }

        private static List<LoadoutSet> CreateEmptySets()
        {
            var result = new List<LoadoutSet>(SetCount);
            for (int i = 0; i < SetCount; ++i) result.Add(new LoadoutSet());
            return result;
        }
    }
}
