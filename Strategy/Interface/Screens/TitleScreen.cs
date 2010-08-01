using System;
using System.Diagnostics;
using System.Threading;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;

using Strategy.Properties;
using Strategy.Library;
using Strategy.Library.Screen;

namespace Strategy.Interface.Screens
{
    /// <summary>
    /// Displays the title.
    /// </summary>
    public class TitleScreen : Screen
    {
        public TitleScreen(StrategyGame game)
        {
            _loader = new ContentPreloader(game.Content);

            _workerThread = new Thread(UpdateDrawWorker);
            _workerExit = new ManualResetEvent(false);
            _device = game.GraphicsDevice;

            // load the initial content for the loading screen
            _background = game.Content.Load<Texture2D>("Images/Background");
            _title = game.Content.Load<Texture2D>("Images/Title");
            _loadingFont = game.Content.Load<SpriteFont>("Fonts/TextSmall");
            _spriteBatch = new SpriteBatch(_device);
        }

        public override void Draw()
        {
            _spriteBatch.Begin(SpriteBlendMode.AlphaBlend, SpriteSortMode.Immediate, SaveStateMode.None);
            _spriteBatch.Draw(_background, Vector2.Zero, Color.White);
            _spriteBatch.Draw(_title, Vector2.Zero, Color.White);
            _spriteBatch.End();
        }

        protected override void UpdateActive(GameTime gameTime)
        {
            if (_workerThread != null)
            {
                // run a worker thread to update the loading animation
                _workerThread.Start();

                // load the content on the main thread
                _loader.Load<Texture2D>("Images");
                _loader.Load<SoundEffect>("Sounds");
                _loader.Load<SpriteFont>("Fonts");

                _workerExit.Set();
                _workerThread.Join();
                _workerThread = null;
            }
            else
            {
                // loading has finished, wait for the player to start the game
            }
        }

        /// <summary>
        /// Manages the update/draw loop while content is loading.
        /// </summary>
        private void UpdateDrawWorker()
        {
            // on the xbox set the processor affinity to move the update
            // code off the core where the loading is happening
#if XBOX
            try
            {
                _workerThread.SetProcessorAffinity(3);
            }
            catch (Exception e)
            {
                Debug.WriteLine(e);
            }
#endif

            _lastWorkerUpdateTime = Stopwatch.GetTimestamp();

            while (_workerExit.WaitOne(WorkerUpdateTime, false))
            {
                long currentUpdateTime = Stopwatch.GetTimestamp();
                long elapsed = currentUpdateTime - _lastWorkerUpdateTime;

                UpdateDrawLoading(elapsed);

                _lastWorkerUpdateTime = currentUpdateTime;
            }
        }

        /// <summary>
        /// Updates and draws the loading animation.
        /// </summary>
        /// <param name="time">The elapsed time, in milliseconds, since the last update.</param>
        private void UpdateDrawLoading(long time)
        {
            if (_device == null || _device.IsDisposed)
            {
                return;
            }

            try
            {
                _device.Clear(new Color(45, 45, 45));

                _spriteBatch.Begin(SpriteBlendMode.AlphaBlend, SpriteSortMode.Immediate, SaveStateMode.None);
                _spriteBatch.Draw(_background, Vector2.Zero, Color.White);
                _spriteBatch.Draw(_title, Vector2.Zero, Color.White);
                _spriteBatch.DrawString(_loadingFont, Resources.Loading, Vector2.Zero, Color.DarkGray);
                _spriteBatch.End();

                _device.Present();
            }
            catch
            {
                // no way to recover so avoid updating
                _device = null;
            }
        }

        private ContentPreloader _loader;

        private Thread _workerThread;
        private long _lastWorkerUpdateTime;
        private EventWaitHandle _workerExit;
        private GraphicsDevice _device;

        private Texture2D _background;
        private Texture2D _title;
        private SpriteFont _loadingFont;
        private SpriteBatch _spriteBatch;

        private const int WorkerUpdateTime = 1000 / 30;
    }
}
