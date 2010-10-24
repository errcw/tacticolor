using System;
using System.Collections.Generic;
using System.Linq;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using Strategy.Gameplay;
using Strategy.Library;
using Strategy.Library.Extensions;
using Strategy.Library.Sprite;

namespace Strategy.Interface.Gameplay
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
            Texture2D connectionTex = context.Content.Load<Texture2D>(sameRow ? "Images/ConnectionRow" : "Images/ConnectionCol");
            _sprites = new List<Sprite>(4);
            foreach (Point p in BresenhamIterator.GetPointsOnLine(closestA.Row, closestA.Col, closestB.Row, closestB.Col))
            {
                Sprite sprite = new ImageSprite(connectionTex);
                sprite.X = context.IsoParams.GetX(p.X, p.Y);
                sprite.Y = context.IsoParams.GetY(p.X, p.Y);
                sprite.Color = new Color(200, 200, 200);
                sprite.Layer = 0.9f;
                _sprites.Add(sprite);
            }
        }

        public void Update(float time)
        {
            // conntections are currently static
        }

        public void Draw(IsometricView isoView)
        {
            _sprites.ForEach(sprite => isoView.Add(sprite));
        }

        private List<Sprite> _sprites;
    }
}
