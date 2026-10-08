using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;



namespace OOP_DZ5.Components
{
    public class Season : IEnumerable<Episode>
    {
        private readonly List<Episode> episodes = new();

        public int SeasonNumber { get; set; }

        public Season(int seasonNumber) => SeasonNumber = seasonNumber;

        public void Add(Episode episode) => episodes.Add(episode);

        public int TotalDuration() => episodes.Sum(e => e.runtime ?? 0);

        public IEnumerator<Episode> GetEnumerator() => episodes.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public override string ToString() => $"Season {SeasonNumber}";
    }
}