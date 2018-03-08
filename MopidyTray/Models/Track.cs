using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MopidyTray.Models
{
    class Track : NamedModel
    {
        public List<Artist> Artists { get; } = new List<Artist>();
        public Album Album { get; set; }
        public List<Artist> Composers { get; } = new List<Artist>();
        public List<Artist> Performers { get; } = new List<Artist>();
        public string Genre { get; set; }
        public int? TrackNo { get; set; }
        public int? DiscNo { get; set; }
        public string Date { get; set; }
        public int? Length { get; set; }
        public int Bitrate { get; set; }
        public string Comment { get; set; }
        public string MusicBrainzID { get; set; }
        public long? LastModified { get; set; }

        [JsonIgnore]
        public DateTime? DateLastModified
        {
            get
            {
                if (LastModified == null)
                    return null;
                else
                    return Utils.IntToDateTime(LastModified.Value);
            }
            set
            {
                if (value == null)
                    LastModified = null;
                else
                    LastModified = Utils.DateTimeToInt(value.Value);
            }
        }
    }

    class TlTrack : BaseModel
    {
        [JsonProperty(PropertyName = "tlid")]
        public int TracklistID { get; set; }
        public Track Track { get; set; }
    }
}
