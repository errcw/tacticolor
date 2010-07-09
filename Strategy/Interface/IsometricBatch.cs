using System;
using System.Collections.Generic;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using Strategy.Library.Sprite;

namespace Strategy.Interface
{
    public class IsometricBatch
    {
        public IsometricBatch(SpriteBatch batch)
        {
            _batch = batch;
            _sprites = new List<Sprite>(32);
        }

        public void Begin()
        {
            _sprites.Clear();
        }

        public void End()
        {
            _sprites.Sort(RenderOrderComparison);

            _batch.Begin(SpriteBlendMode.AlphaBlend, SpriteSortMode.Deferred, SaveStateMode.None);
            foreach (Sprite sprite in _sprites)
            {
                sprite.Draw(_batch);
            }
            _batch.End();
        }

        public void Draw(Sprite sprite)
        {
            _sprites.Add(sprite);
        }

        private int RenderOrderComparison(Sprite a, Sprite b)
        {
            if (a == b)
            {
                return 0;
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
        private SpriteBatch _batch;
    }
}
