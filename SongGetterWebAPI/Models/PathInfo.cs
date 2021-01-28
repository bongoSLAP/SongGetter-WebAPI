using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace SongGetterWebAPI.Models
{
    public class PathInfo
    {
        public string FilePath { get; set; }

        public string FileName { get; set; }

        public bool IsError { get; set; }
    }
}