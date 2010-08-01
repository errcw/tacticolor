using System;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using Strategy.Library.Screen;

namespace Strategy.Interface.Screens
{
    /// <summary>
    /// Shows a busy indicator while an asynchronous operation completes.
    /// </summary>
    public class AsyncBusyScreen : Screen
    {
        public event EventHandler<AsyncOperationCompletedEventArgs> OperationCompleted;

        public AsyncBusyScreen(IAsyncResult result)
        {
            _result = result;

            TransitionOnTime = 0f;
            TransitionOffTime = 0f;
            ShowBeneath = true;
        }

        public override void Draw()
        {
            if (_image != null)
            {
                _batch.Begin(SpriteBlendMode.AlphaBlend, SpriteSortMode.Immediate, SaveStateMode.None);
                _batch.Draw(_image, Position, Color.White);
                _batch.End();
            }
        }

        protected override void UpdateTransitionOn(GameTime gameTime, float progress, bool pushed)
        {
            if (_image == null)
            {
                _image = Stack.Game.Content.Load<Texture2D>("Images/PieceAvailable");
            }
        }

        protected override void UpdateActive(GameTime gameTime)
        {
            if (_result != null && _result.IsCompleted)
            {
                if (OperationCompleted != null)
                {
                    OperationCompleted(this, new AsyncOperationCompletedEventArgs(_result));
                }
                Stack.Pop();
                _result = null;
            }
        }

        private IAsyncResult _result;

        private SpriteBatch _batch;
        private Texture2D _image;

        private readonly Vector2 Position = new Vector2(100, 100);
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
