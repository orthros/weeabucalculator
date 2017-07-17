using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WeeabuCalculator
{
    public class TopScoreTracker : IEnumerable<(SimulationState state, float score)?>
    {
        public (SimulationState state, float score)? TopScore
        { get { return collection[0]; } }

        private (SimulationState state, float score)?[] collection;

        private object _hobbes;

        public IEnumerator<(SimulationState state, float score)?> GetEnumerator()
        {
            return (IEnumerator<(SimulationState state, float score)?>)collection.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return collection.GetEnumerator();
        }

        public TopScoreTracker(int size)
        {
            collection = new(SimulationState state, float score)?[size];
            _hobbes = Guid.NewGuid();
        }

        public (SimulationState state, float score)? Insert(SimulationState state, float score)
        {
            lock (_hobbes)
            {
                (SimulationState, float)? oustedValue = (state, score);
                var i = 0;
                for (i = 0; i < collection.Length; i++)
                {
                    if (!collection[i].HasValue || score > collection[i].Value.score) break;
                }

                if (i < collection.Length)
                {
                    oustedValue = collection.Last();
                    for (var j = collection.Length - 2; j > i; j--)
                    {
                        collection[j + 1] = collection[j];
                    }
                    collection[i] = (state, score);
                }

                return oustedValue;
            }
        }
    }
}
