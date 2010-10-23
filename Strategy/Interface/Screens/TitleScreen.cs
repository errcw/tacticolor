using System;
using System.Diagnostics;
using System.Threading;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.GamerServices;
using Microsoft.Xna.Framework.Graphics;

using Strategy.Properties;
using Strategy.Library;
using Strategy.Library.Extensions;
using Strategy.Library.Screen;
using Strategy.Library.Sprite;
using Strategy.Library.Storage;

namespace Strategy.Interface.Screens
{
    /// <summary>
    /// Displays the title.
    /// </summary>
    public class TitleScreen : Screen
    {
        /// <summary>
        /// Occurs when the game has finished preloading its content.
        /// </summary>
        public event EventHandler<EventArgs> ContentLoaded;

        public TitleScreen(StrategyGame game)
        {
            _loader = new ContentPreloader(game.Content);

            _workerThread = new Thread(UpdateDrawWorker);
            _workerExit = new ManualResetEvent(false);
            _device = game.GraphicsDevice;

            _input = game.Services.GetService<MenuInput>();
            _storage = game.Services.GetService<Storage>();

            // load the initial content for the loading screen
            _background = new ImageSprite(game.Content.Load<Texture2D>("Images/BackgroundUI"));

            _title = new ImageSprite(game.Content.Load<Texture2D>("Images/Title"));
            _title.Position = new Vector2((1280 - _title.Size.X) / 2, (720 - _title.Size.Y) / 2);

            SpriteFont font = game.Content.Load<SpriteFont>("Fonts/TextLarge");
            Vector2 loadingTextSize = font.MeasureString(Resources.Loading);
            _loadingText = new TextSprite(font, Resources.Loading);
            _loadingText.Position = new Vector2((1280 - loadingTextSize.X) / 2, 720 - 200);
            _loadingText.Color = Color.White;
            _loadingText.OutlineColor = Color.Black;
            _loadingText.OutlineWidth = 2;

            Vector2 readyTextSize = font.MeasureString(Resources.PressStart);
            _readyText = new TextSprite(font, Resources.PressStart);
            _readyText.Position = new Vector2((1280 - readyTextSize.X) / 2, 720 - 200);
            _readyText.Color = Color.Black;

            _loadingAnimation = new SequentialAnimation(
                new ColorAnimation(_loadingText, Color.Transparent, 2f, Interpolation.InterpolateColor(Easing.QuadraticIn)),
                new ColorAnimation(_loadingText, Color.White, 2f, Interpolation.InterpolateColor(Easing.QuadraticOut)));

            _spriteBatch = new SpriteBatch(_device);

            TransitionOnTime = 0f;
            TransitionOffTime = 1f;
        }

        public override void Draw()
        {
            _spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend);
            _background.Draw(_spriteBatch);
            _title.Draw(_spriteBatch);
            _readyText.Draw(_spriteBatch);
            _spriteBatch.End();
        }

        protected internal override void Show(bool pushed)
        {
            // unset the controller value so a new controller may take control
            _input.Controller = null;

            base.Show(pushed);
        }

        protected internal override void Hide(bool popped)
        {
            // future returns to the screen need animation
            TransitionOnTime = 0.5f;

            base.Hide(popped);
        }

        protected override void UpdateTransitionOn(GameTime gameTime, float progress, bool pushed)
        {
            _title.Color = ColorExtensions.FromNonPremultiplied(_readyText.Color, 0.8f * progress + 0.2f);
            _readyText.Color = ColorExtensions.FromNonPremultiplied(_readyText.Color, progress);
        }

        protected override void UpdateTransitionOff(GameTime gameTime, float progress, bool popped)
        {
            _title.Color = ColorExtensions.FromNonPremultiplied(_readyText.Color, 0.8f * (1 - progress) + 0.2f);
            _readyText.Color = ColorExtensions.FromNonPremultiplied(_readyText.Color, 1 - progress);
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

                if (ContentLoaded != null)
                {
                    ContentLoaded(this, EventArgs.Empty);
                }
            }
            else
            {
                // loading has finished, wait for the player to start the game
                if (_input.FindAndSetActiveController())
                {
                    if (!_input.Controller.Value.IsSignedIn())
                    {
                        Guide.ShowSignIn(1, false);
                    }
                }
                // require a signed in profile to continue
                if (_input.Controller.HasValue && _input.Controller.Value.IsSignedIn())
                {
                    // prompt for storage before continuing
                    _storage.PromptForDevice();

                    MainMenuScreen menuScreen = new MainMenuScreen((StrategyGame)Stack.Game);
                    Stack.Push(menuScreen);
                }
            }
        }

        /// <summary>
        /// Manages the update/draw loop while content is loading.
        /// </summary>
        private void UpdateDrawWorker()
        {
#if XBOX
            // on the xbox set the processor affinity to move the update
            // code off the core where the loading is happening
            _workerThread.SetProcessorAffinity(3);
#endif

            _lastWorkerUpdateTime = Stopwatch.GetTimestamp();

            while (!_workerExit.WaitOne(WorkerUpdateTime, false))
            {
                long currentUpdateTime = Stopwatch.GetTimestamp();
                float elapsed = (currentUpdateTime - _lastWorkerUpdateTime) / (float)Stopwatch.Frequency;

                UpdateDrawLoading(elapsed);

                _lastWorkerUpdateTime = currentUpdateTime;
            }
        }

        private void UpdateDrawLoading(float time)
        {
            if (_device == null || _device.IsDisposed)
            {
                return;
            }

            try
            {
                if (!_loadingAnimation.Update(time))
                {
                    _loadingAnimation.Start();
                }

                _device.Clear(Color.White);

                _spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend);
                _background.Draw(_spriteBatch);
                _title.Draw(_spriteBatch);
                _loadingText.Draw(_spriteBatch);
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

        private ImageSprite _background;
        private ImageSprite _title;
        private TextSprite _loadingText;
        private TextSprite _readyText;
        private IAnimation _loadingAnimation;
        private SpriteBatch _spriteBatch;

        private MenuInput _input;
        private Storage _storage;

        private const int WorkerUpdateTime = 1000 / 30;
    }
}
