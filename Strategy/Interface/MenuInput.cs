using System;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

using Strategy.Library.Extensions;
using Strategy.Library.Input;

namespace Strategy.Interface
{
    public class MenuInput : GameComponent
    {
        public PlayerIndex? Controller
        {
            get { return _controller; }
            set { _input.Controller = _controller = value; }
        }

        public readonly ControlState Action = new ControlState();
        public readonly ControlState Cancel = new ControlState();
        public readonly ControlState Next = new ControlState() { RepeatEnabled = true };
        public readonly ControlState Previous = new ControlState() { RepeatEnabled = true };
        public readonly ControlState Buy = new ControlState();
        public readonly ControlState Invite = new ControlState();
        public readonly ControlState Debug = new ControlState();

        public readonly ControlState[] Activate;
        public readonly ControlState[] Join;
        public readonly ControlState[] ToggleReady;

        public MenuInput(Game game) : base(game)
        {
            // register the primary input methods
            _input = new Input();
            _input.Register(Action, Polling.Any(Polling.One(Buttons.A), Polling.One(Buttons.Start)));
            _input.Register(Cancel, Polling.Any(Polling.One(Buttons.B), Polling.One(Buttons.Back)));
            _input.Register(Next, Polling.Any(Polling.One(Buttons.DPadRight), Polling.One(Buttons.LeftThumbstickRight)));
            _input.Register(Previous, Polling.Any(Polling.One(Buttons.DPadLeft), Polling.One(Buttons.LeftThumbstickLeft)));
            _input.Register(Buy, Polling.One(Buttons.X));
            _input.Register(Invite, Polling.One(Buttons.Y));
            _input.Register(Debug, Polling.All(Polling.One(Buttons.LeftShoulder), Polling.One(Buttons.RightShoulder)));

            // register the lobby inputs
            _inputs = new Input[4];
            Activate = new ControlState[4];
            Join = new ControlState[4];
            ToggleReady = new ControlState[4];
            for (PlayerIndex p = PlayerIndex.One; p <= PlayerIndex.Four; p++)
            {
                int index = (int)p;
                _inputs[index] = new Input();
                _inputs[index].Controller = p;
                Activate[index] = new ControlState();
                _inputs[index].Register(Activate[index], Polling.One(Buttons.Start));
                Join[index] = new ControlState();
                _inputs[index].Register(Join[index], Polling.One(Buttons.A));
                ToggleReady[index] = new ControlState();
                _inputs[index].Register(ToggleReady[index], Polling.One(Buttons.X));
            }
        }

        public override void Update(GameTime gameTime)
        {
            float seconds = gameTime.GetElapsedSeconds();
            _input.Update(seconds);
            foreach (Input input in _inputs)
            {
                input.Update(seconds);
            }
        }

        /// <summary>
        /// Finds and sets the active controller when Start is pressed.
        /// </summary>
        public bool FindAndSetActiveController()
        {
            for (PlayerIndex p = PlayerIndex.One; p <= PlayerIndex.Four; p++)
            {
                if (Activate[(int)p].Pressed)
                {
                    _controller = p;
                    _input.Controller = _controller;
                    _input.Update(0f); // force an update to initialise the state
                    return true;
                }
            }
            return false;
        }

        private Input _input;
        private Input[] _inputs;

        private PlayerIndex? _controller;
    }
}
