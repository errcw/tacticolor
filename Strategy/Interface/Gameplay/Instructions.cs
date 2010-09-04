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


            Texture2D background = context.Content.Load<Texture2D>("Images/InstructionsBackground");
            SpriteFont font = context.Content.Load<SpriteFont>("Fonts/TextLarge");
            Vector2 charSize = font.MeasureString(Resources.InstructionsAction);

            _imageTextures = new Texture2D[8];
            _imageTextures[(int)InstructionState.Idle] = context.Content.Load<Texture2D>("Images/ButtonA");
            _imageTextures[(int)InstructionState.Selection] = context.Content.Load<Texture2D>("Images/ButtonA");
            _imageTextures[(int)InstructionState.Movement] = context.Content.Load<Texture2D>("Images/ButtonThumb");
            _imageTextures[(int)InstructionState.Action] = context.Content.Load<Texture2D>("Images/ButtonA");
            _imageTextures[(int)InstructionState.Cancel] = context.Content.Load<Texture2D>("Images/ButtonB");
            _imageTextures[(int)InstructionState.Placement] = context.Content.Load<Texture2D>("Images/ButtonX");
            _imageTextures[(int)InstructionState.Readiness] = context.Content.Load<Texture2D>("Images/InstructionsPiece");
            _imageTextures[(int)InstructionState.Owning] = context.Content.Load<Texture2D>("Images/InstructionsPiece");

            ImageSprite backSprite = new ImageSprite(background);

            _imageSprite = new ImageSprite(_imageTextures[0]);
            _imageSprite.Position = new Vector2(5, (int)((40 - _imageSprite.Size.Y) / 2));

            _textSprite = new TextSprite(font);
            _textSprite.Color = Color.Black;
            _textSprite.Position = new Vector2(_imageSprite.Size.X + 10, (int)((backSprite.Size.Y - charSize.Y) / 2));

            _sprite = new CompositeSprite(backSprite, _imageSprite, _textSprite);
            _sprite.Position = Hidden;

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
            if (_showed.All(b => b) && _sprite.Position == Hidden)
            {
                _options.InstructionsToggle = false;
            }

            // run the animation
            if (_animation != null)
            {
                if (!_animation.Update(time))
                {
                    _animation = null;
                }
            }
        }

        public void Draw(SpriteBatch spriteBatch)
        {
            // hide the instructions if requested by the user
            if (!Enabled || !_options.InstructionsToggle)
            {
                return;
            }
            _sprite.Draw(spriteBatch);
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
                    SetInstructions(String.Empty, imageTex);
                    break;
                case InstructionState.Selection:
                    SetInstructions(Resources.InstructionsSelection, imageTex);
                    break;
                case InstructionState.Movement:
                    SetInstructions(Resources.InstructionsMovement, imageTex);
                    break;
                case InstructionState.Action:
                    SetInstructions(Resources.InstructionsAction, imageTex);
                    break;
                case InstructionState.Cancel:
                    SetInstructions(Resources.InstructionsCancel, imageTex);
                    break;
                case InstructionState.Placement:
                    SetInstructions(Resources.InstructionsPlacement, imageTex);
                    break;
                case InstructionState.Readiness:
                    SetInstructions(Resources.InstructionsWaiting, imageTex);
                    break;
                case InstructionState.Owning:
                    SetInstructions(Resources.InstructionsTerritories, imageTex);
                    break;
            }
        }

        private void SetInstructions(string newText, Texture2D newImage)
        {
            IAnimation setNewInstructions = new CompositeAnimation(
                new TextAnimation(_textSprite, newText),
                new ImageAnimation(_imageSprite, newImage));
            if (_textSprite.Text.Length > 0 && newText.Length > 0)
            {
                // replace the existing text
                _animation = new SequentialAnimation(
                    new CompositeAnimation(
                        new ColorAnimation(_textSprite, Color.TransparentBlack, 0.2f, Interpolation.InterpolateColor(Easing.Uniform)),
                        new ColorAnimation(_imageSprite, Color.TransparentWhite, 0.2f, Interpolation.InterpolateColor(Easing.Uniform))),
                    new DelayAnimation(0.1f),
                    setNewInstructions,
                    new CompositeAnimation(
                        new ColorAnimation(_textSprite, Color.Black, 0.2f, Interpolation.InterpolateColor(Easing.Uniform)),
                        new ColorAnimation(_imageSprite, Color.White, 0.2f, Interpolation.InterpolateColor(Easing.Uniform))));

                // if the panel was on its way out bring it back
                if (_sprite.Position != Visible)
                {
                    _animation = new CompositeAnimation(
                        _animation,
                        new PositionAnimation(_sprite, Visible, 0.3f, Interpolation.InterpolateVector2(Easing.QuadraticOut)));
                }
            }
            else
            {
                if (newText.Length > 0)
                {
                    // show the instructions panel
                    _animation = new SequentialAnimation(
                        setNewInstructions,
                        new PositionAnimation(_sprite, Visible, 1f, Interpolation.InterpolateVector2(Easing.QuadraticOut)));
                }
                else
                {
                    // hide the instructions panel
                    _animation = new SequentialAnimation(
                        new PositionAnimation(_sprite, Hidden, 1f, Interpolation.InterpolateVector2(Easing.QuadraticIn)),
                        setNewInstructions);
                }
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

        private Sprite _sprite;
        private IAnimation _animation;

        private TextSprite _textSprite;
        private ImageSprite _imageSprite;
        private Texture2D[] _imageTextures;

        private readonly Vector2 Visible = new Vector2(650, 75);
        private readonly Vector2 Hidden = new Vector2(1280, 75);
    }
}
