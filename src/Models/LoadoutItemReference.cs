using Newtonsoft.Json;

namespace BerserkerSets.Models
{
    public sealed class LoadoutItemReference
    {
        [JsonProperty("itemId")]
        public string ItemId { get; set; } = string.Empty;

        [JsonProperty("prefab")]
        public string PrefabName { get; set; } = string.Empty;

        [JsonProperty("displayName")]
        public string DisplayName { get; set; } = string.Empty;

        [JsonProperty("twoHanded")]
        public bool TwoHanded { get; set; }

        [JsonIgnore]
        public bool IsConfigured => !string.IsNullOrWhiteSpace(ItemId) && !string.IsNullOrWhiteSpace(PrefabName);
    }
}
