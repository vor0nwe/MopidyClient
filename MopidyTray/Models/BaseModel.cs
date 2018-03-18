using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MopidyTray.Models
{
    public class BaseModel
    {
        [JsonProperty(PropertyName = "__model__")]
        public string Model {
            get
            {
                return this.GetType().Name;
            }
            set
            {
                if (!value.Equals(this.Model, StringComparison.InvariantCultureIgnoreCase))
                    throw new NotSupportedException($"Model {value} does not match type {this.Model}");
            }
        }


        public static class Utils
        {
            private const long UnixEpochStart = 621355968000000000; // new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc).Ticks;

            public static DateTime IntToDateTime(long pythonDateTime)
            {
                return new DateTime(UnixEpochStart + pythonDateTime, DateTimeKind.Utc);
            }

            public static long DateTimeToInt(DateTime dateTime)
            {
                return dateTime.ToUniversalTime().Ticks - UnixEpochStart;
            }
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
