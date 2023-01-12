using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Snapper.Models
{
    internal class SnapshotInfo
    {
        public string GlamourerString { get; set; } = string.Empty;
        public string CustomizeData { get; set; } = string.Empty;
        public string ManipulationString { get; set; } = string.Empty;
        public Dictionary<string, List<string>> FileReplacements { get; set; } = new();
    }
}
