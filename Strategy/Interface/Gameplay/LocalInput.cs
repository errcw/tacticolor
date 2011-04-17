using System;
using System.Collections.Generic;
using System.Linq;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

using Strategy.Gameplay;
using Strategy.Net;
using Strategy.Library.Extensions;
using Strategy.Library.Input;

namespace Strategy.Interface.Gameplay
{
    /// <summary>
    /// Handles input from a local player.
    /// </summary>
    public class LocalInput : ICommandProvider
    {
        /// <summary>
        /// Occurs when the currently hovered territory changes.
        /// </summary>
        public event EventHandler<InputChangedEventArgs> HoveredChanged;

        /// <summary>
        /// Occurs when the currently selected territory changes.
        /// </summary>
        public event EventHandler<InputChangedEventArgs> SelectedChanged;

        /// <summary>
        /// Occurs when the player performs an action.
        /// </summary>
        public event EventHandler<ActionEventArgs> ActionPerformed;

        /// <summary>
        /// Occurs when the player attempts an invalid action.
        /// </summary>
        public event EventHandler<EventArgs> ActionRejected;

        /// <summary>
        /// Occurs when the player controller is disconnected.
        /// </summary>
        public event EventHandler<EventArgs> ControllerDisconnected;

        /// <summary>
        /// The player for this input.
        /// </summary>
        public PlayerId Player { get; private set; }

        /// <summary>
        /// The territory currently hovered.
        /// </summary>
        public Territory Hovered { get; private set; }

        /// <summary>
        /// The territory currently selected for an action (null for none).
        /// </summary>
        public Territory Selected { get; private set; }

        /// <summary>
        /// True if input is interpreted in screen space; false for isometric space.
        /// </summary>
        public bool UseScreenSpace { get; set; }

        public LocalInput(PlayerId player, PlayerIndex controller, Match match, IsometricParameters isoParams)
        {
            Player = player;
            _match = match;
            _isoParams = isoParams;

            UseScreenSpace = true;

            _input = new Input();
            _input.Controller = controller;
            _input.Register(Move, (state) => state.ThumbSticks.Left.LengthSquared() >= MoveTolerance);
            _input.Register(MoveDirection, Polling.LeftThumbStick);
            _input.Register(Action, Polling.One(Buttons.A));
            _input.Register(Cancel, Polling.One(Buttons.B));
            _input.Register(Place, Polling.One(Buttons.X));
            _input.ControllerDisconnected += (s, a) => ControllerDisconnected(this, a);

            SetHovered(_match.Map.Territories.First(t => t.Owner == Player));
            SetSelected(null, false);
        }

        /// <summary>
        /// Updates the input state.
        /// </summary>
        public MatchCommand Update(int time)
        {
            MatchCommand command = null;

            _input.Update(time / 1000f);

            // the selected territory may have been captured;
            // if so, it is no longer a valid selection
            if (Selected != null && Selected.Owner != Player)
            {
                _actionPending = false;
                SetSelected(null, false);
            }

            if (Action.Pressed)
            {
                if (_actionPending)
                {
                    if (_match.CanMove(Player, Selected, Hovered))
                    {
                        command = new MoveCommand(Player, Selected, Hovered);
                        _actionPending = false;
                    }
                    else if (_match.CanAttack(Player, Selected, Hovered))
                    {
                        command = new AttackCommand(Player, Selected, Hovered);
                        _actionPending = false;
                    }
                    else
                    {
                        NotifyActionRejected();
                    }
                    if (!_actionPending)
                    {
                        SetSelected(null, false);
                    }
                }
                else
                {
                    if (CanSelect(Hovered))
                    {
                        SetSelected(Hovered, true);
                        _actionPending = true;
                    }
                    else
                    {
                        NotifyActionRejected();
                    }
                }
            }
            else if (Cancel.Pressed)
            {
                _actionPending = false;
                SetSelected(null, true);
            }
            else if (Place.Pressed)
            {
                if (_match.CanPlacePiece(Player, Hovered))
                {
                    command = new PlaceCommand(Player, Hovered);
                }
                else
                {
                    NotifyActionRejected();
                }
            }
            else if (Move.Pressed)
            {
                Vector2 direction = GetDirectionInInputSpace();
                Point curLoc = GetPointInInputSpace(Hovered.Location);

                IEnumerable<Territory> territoriesToConsider = (Selected != null)
                    ? Selected.Neighbors.Concat(Selected)
                    : _match.Map.Territories.Where(t => t.Owner == Player);

                List<MoveOption> options = new List<MoveOption>();

                foreach (Territory other in territoriesToConsider)
                {
                    if (other == Hovered)
                    {
                        // zero-distance moves are invalid
                        continue;
                    }

                    Point otherLoc = GetPointInInputSpace(other.Location);
                    Vector2 toOtherLoc = new Vector2(otherLoc.X - curLoc.X, otherLoc.Y - curLoc.Y);

                    float dot = Vector2.Dot(direction, toOtherLoc);
                    float crossMag = direction.X * toOtherLoc.Y - direction.Y * toOtherLoc.X;
                    float angle = (float)Math.Abs(Math.Atan2(crossMag, dot));

                    float distance = Hovered.Neighbors.Contains(other) ? 0 : toOtherLoc.LengthSquared();

                    options.Add(new MoveOption()
                    {
                        Territory = other,
                        Angle = angle,
                        Distance = distance
                    });
                }

                // choose the closest territory with an acceptable angle (possibly none)
                var sortedOptions = options.OrderBy(t => t.Distance).ThenBy(t => t.Angle);
                foreach (MoveOption option in sortedOptions)
                {
                    if (option.Angle < (Math.PI / 3))
                    {
                        SetHovered(option.Territory);
                        break;
                    }
                }
            }

            if (command != null)
            {
                NotifyActionPerformed(command);
            }

            return command;
        }

        private void SetHovered(Territory territory)
        {
            Territory previous = Hovered;
            Hovered = territory;
            if (HoveredChanged != null && Hovered != previous)
            {
                HoveredChanged(this, new InputChangedEventArgs(previous, false));
            }
        }

        private void SetSelected(Territory territory, bool wasPlayerInitiated)
        {
            Territory previous = Selected;
            Selected = territory;
            if (SelectedChanged != null && Selected != previous)
            {
                SelectedChanged(this, new InputChangedEventArgs(previous, wasPlayerInitiated));
            }
        }

        private bool CanSelect(Territory territory)
        {
            return territory.Owner == Player && territory.Pieces.Count > 1 && territory.Cooldown <= 0;
        }

        private void NotifyActionPerformed(Command command)
        {
            if (ActionPerformed != null)
            {
                ActionPerformed(this, new ActionEventArgs(command));
            }
        }

        private void NotifyActionRejected()
        {
            if (ActionRejected != null)
            {
                ActionRejected(this, EventArgs.Empty);
            }
        }

        private Vector2 GetDirectionInInputSpace()
        {
            Vector2 direction = MoveDirection.Position;
            if (UseScreenSpace)
            {
                direction.Y = -direction.Y;
            }
            return direction;
        }

        private Point GetPointInInputSpace(Cell cell)
        {
            if (UseScreenSpace)
            {
                return _isoParams.GetPoint(cell);
            }
            else
            {
                return new Point(cell.Row, cell.Col);
            }
        }

        /// <summary>
        /// Describes a possible move.
        /// </summary>
        private struct MoveOption
        {
            public Territory Territory;
            public float Angle;
            public float Distance;
        }

        private Match _match;
        private IsometricParameters _isoParams;

        private bool _actionPending;

        private Input _input;
        private readonly ControlState Move = new ControlState();
        private readonly ControlPosition MoveDirection = new ControlPosition();
        private readonly ControlState Action = new ControlState();
        private readonly ControlState Cancel = new ControlState();
        private readonly ControlState Place = new ControlState();

        private const float MoveTolerance = 0.5f * 0.5f;
        private const float MoveAngleThreshold = MathHelper.Pi / 6f;
    }

    /// <summary>
    /// Event data for when the hovered or selected territory changes.
    /// </summary>
    public class InputChangedEventArgs : EventArgs
    {
        public readonly Territory PreviousInput;
        public readonly bool WasPlayerInitiated;
        public InputChangedEventArgs(Territory previousInput, bool wasPlayerInitiated)
        {
            PreviousInput = previousInput;
            WasPlayerInitiated = wasPlayerInitiated;
        }
    }

    /// <summary>
    /// Event data for when the player submits an action.
    /// </summary>
    public class ActionEventArgs : EventArgs
    {
        public readonly Command Command;
        public ActionEventArgs(Command command)
        {
            Command = command;
        }
    }
}
