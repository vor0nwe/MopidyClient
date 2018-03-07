using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MopidyTray.Models
{
    class BaseModel
    {
        public string Model {
            get
            {
                return "__" + this.GetType().Name.ToLowerInvariant() + "__";
            }
            set
            {
                if (value != this.Model)
                    throw new NotSupportedException($"Model {value} does not match type {this.GetType().Name}");
            }
        }

        private const long UnixEpochStart = 621355968000000000; // new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc).Ticks;
        protected DateTime IntToDateTime(long pythonDateTime)
        {
            // TODO: check if this gives the correct date/time
            return new DateTime(UnixEpochStart + pythonDateTime, DateTimeKind.Utc);
        }
        protected long DateTimeToInt(DateTime dateTime)
        {
            // TODO: check if this gives the correct timestamp
            return dateTime.ToUniversalTime().Ticks - UnixEpochStart;
        }
    }

    class NamedModel : BaseModel
    {
        public string Name { get; set; }
        public string Uri { get; set; }

        [JsonIgnore]
        public Uri UriObject
        {
            get
            {
                return new Uri(Uri);
            }
            set
            {
                Uri = value.ToString();
            }
        }
    }
}
