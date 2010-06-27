using System;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

using Strategy.Library.Input;

namespace Strategy.Interface
{
    /// <summary>
    /// Polls for input from a local player.
    /// </summary>
    public class LocalInput : Input
    {
        public readonly ControlState Move = new ControlState() { RepeatEnabled = true };
        public readonly ControlPosition MoveDirection = new ControlPosition();

        public readonly ControlState Action = new ControlState();
        public readonly ControlState Cancel = new ControlState();
        public readonly ControlState Place = new ControlState();

        public readonly ControlState Debug = new ControlState();

        public LocalInput(Game game) : base(game)
        {
            Register(Move, (state) => state.ThumbSticks.Left.LengthSquared() >= MoveTolerance);
            Register(MoveDirection, Polling.LeftThumbStick);
            Register(Action, Polling.One(Buttons.A));
            Register(Cancel, Polling.One(Buttons.B));
            Register(Place, Polling.One(Buttons.X));
            Register(Debug, Polling.All(Polling.One(Buttons.LeftShoulder), Polling.One(Buttons.RightShoulder)));
        }

        /// <summary>
        /// Polls for the controller with the Start button pressed.
        /// </summary>
        /// <returns>True if a controller was found; otherwise, false.</returns>
        public bool FindActiveController()
        {
            return FindActiveController(Polling.One(Buttons.Start));
        }

        private const float MoveTolerance = 0.5f * 0.5f;
    }
}
