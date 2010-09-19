using System;
using System.Linq;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using Strategy.Gameplay;
using Strategy.Library;
using Strategy.Library.Extensions;
using Strategy.Library.Sprite;
using Strategy.Properties;

namespace Strategy.Interface.Gameplay
{
    /// <summary>
    /// Shows instructions for the controls and how to interact with the game.
    /// </summary>
    public class Instructions
    {
        public bool Enabled { get; set; }

        public Instructions(LocalInput input, Match match, InterfaceContext context)
        {
            Enabled = true;

            _input = input;
            _input.HoveredChanged += OnHoveredChanged;
            _input.SelectedChanged += OnSelectedChanged;
            _input.ActionRejected += OnActionRejected;

            _match = match;
            _match.PiecesMoved += OnPiecesMoved;
            _match.TerritoryAttacked += OnTerritoryAttacked;
            _match.PiecePlaced += OnPiecePlaced;

            _options = context.Game.Services.GetService<Options>();

            _imageTextures = new Texture2D[8];
            _imageTextures[(int)InstructionState.Idle] = context.Content.Load<Texture2D>("Images/ButtonA");
            _imageTextures[(int)InstructionState.Selection] = context.Content.Load<Texture2D>("Images/ButtonA");
            _imageTextures[(int)InstructionState.Movement] = context.Content.Load<Texture2D>("Images/ButtonThumb");
            _imageTextures[(int)InstructionState.Action] = context.Content.Load<Texture2D>("Images/ButtonA");
            _imageTextures[(int)InstructionState.Cancel] = context.Content.Load<Texture2D>("Images/ButtonB");
            _imageTextures[(int)InstructionState.Placement] = context.Content.Load<Texture2D>("Images/ButtonX");
            _imageTextures[(int)InstructionState.Readiness] = context.Content.Load<Texture2D>("Images/InstructionsPiece");
            _imageTextures[(int)InstructionState.Owning] = context.Content.Load<Texture2D>("Images/InstructionsPiece");
            _panel = new SlidingPanel(Resources.InstructionsAction, _imageTextures[0], 75, context.Content);

            _state = InstructionState.Idle;
        }

        public void Update(float time)
        {
            // do not update the instructions if requested by the user
            if (!Enabled || !_options.InstructionsToggle)
            {
                return;
            }

            // choose a new instruction path
            if (_state == InstructionState.Idle)
            {
                // show the basics before any other instructions
                if (!_showed[(int)InstructionState.Action])
                {
                    if (_input.Hovered.Owner == _input.Player)
                    {
                        SetState(InstructionState.Selection);
                    }
                }
                else
                {
                    if (!_showed[(int)InstructionState.Placement] && _match.PiecesAvailable[(int)_input.Player] >= 2)
                    {
                        SetState(InstructionState.Placement);
                    }
                    else if (!_showed[(int)InstructionState.Readiness] && _input.Selected != null && _input.Selected.Pieces.Count(p => !p.Ready) > 0)
                    {
                        SetState(InstructionState.Readiness);
                    }
                    else if (!_showed[(int)InstructionState.Cancel] && _input.Selected != null && _input.Selected.Pieces.Count(p => p.Ready) == 0)
                    {
                        SetState(InstructionState.Cancel);
                    }
                    else if (!_showed[(int)InstructionState.Owning] && _match.Map.Territories.Count(t => t.Owner == _input.Player) >= 4)
                    {
                        SetState(InstructionState.Owning);
                    }
                }
            }

            // only show the instructions once
            if (_showed.All(b => b) && !_panel.IsVisible)
            {
                _options.InstructionsToggle = false;
            }

            _panel.Update(time);
        }

        public void Draw(SpriteBatch spriteBatch)
        {
            // hide the instructions if requested by the user
            if (!Enabled || !_options.InstructionsToggle)
            {
                return;
            }
            _panel.Draw(spriteBatch);
        }

        private void OnHoveredChanged(object inputObj, InputChangedEventArgs args)
        {
            if (_input.Hovered != _input.Selected && _state == InstructionState.Movement)
            {
                SetState(InstructionState.Action);
            }
        }

        private void OnSelectedChanged(object inputObj, InputChangedEventArgs args)
        {
            if (_input.Selected != null && _state == InstructionState.Selection)
            {
                SetState(InstructionState.Movement);
            }
            else if (_input.Selected == null && _state == InstructionState.Cancel)
            {
                SetState(InstructionState.Idle);
            }
        }

        private void OnActionRejected(object inputObj, EventArgs args)
        {
            if (_input.Selected == null && _input.Hovered.Owner == _input.Player && _input.Hovered.Pieces.Count <= 1)
            {
                // show players must select a territory with more than one piece?
            }
        }

        private void OnPiecesMoved(object matchObj, PiecesMovedEventArgs args)
        {
            if (args.Source.Owner == _input.Player && (_state == InstructionState.Action || _state == InstructionState.Readiness || _state == InstructionState.Owning))
            {
                SetState(InstructionState.Idle);
            }
        }

        private void OnTerritoryAttacked(object matchObj, TerritoryAttackedEventArgs args)
        {
            if (args.Attacker.Owner == _input.Player && (_state == InstructionState.Action || _state == InstructionState.Readiness || _state == InstructionState.Owning))
            {
                SetState(InstructionState.Idle);
            }
        }

        private void OnPiecePlaced(object matchObj, PiecePlacedEventArgs args)
        {
            if (args.Location.Owner == _input.Player && (_state == InstructionState.Placement || _state == InstructionState.Readiness || _state == InstructionState.Owning))
            {
                SetState(InstructionState.Idle);
            }
        }

        private void SetState(InstructionState state)
        {
            _showed[(int)_state] = true;
            _state = state;

            Texture2D imageTex = _imageTextures[(int)_state];
            switch (_state)
            {
                case InstructionState.Idle:
                    _panel.Hide();
                    break;
                case InstructionState.Selection:
                    _panel.Show(Resources.InstructionsSelection, imageTex);
                    break;
                case InstructionState.Movement:
                    _panel.Show(Resources.InstructionsMovement, imageTex);
                    break;
                case InstructionState.Action:
                    _panel.Show(Resources.InstructionsAction, imageTex);
                    break;
                case InstructionState.Cancel:
                    _panel.Show(Resources.InstructionsCancel, imageTex);
                    break;
                case InstructionState.Placement:
                    _panel.Show(Resources.InstructionsPlacement, imageTex);
                    break;
                case InstructionState.Readiness:
                    _panel.Show(Resources.InstructionsWaiting, imageTex);
                    break;
                case InstructionState.Owning:
                    _panel.Show(Resources.InstructionsTerritories, imageTex);
                    break;
            }
        }

        private enum InstructionState
        {
            Idle,
            Selection,
            Movement,
            Action,
            Cancel,
            Placement,
            Readiness,
            Owning,
        }

        private InstructionState _state;
        private bool[] _showed = new bool[8];

        private LocalInput _input;
        private Match _match;
        private Options _options;

        private Texture2D[] _imageTextures;
        private SlidingPanel _panel;
    }
}
