using System;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using Strategy.Library.Screen;
using Strategy.Library.Sprite;

namespace Strategy.Interface.Screens
{
    /// <summary>
    /// Shows a busy indicator while an asynchronous operation completes.
    /// </summary>
    public class AsyncBusyScreen : Screen
    {
        public event EventHandler<AsyncOperationCompletedEventArgs> OperationCompleted;

        public AsyncBusyScreen(Game game, IAsyncResult result)
        {
            _result = result;

            _background = new ImageSprite(game.Content.Load<Texture2D>("Images/Colourable"));
            _background.Scale = new Vector2(1280, 720);
            _background.Color = new Color(255, 255, 255, 128);
            _background.Position = Vector2.Zero;

            _marker = new ImageSprite(game.Content.Load<Texture2D>("Images/Piece"));
            _marker.Position = new Vector2(640, 360);

            _spriteBatch = new SpriteBatch(game.GraphicsDevice);

            TransitionOnTime = 0f;
            TransitionOffTime = 0f;
            ShowBeneath = true;
        }

        public override void Draw()
        {
            _spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend);
            _background.Draw(_spriteBatch);
            _marker.Draw(_spriteBatch);
            _spriteBatch.End();
        }

        protected override void UpdateActive(GameTime gameTime)
        {
            if (_result != null && _result.IsCompleted)
            {
                // the callback might want to push new screens so pop first
                Stack.Pop();

                if (OperationCompleted != null)
                {
                    OperationCompleted(this, new AsyncOperationCompletedEventArgs(_result));
                }

                _result = null;
            }
        }

        private IAsyncResult _result;

        private ImageSprite _background;
        private ImageSprite _marker;
        private SpriteBatch _spriteBatch;
    }

    public class AsyncOperationCompletedEventArgs : EventArgs
    {
        public IAsyncResult AsyncResult { get; private set; }
        public AsyncOperationCompletedEventArgs(IAsyncResult result)
        {
            AsyncResult = result;
        }
    }
}
