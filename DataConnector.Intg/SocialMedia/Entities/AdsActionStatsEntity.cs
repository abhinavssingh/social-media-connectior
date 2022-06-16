using Newtonsoft.Json;

namespace DataConnector.Intg.SocialMedia.Entities
{

    public class AdsActionStatsEntity
    {        
        [JsonProperty("1d_click")]
        public string OneD_Click { get; set; }

        [JsonProperty("1d_view")]
        public string OneD_View { get; set; }

        [JsonProperty("28d_click")]
        public string TwentyEightD_Click { get; set; }

        [JsonProperty("28d_view")]
        public string TwentyEightD_View { get; set; }

        [JsonProperty("7d_click")]
        public string SevenD_Click { get; set; }

        [JsonProperty("7d_view")]
        public string SevenD_View { get; set; }

        [JsonProperty("action_canvas_component_name")]
        public string Action_Canvas_Component_Name { get; set; }

        [JsonProperty("action_carousel_card_id")]
        public string Action_Carousel_Card_Id { get; set; }

        [JsonProperty("action_carousel_card_name")]
        public string Action_Carousel_Card_Name { get; set; }

        [JsonProperty("action_destination")]
        public string Action_Destination { get; set; }

        [JsonProperty("action_device")]
        public string Action_Device { get; set; }

        [JsonProperty("action_reaction")]
        public string Action_Reaction { get; set; }

        [JsonProperty("action_target_id")]
        public string Action_Target_Id { get; set; }

        [JsonProperty("action_type")]
        public string Action_Type { get; set; }
        [JsonProperty("action_video_sound")]
        public string Action_Video_Sound { get; set; }

        [JsonProperty("action_video_type")]
        public string Action_Video_Type { get; set; }

        [JsonProperty("dda")]
        public string Dda { get; set; }

        [JsonProperty("inline")]
        public string Inline { get; set; }

        [JsonProperty("value")]
        public string Value { get; set; }
    }
}
