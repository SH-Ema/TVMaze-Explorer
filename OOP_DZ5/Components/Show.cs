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
        public string name { get; set; }
        public string language { get; set; }
        public string status { get; set; }
        public string summary { get; set; }

        public List<Season> Seasons { get; set; } = new();

        public override string ToString() => name;
    }
}
