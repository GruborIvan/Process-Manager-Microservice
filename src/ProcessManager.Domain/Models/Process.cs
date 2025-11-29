using Newtonsoft.Json.Linq;

namespace ProcessManager.Domain.Models
{
    public class Process
    {
        public JObject Parameters { get; set; }
        public string Key { get; set; }
        public string StartUrl { get; set; }
    }
}
