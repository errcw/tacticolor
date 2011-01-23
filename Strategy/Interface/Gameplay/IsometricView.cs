using System;
using System.Collections.Generic;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using Strategy.Library.Sprite;

namespace Strategy.Interface.Gameplay
{
    /// <summary>
    /// Draws sprites in isometric space with the correct ordering.
    /// </summary>
    public class IsometricView
    {
        public IsometricView()
        {
            _sprites = new List<Sprite>();
        }

        public void Add(Sprite sprite)
        {
            _sprites.Add(sprite);
        }

        public void Clear()
        {
            _sprites.Clear();
        }

        public IEnumerable<Sprite> GetSpritesInDrawOrder()
        {
            _sprites.Sort(RenderOrderComparison);
            return _sprites;
        }

        public void Draw(SpriteBatch spriteBatch)
        {
            _sprites.Sort(RenderOrderComparison);
            foreach (Sprite sprite in _sprites)
            {
                sprite.Draw(spriteBatch);
            }
        }

        private int RenderOrderComparison(Sprite a, Sprite b)
        {
            if (a == b)
            {
                return 0;
            }
            if (a.Layer != b.Layer)
            {
                return b.Layer > a.Layer ? 1 : -1;
            }
            Vector2 apos = a.Position + a.Origin;
            Vector2 bpos = b.Position + b.Origin;
            if (apos.Y < bpos.Y)
            {
                return -1;
            }
            else if (apos.Y > bpos.Y)
            {
                return 1;
            }
            else
            {
                if (apos.X < bpos.X)
                {
                    return -1;
                }
                else if (apos.X > bpos.X)
                {
                    return 1;
                }
                else
                {
                    return apos.GetHashCode() < bpos.GetHashCode() ? 1 : -1; // consistent ordering
                }
            }
        }

        private List<Sprite> _sprites;
    }
}
