using Nest;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace SongGetterWebAPI.Models
{
    public class PathInfo
    {
        public string FilePath { get; set; }
        [JsonProperty(PropertyName = "fileName")]
        public string FileName { get; set; }

        public bool IsError { get; set; }
    }
}