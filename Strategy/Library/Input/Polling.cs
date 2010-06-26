using System;
using System.Linq;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace Strategy.Library.Input
{
    /// <summary>
    /// A predicate to check if a gamepad control is down.
    /// </summary>
    /// <param name="state">The gamepad state to test.</param>
    /// <returns>True if the control is down; otherwise, false.</returns>
    public delegate bool PollIsDown(GamePadState state);

    /// <summary>
    /// A function to poll the position of a gamepad control.
    /// </summary>
    /// <param name="state">The gamepad state to test.</param>
    /// <returns>The position of the polled control.</returns>
    /// <typeparam name="T">The type of position returned.</typeparam>
    public delegate Vector2 PollPosition(GamePadState state);

    /// <summary>
    /// A set of gamepad polling functions.
    /// </summary>
    public static class Polling
    {
        /// <summary>
        /// Polls the position of a controller's left thumb stick.
        /// </summary>
        public static readonly PollPosition LeftThumbStick = (s => s.ThumbSticks.Left);

        /// <summary>
        /// Polls the position of a controller's right thumb stick.
        /// </summary>
        public static readonly PollPosition RightThumbStick = (s => s.ThumbSticks.Right);

        /// <summary>
        /// Polls the position of a controller's triggers (left in X, right in Y).
        /// </summary>
        public static readonly PollPosition Triggers = (s => new Vector2(s.Triggers.Left, s.Triggers.Right));

        /// <summary>
        /// Creates a predicate to check if one or more buttons is down.
        /// </summary>
        /// <param name="buttons">The button(s) to test.</param>
        /// <returns>A predicate.</returns>
        public static PollIsDown One(Buttons buttons)
        {
            return (state => state.IsButtonDown(buttons));
        }

        /// <summary>
        /// Creates a predicate to check if any of the specified predicates return true.
        /// </summary>
        /// <param name="pollers">The predicates to test.</param>
        /// <returns>A composite predicate.</returns>
        public static PollIsDown Any(params PollIsDown[] pollers)
        {
            return (state => pollers.Any(p => p(state)));
        }

        /// <summary>
        /// Creates a predicate to check if all of the specified predicates return true.
        /// </summary>
        /// <param name="pollers">The predicates to test.</param>
        /// <returns>A composite predicate.</returns>
        public static PollIsDown All(params PollIsDown[] pollers)
        {
            return (state => pollers.All(p => p(state)));
        }
    }
}
