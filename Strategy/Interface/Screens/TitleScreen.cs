using System;
using System.Diagnostics;
using System.Threading;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

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
        }

        public override void Draw()
        {
        }

        protected override void UpdateActive(GameTime gameTime)
        {
            if (_workerThread != null)
            {
                // run a worker thread to update the loading animation
                _workerThread.Start();

                // load the content on the main thread
                _loader.LoadTextures("Images");
                _loader.LoadSounds("Sounds");
                _loader.LoadFonts("Fonts");

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
                _device.Clear(Color.White);
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

        private const int WorkerUpdateTime = 1000 / 30;
    }
}
