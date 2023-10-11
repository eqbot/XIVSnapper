using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Snapper.PMP
{
    internal class PMPDefaultMod
    {
        public string Name { get; } = "Default";
        public string Description { get; } = "";
        public Dictionary<string, string> Files { get; set; } = new();
        public Dictionary<string, string> FileSwaps { get; set; } = new();
        public List<PMPManipulationEntry> Manipulations { get; set; } = new();
    }
}
