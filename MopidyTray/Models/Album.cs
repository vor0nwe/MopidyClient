using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MopidyTray.Models
{
    class Album : NamedModel
    {
        List<Artist> Artists { get; } = new List<Artist>();
        int? NumTracks { get; set; }
        int? NumDiscs { get; set; }
        DateTime? Date { get; set; }
        string MusicBrainzID { get; set; }
    }
}
