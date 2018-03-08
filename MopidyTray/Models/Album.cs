using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MopidyTray.Models
{
    class Album : NamedModel
    {
        public List<Artist> Artists { get; } = new List<Artist>();
        public int? NumTracks { get; set; }
        public int? NumDiscs { get; set; }
        public DateTime? Date { get; set; }
        public string MusicBrainzID { get; set; }
    }
}
