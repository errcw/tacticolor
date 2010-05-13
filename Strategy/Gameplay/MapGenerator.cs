using System;
using System.Collections.Generic;

using Microsoft.Xna.Framework;

namespace Strategy.Gameplay
{
    /// <summary>
    /// Generates random maps.
    /// </summary>
    public class MapGenerator
    {
        /// <summary>
        /// Creates a new map generator.
        /// </summary>
        public MapGenerator() : this(new Random())
        {
        }

        /// <summary>
        /// Creates a new map generator.
        /// </summary>
        /// <param name="random">The random number generator to use.</param>
        public MapGenerator(Random random)
        {
            _random = random;
        }

        /// <summary>
        /// Generates a new map.
        /// </summary>
        /// <param name="numTerritories">The number of territories on the map.</param>
        /// <param name="territoriesPerPlayer">The number of territories per player.</param>
        /// <returns>The generated map.</returns>
        public Map Generate(int numTerritories, int territoriesPerPlayer)
        {
            Territory[] territories = new Territory[numTerritories];

            int rows = (int)Math.Floor(Math.Sqrt(numTerritories));
            int cols = (int)Math.Ceiling(Math.Sqrt(numTerritories));
            System.Diagnostics.Debug.Assert(rows * cols == numTerritories);

            // generate the territory locations
            for (int r = 0; r < rows; r++)
            {
                for (int c = 0; c < cols; c++)
                {
                    int xoffset = 0;
                    if (r % 2 != 0)
                    {
                        xoffset = GRID_SIZE / 2;
                    }

                    int x = c * GRID_SIZE + xoffset + _random.Next(-OFFSET_THRESHOLD, OFFSET_THRESHOLD);
                    int y = r * GRID_SIZE + _random.Next(-OFFSET_THRESHOLD, OFFSET_THRESHOLD);

                    territories[r * rows + c] = new Territory(x, y);
                }
            }

            // assign owners to the territories
            for (PlayerId p = PlayerId.A; p <= PlayerId.D; p++)
            {
                for (int i = 0; i < territoriesPerPlayer; i++)
                {
                    while (true)
                    {
                        int t = _random.Next(numTerritories);
                        if (territories[t].Owner == null)
                        {
                            territories[t].Owner = p;
                            break;
                        }
                    }
                }
            }

            // connect the territories
            LinkedList<LineSegment> segments = new LinkedList<LineSegment>();
            foreach (Territory ta in territories)
            {
                // find all the candidate territories
                List<Territory> neighbors = new List<Territory>(numTerritories);
                foreach (Territory tb in territories)
                {
                    if (ta != tb &&
                        !tb.Adjacent.Contains(ta) &&
                        !WouldIntersect(segments, new LineSegment(ta.Position, tb.Position)) &&
                        Vector2.Distance(ta.Position, tb.Position) < CONNECTION_THRESHOLD)
                    {
                        neighbors.Add(tb);
                    }
                }
                // add the minimum number of connections
                while (ta.Adjacent.Count < CONNECTION_MINIMUM)
                {
                    for (int i = 0; i < neighbors.Count; i++)
                    {
                        Territory tb = neighbors[i];
                        if (tb == null)
                        {
                            continue;
                        }
                        if (_random.NextDouble() < CONNECTION_CHANCE)
                        {
                            ta.Adjacent.Add(tb);
                            tb.Adjacent.Add(ta);
                            segments.AddLast(new LineSegment(ta.Position, tb.Position));
                            neighbors[i] = null;
                        }
                    }
                }
            }

            Map map = new Map(territories);
            return map;
        }

        /// <summary>
        /// Determines if a given line segment intersects any other in a set of segments.
        /// </summary>
        private bool WouldIntersect(IEnumerable<LineSegment> segments, LineSegment segment)
        {
            foreach (LineSegment s in segments)
            {
                // segment: x1y1 to x2y2
                // s: x3y3 to x4y4
                float y21 = segment.End.Y - segment.Start.Y;
                float y43 = s.End.Y - s.Start.Y;
                float x21 = segment.End.X - segment.Start.X;
                float x43 = s.End.X - s.Start.X;

                float denom = y43 * x21 - x43 * y21;
                if (denom == 0)
                {
                    continue; // parallel or coincident
                }

                float y13 = segment.Start.Y - s.Start.Y;
                float x13 = segment.Start.X - s.Start.X;

                float tseg = (x43 * y13 - y43 * x13) / denom;
                float ts = (x21 * y13 - y21 * x13) / denom;

                if (tseg > 0 && tseg < 1 && ts > 0 && ts < 1)
                {
                    return true; // interior intersection
                }
            }
            return false;
        }

        /// <summary>
        /// A line segment.
        /// </summary>
        private struct LineSegment
        {
            public Vector2 Start;
            public Vector2 End;

            public LineSegment(Vector2 start, Vector2 end)
            {
                Start = start;
                End = end;
            }
        }

        private Random _random;

        private const int GRID_SIZE = 150;
        private const int OFFSET_THRESHOLD = 25;
        private const int CONNECTION_MINIMUM = 2;
        private const double CONNECTION_CHANCE = 0.75;
        private const int CONNECTION_THRESHOLD = 250;
    }
}
