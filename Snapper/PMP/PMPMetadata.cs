using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Snapper.PMP
{
    internal class PMPMetadata
    {
        public int FileVersion { get; set; } = 3;
        public string Name { get; set; } = "";
        public string Author { get; set; } = "";
        public string Description { get; set; } = "Yoink!";
        public string Version { get; set; } = "1.0.0";
        public string Website { get; set; } = "";
        public string[] ModTags { get; set; } = { };
    }
}
