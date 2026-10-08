using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OOP_DZ5.Components
{
    public class Show
    {
        public int id { get; set; }
        public string name { get; set; } = string.Empty;
        public string language { get; set; } = string.Empty;
        public string status { get; set; } = string.Empty;
        public string summary { get; set; } = string.Empty;

        public List<Season> Seasons { get; set; } = new();

        public override string ToString() => name;
    }
}
