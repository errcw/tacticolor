using System;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

using Strategy.Library.Input;

namespace Strategy.Interface
{
    public class MenuInput
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

        public MenuInput(Game game)
        {
            _input = new Input(game);
            _input.Register(Action, Polling.Any(Polling.One(Buttons.A), Polling.One(Buttons.Start)));
            _input.Register(Cancel, Polling.Any(Polling.One(Buttons.B), Polling.One(Buttons.Back)));
            _input.Register(Up, Polling.Any(Polling.One(Buttons.DPadUp), Polling.One(Buttons.LeftThumbstickUp)));
            _input.Register(Down, Polling.Any(Polling.One(Buttons.DPadDown), Polling.One(Buttons.LeftThumbstickDown)));
        }

        public void Update(float time)
        {
            _input.Update(time);
        }

        private Input _input;
    }
}
