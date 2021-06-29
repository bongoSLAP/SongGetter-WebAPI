using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace SongGetterWebAPI.Models
{
    public class SongRequest
    {
        [JsonProperty(PropertyName = "url")]
        public string Url { get; set; }
        [JsonProperty(PropertyName = "isPlaylist")]
        public bool IsPlaylist { get; set; }
    }
}