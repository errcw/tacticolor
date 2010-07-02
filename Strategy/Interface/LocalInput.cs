using System;
using System.Linq;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

using Strategy.Gameplay;
using Strategy.Library.Input;

namespace Strategy.Interface
{
    /// <summary>
    /// Handles input from a local player.
    /// </summary>
    public class LocalInput
    {
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

        public LocalInput(LocalInputPolling input, PlayerId player, Match match, InterfaceContext context)
        {
            _input = input;
            _match = match;
            _context = context;

            Player = player;
            Hovered = match.Map.Territories.First();
            Selected = null;
        }

        /// <summary>
        /// Updates the input state.
        /// </summary>
        public void Update()
        {
            if (_input.Action.Pressed)
            {
                if (_actionPending)
                {
                    if (_match.CanMove(Player, Selected, Hovered))
                    {
                        _match.Move(Selected, Hovered);
                        _actionPending = false;
                    }
                    else if (_match.CanAttack(Player, Selected, Hovered))
                    {
                        _match.Attack(Selected, Hovered);
                        _actionPending = false;
                    }
                }
                else
                {
                    Selected = Hovered;
                    _actionPending = true;
                }
            }
            else if (_input.Cancel.Pressed)
            {
                _actionPending = false;
                Selected = null;
            }
            else if (_input.Place.Pressed)
            {
                if (_match.CanPlacePiece(Player, Hovered))
                {
                    _match.PlacePiece(Hovered);
                }
            }
            else if (_input.Move.Pressed)
            {
                Vector2 direction = _input.MoveDirection.Position;
                direction.Y = -direction.Y;

                Territory newHovered = null;
                float minAngle = float.MaxValue;
                Point curLoc = _context.IsoParams.GetPoint(Hovered.Location);

                foreach (Territory other in Hovered.Neighbors)
                {
                    Point otherLoc = _context.IsoParams.GetPoint(other.Location);
                    Vector2 toOtherLoc = new Vector2(otherLoc.X - curLoc.X, otherLoc.Y - curLoc.Y);

                    float dot = Vector2.Dot(direction, toOtherLoc);
                    float crossMag = direction.X * toOtherLoc.Y - direction.Y * toOtherLoc.X;
                    float angle = (float)Math.Abs(Math.Atan2(crossMag, dot));

                    if (angle < MoveAngleThreshold && angle < minAngle)
                    {
                        minAngle = angle;
                        newHovered = other;
                    }
                }

                if (newHovered != null)
                {
                    Hovered = newHovered;
                }
            }

        }

        private LocalInputPolling _input;
        private Match _match;
        private InterfaceContext _context;

        private bool _actionPending;

        private const float MoveAngleThreshold = MathHelper.PiOver2;
    }
}
