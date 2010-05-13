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
            if (a.Y < b.Y)
            {
                return -1;
            }
            else if (a.Y > b.Y)
            {
                return 1;
            }
            else
            {
                if (a.X < b.X)
                {
                    return -1;
                }
                else if (a.X > b.X)
                {
                    return 1;
                }
                else
                {
                    return a.GetHashCode() < b.GetHashCode() ? 1 : -1; // consistent ordering
                }
            }
        }

        private List<IsometricSprite> _sprites;
        private SpriteBatch _batch;
    }
}
