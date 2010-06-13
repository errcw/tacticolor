using System;
using System.Collections.Generic;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Strategy.Interface
{
    public class IsometricBatch
    {
        public IsometricBatch(SpriteBatch batch)
        {
            _batch = batch;
            _sprites = new List<IsometricSprite>(32);
        }

        public void Begin()
        {
            _sprites.Clear();
        }

        public void End()
        {
            _sprites.Sort(RenderOrderComparison);

            _batch.Begin(SpriteBlendMode.AlphaBlend, SpriteSortMode.Deferred, SaveStateMode.None);
            foreach (IsometricSprite sprite in _sprites)
            {
                sprite.Draw(_batch);
            }
            _batch.End();
        }

        public void Draw(IsometricSprite sprite)
        {
            _sprites.Add(sprite);
        }

        private int RenderOrderComparison(IsometricSprite a, IsometricSprite b)
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

        private List<IsometricSprite> _sprites;
        private SpriteBatch _batch;
    }
}
