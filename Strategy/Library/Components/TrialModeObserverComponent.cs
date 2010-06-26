using System;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.GamerServices;

namespace Strategy.Library.Components
{
    /// <summary>
    /// Watches for changes to Guide.IsTrialMode and reports when the trial mode ends.
    /// </summary>
    /// <remarks>Disables itself after detecting the event.</remarks>
    public class TrialModeObserverComponent : GameComponent
    {
        /// <summary>
        /// Fired when the trial mode ends.
        /// </summary>
        public event EventHandler<EventArgs> TrialModeEnded;

        public TrialModeObserverComponent(Game game) : base(game)
        {
            _wasTrialMode = Guide.IsTrialMode;
        }

        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);
            bool isTrialMode = Guide.IsTrialMode;
            if (_wasTrialMode && !isTrialMode)
            {
                if (TrialModeEnded != null)
                {
                    TrialModeEnded(this, EventArgs.Empty);
                }
                Enabled = false;
            }
            _wasTrialMode = isTrialMode;
        }

        private bool _wasTrialMode;
    }
}
