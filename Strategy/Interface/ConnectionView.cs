using System;
using System.Collections.Generic;
using System.Linq;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using Strategy.Gameplay;
using Strategy.Library;
using Strategy.Library.Sprite;

namespace Strategy.Interface
{
    /// <summary>
    /// Shows a connection between two territories.
    /// </summary>
    public class ConnectionView
    {
        public ConnectionView(Territory a, Territory b, InterfaceContext context)
        {
            // find the closest points to connect
            Cell closestA = a.Area.First(), closestB = b.Area.First();
            int closestDist2 = int.MaxValue;
            foreach (Cell ca in a.Area)
            {
                foreach (Cell cb in b.Area)
                {
                    int d2 = (ca.Row - cb.Row) * (ca.Row - cb.Row) + (ca.Col - cb.Col) * (ca.Col - cb.Col);
                    if (d2 < closestDist2 && (ca.Row == cb.Row || ca.Col == cb.Col))
                    {
                        closestA = ca;
                        closestB = cb;
                        closestDist2 = d2;
                    }
                }
            }

            // create the connection sprites
            bool sameRow = (closestA.Row == closestB.Row);
            Texture2D connectionTex = context.Content.Load<Texture2D>(sameRow ? "ConnectionRow" : "ConnectionCol");
            _sprites = new List<Sprite>(2);
            foreach (Point p in BresenhamIterator.GetPointsOnLine(closestA.Row, closestA.Col, closestB.Row, closestB.Col))
            {
                Sprite sprite = new ImageSprite(connectionTex);
                sprite.X = context.IsoParams.GetX(p.X, p.Y);
                sprite.Y = context.IsoParams.GetY(p.X, p.Y);
                sprite.Color = new Color(200, 200, 200);
                _sprites.Add(sprite);
            }
        }

        public void Update(float time)
        {
        }

        public void Draw(IsometricBatch isoBatch)
        {
            foreach (Sprite sprite in _sprites)
            {
                isoBatch.Draw(sprite);
            }
        }

        private ICollection<Sprite> _sprites;
    }
}
