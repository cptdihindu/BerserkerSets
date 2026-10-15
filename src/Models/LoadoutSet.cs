using Newtonsoft.Json;

namespace BerserkerSets.Models
{
    public sealed class LoadoutSet
    {
        [JsonProperty("mainHand")]
        public LoadoutItemReference MainHand { get; set; }

        [JsonProperty("offHand")]
        public LoadoutItemReference OffHand { get; set; }

        [JsonIgnore]
        public bool IsEmpty => MainHand == null && OffHand == null;
    }
}
