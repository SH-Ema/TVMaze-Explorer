using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Formats.Asn1.AsnWriter;

namespace OOP_DZ5.Components
{
    public class Episode
    {
        public int id { get; set; }
        public string name { get; set; }
        public int season { get; set; }
        public int number { get; set; }
        public int? runtime { get; set; }

        public override string ToString()
        {
            return $"{number}. {name} ({runtime ?? 0} min)";
        }

    }
}