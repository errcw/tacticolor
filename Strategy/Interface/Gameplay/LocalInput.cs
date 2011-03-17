using System;
using System.Collections.Generic;
using System.Linq;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

using Strategy.Gameplay;
using Strategy.Net;
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
            SetSelected(null, true);
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
                SetSelected(null, true);
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
                        SetSelected(null, true);
                    }
                }
                else
                {
                    if (CanSelect(Hovered))
                    {
                        SetSelected(Hovered, false);
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
                SetSelected(null, false);
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

                Territory newHovered = null;
                float minAngle = float.MaxValue;
                float minDistance2 = float.MaxValue;
                Point curLoc = GetPointInInputSpace(Hovered.Location);

                IEnumerable<Territory> territoriesToConsider = (Selected != null)
                    ? Selected.Neighbors.Concat(Enumerable.Repeat(Selected, 1))
                    : _match.Map.Territories.Where(t => t.Owner == Player);

                foreach (Territory other in territoriesToConsider)
                {
                    if (other == Hovered)
                    {
                        // zero-distance moves are invalid
                        continue;
                    }

                    Point otherLoc = GetPointInInputSpace(other.Location);
                    Vector2 toOtherLoc = new Vector2(otherLoc.X - curLoc.X, otherLoc.Y - curLoc.Y);

                    float distance2 = toOtherLoc.LengthSquared();

                    float dot = Vector2.Dot(direction, toOtherLoc);
                    float crossMag = direction.X * toOtherLoc.Y - direction.Y * toOtherLoc.X;
                    float angle = (float)Math.Abs(Math.Atan2(crossMag, dot));

                    // choose the best angle and distance, but favour localaity over minimizing the angle
                    bool areAnglesClose = Math.Abs(angle - minAngle) < MoveAngleThreshold;
                    if ((angle < minAngle && distance2 < minDistance2) ||
                        (areAnglesClose && distance2 < minDistance2) || (!areAnglesClose && angle < minAngle))
                    {
                        minAngle = angle;
                        minDistance2 = distance2;
                        newHovered = other;
                    }
                }

                if (newHovered != null)
                {
                    SetHovered(newHovered);
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

        private void SetSelected(Territory territory, bool wasForced)
        {
            Territory previous = Selected;
            Selected = territory;
            if (SelectedChanged != null && Selected != previous)
            {
                SelectedChanged(this, new InputChangedEventArgs(previous, wasForced));
            }
        }

        private bool CanSelect(Territory territory)
        {
            return territory.Owner == Player && territory.Pieces.Count > 1;
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

        private Match _match;
        private IsometricParameters _isoParams;

        private bool _actionPending;

        private Input _input;
        private readonly ControlState Move = new ControlState() { RepeatEnabled = true };
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
        public readonly bool WasForced;
        public InputChangedEventArgs(Territory previousInput, bool wasForced)
        {
            PreviousInput = previousInput;
            WasForced = wasForced;
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
