using System;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using Strategy.Gameplay;
using Strategy.Library.Extensions;
using Strategy.Library.Screen;

namespace Strategy.Interface.Screens
{
    /// <summary>
    /// Shows a busy indicator while an asynchronous operation completes.
    /// </summary>
    public class MatchOverScreen : Screen
    {
        public MatchOverScreen(StrategyGame game, PlayerId winner)
        {
            _input = game.Services.GetService<MenuInput>();

            TransitionOnTime = 0f;
            TransitionOffTime = 0f;
            ShowBeneath = true;
        }

        public override void Draw()
        {
            _batch.Begin(SpriteBlendMode.AlphaBlend, SpriteSortMode.Immediate, SaveStateMode.None);
            _batch.End();
        }

        protected override void UpdateActive(GameTime gameTime)
        {
            if (_input.Action.Released)
            {
                while (!(Stack.ActiveScreen is MainMenuScreen))
                {
                    Stack.Pop();
                }
            }
        }

        private MenuInput _input;
        private SpriteBatch _batch;
    }
}
