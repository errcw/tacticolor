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
            get { return _input.Controller; }
            set { _input.Controller = value; }
        }

        public readonly ControlState Action = new ControlState();
        public readonly ControlState Cancel = new ControlState();
        public readonly ControlState Up = new ControlState();
        public readonly ControlState Down = new ControlState();
        public readonly ControlState Buy = new ControlState();

        public readonly ControlState[] Join;
        public readonly ControlState[] Leave;

        public MenuInput(Game game) : base(game)
        {
            // register the primary input methods
            _input = new Input();
            _input.Register(Action, Polling.Any(Polling.One(Buttons.A), Polling.One(Buttons.Start)));
            _input.Register(Cancel, Polling.Any(Polling.One(Buttons.B), Polling.One(Buttons.Back)));
            _input.Register(Up, Polling.Any(Polling.One(Buttons.DPadUp), Polling.One(Buttons.LeftThumbstickUp)));
            _input.Register(Down, Polling.Any(Polling.One(Buttons.DPadDown), Polling.One(Buttons.LeftThumbstickDown)));
            _input.Register(Buy, Polling.One(Buttons.X));

            // register the lobby inputs
            _inputs = new Input[4];
            Join = new ControlState[4];
            Leave = new ControlState[4];
            for (PlayerIndex p = PlayerIndex.One; p <= PlayerIndex.Four; p++)
            {
                int index = (int)p;
                _inputs[index] = new Input();
                _inputs[index].Controller = p;
                Join[index] = new ControlState();
                _inputs[index].Register(Join[index], Polling.One(Buttons.A));
                Leave[index] = new ControlState();
                _inputs[index].Register(Leave[index], Polling.One(Buttons.B));
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
            return _input.FindActiveController(Polling.One(Buttons.Start));
        }

        private Input _input;
        private Input[] _inputs;
    }
}
