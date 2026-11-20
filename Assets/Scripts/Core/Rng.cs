using System;
using System.Collections.Generic;

namespace Tarot.Core
{
    public sealed class Rng
    {
        readonly Random m_Random;

        public Rng(int seed)
        {
            m_Random = new Random(seed);
        }

        public int Next(int maxExclusive) => maxExclusive <= 0 ? 0 : m_Random.Next(maxExclusive);
        public bool Chance(int percent) => m_Random.Next(100) < percent;

        public void Shuffle<T>(IList<T> list)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = m_Random.Next(i + 1);
                T tmp = list[i];
                list[i] = list[j];
                list[j] = tmp;
            }
        }
    }
}
