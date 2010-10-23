using System;
using System.Collections.Generic;
using System.Linq;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

using Strategy.Library.Extensions;

namespace Strategy.Library.Input
{
    /// <summary>
    /// Polls for input from one controller.
    /// </summary>
    public class Input
    {
        /// <summary>
        /// The currently active controller (i.e., the one being polled).
        /// </summary>
        public PlayerIndex? Controller { get; set; }

        /// <summary>
        /// An event triggered when the active controller is disconnected.
        /// </summary>
        public event EventHandler<EventArgs> ControllerDisconnected;

        /// <summary>
        /// If vibration is enabled for the active controller.
        /// </summary>
        public bool VibrationEnabled { get; set; }

        /// <summary>
        /// Creates a new input poller.
        /// </summary>
        public Input()
        {
            Controller = null;
        }

        /// <summary>
        /// Polls the current input state.
        /// </summary>
        /// <param name="time">The elapsed time, in seconds, since the last update.</param>
        public void Update(float time)
        {
            PollControllerConnectivity();
            if (Controller.HasValue)
            {
                UpdateControls(time);
                UpdateVibration(time);
            }
        }

        /// <summary>
        /// Polls all the controllers looking for the specified predicate to be true. The
        /// first controller matching the predicate is assigned to Controller.
        /// </summary>
        /// <param name="poll">The predicate to test.</param>
        /// <returns>True if a controller was selected; otherwise, false.</returns>
        public bool FindActiveController(PollIsDown poll)
        {
            for (PlayerIndex p = PlayerIndex.One; p <= PlayerIndex.Four; p++)
            {
                GamePadState state = PollState(p);
                if (poll(state))
                {
                    Controller = p;
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Adds vibration on the active controller. Multiple calls are additive.
        /// </summary>
        /// <param name="vibration">The vibration function.</param>
        public void AddVibration(Vibration vibration)
        {
            _vibration.Add(vibration);
        }

        /// <summary>
        /// Stops the vibration on the active controller.
        /// </summary>
        public void StopVibration()
        {
            _vibration.Clear();
            GamePad.SetVibration(Controller.Value, 0f, 0f);
        }

        /// <summary>
        /// Registers a control state to be updated every frame.
        /// </summary>
        /// <param name="control">The control state to update.</param>
        /// <param name="pollFn">The polling function to update the control with.</param>
        public void Register(ControlState control, PollIsDown pollFn)
        {
            _stateCtrls.Add(control, pollFn);
        }

        /// <summary>
        /// Registers a control position to be updated every frame.
        /// </summary>
        /// <param name="control">The control position to update.</param>
        /// <param name="pollFn">The polling function to update the control with.</param>
        public void Register(ControlPosition control, PollPosition pollFn)
        {
            _positionCtrls.Add(control, pollFn);
        }

        /// <summary>
        /// Updates the state of the registered controls.
        /// </summary>
        /// <param name="time">The elapsed time, in seconds, since the last update.</param>
        private void UpdateControls(float time)
        {
            GamePadState state = PollState(Controller.Value);
            foreach (var entry in _stateCtrls)
            {
                entry.Key.Update(time, entry.Value(state));
            }
            foreach (var entry in _positionCtrls)
            {
                entry.Key.Update(time, entry.Value(state));
            }
        }

        /// <summary>
        /// Updates the controller vibration.
        /// </summary>
        /// <param name="time">The elapsed time, in seconds, since the last update.</param>
        private void UpdateVibration(float time)
        {
            Vector2 vibration = Vector2.Zero;
            _vibration = _vibration.Where(delegate(Vibration v)
            {
                Vector2? amount = v(time);
                if (amount != null)
                {
                    vibration += amount.Value;
                    return false;
                }
                else
                {
                    return true;
                }
            }).ToList();
            if (VibrationEnabled)
            {
                GamePad.SetVibration(Controller.Value, vibration.X, vibration.Y);
            }
        }

        /// <summary>
        /// Monitors the connected state of the active controller.
        /// </summary>
        [System.Diagnostics.Conditional("XBOX")]
        private void PollControllerConnectivity()
        {
            if (Controller.HasValue)
            {
                // check if the controller is disconnected
                GamePadState padState = GamePad.GetState(Controller.Value);
                if (!padState.IsConnected)
                {
                    _prevController = Controller.Value;
                    Controller = null;
                    if (ControllerDisconnected != null)
                    {
                        ControllerDisconnected(this, EventArgs.Empty);
                    }
                }
            }
            else if (_prevController.HasValue)
            {
                // check if the controller is reconnected
                GamePadState padState = GamePad.GetState(_prevController.Value);
                if (padState.IsConnected)
                {
                    Controller = _prevController.Value;
                }
            }
        }

        /// <summary>
        /// Polls a controller for its current state.
        /// </summary>
        /// <param name="playerIdx">The player index to poll.</param>
        private GamePadState PollState(PlayerIndex playerIdx)
        {
            GamePadState state = GamePad.GetState(playerIdx);

#if WINDOWS // on windows fabricate the state when there is no controller
            if (!state.IsConnected)
            {
                KeyboardState kbd = Keyboard.GetState();
                Buttons buttons = 0;
                Vector2 leftStick = Vector2.Zero;
                if (kbd.IsKeyDown(Keys.Up)) leftStick.Y = 1;
                else if (kbd.IsKeyDown(Keys.Down)) leftStick.Y = -1;
                if (kbd.IsKeyDown(Keys.Left)) leftStick.X = -1;
                else if (kbd.IsKeyDown(Keys.Right)) leftStick.X = 1;
                if (kbd.IsKeyDown(Keys.NumPad8)) buttons |= Buttons.DPadUp;
                if (kbd.IsKeyDown(Keys.NumPad5)) buttons |= Buttons.DPadDown;
                if (kbd.IsKeyDown(Keys.NumPad4)) buttons |= Buttons.DPadLeft;
                if (kbd.IsKeyDown(Keys.NumPad6)) buttons |= Buttons.DPadRight;
                if (kbd.IsKeyDown(Keys.A)) buttons |= Buttons.A;
                if (kbd.IsKeyDown(Keys.B)) buttons |= Buttons.B;
                if (kbd.IsKeyDown(Keys.X)) buttons |= Buttons.X;
                if (kbd.IsKeyDown(Keys.Y)) buttons |= Buttons.Y;
                if (kbd.IsKeyDown(Keys.Enter)) buttons |= Buttons.Start;
                if (kbd.IsKeyDown(Keys.RightShift)) buttons |= Buttons.Back;
                state = new GamePadState(leftStick, Vector2.Zero, 0f, 0f, buttons);
            }
#endif

            return state;
        }

        private List<Vibration> _vibration = new List<Vibration>();

        private Dictionary<ControlState, PollIsDown> _stateCtrls = new Dictionary<ControlState, PollIsDown>();
        private Dictionary<ControlPosition, PollPosition> _positionCtrls = new Dictionary<ControlPosition, PollPosition>();

        private PlayerIndex? _prevController;
    }
}
