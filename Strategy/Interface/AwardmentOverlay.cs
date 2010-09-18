using System;
using System.Collections.Generic;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.GamerServices;
using Microsoft.Xna.Framework.Graphics;

using Strategy.Gameplay;
using Strategy.Library;
using Strategy.Library.Extensions;
using Strategy.Library.Screen;
using Strategy.Library.Sprite;

namespace Strategy.Interface
{
    /// <summary>
    /// Shows awardments earned by players.
    /// </summary>
    public class AwardmentOverlay : Screen
    {
        public AwardmentOverlay(StrategyGame game, Awardments awardments)
        {
            awardments.AwardmentEarned += (s, a) => _pendingAwardments.Enqueue(a);

            Texture2D background = game.Content.Load<Texture2D>("Images/InstructionsBackground");
            Texture2D trophy = game.Content.Load<Texture2D>("Images/PieceAvailable");
            SpriteFont font = game.Content.Load<SpriteFont>("Fonts/TextLarge");
            Vector2 charSize = font.MeasureString("W");

            ImageSprite backSprite = new ImageSprite(background);

            _imageSprite = new ImageSprite(trophy);
            _imageSprite.Position = new Vector2(5, (int)((40 - _imageSprite.Size.Y) / 2));

            _textSprite = new TextSprite(font);
            _textSprite.Color = Color.Black;
            _textSprite.Position = new Vector2(_imageSprite.Size.X + 10, (int)((backSprite.Size.Y - charSize.Y) / 2));

            _sprite = new CompositeSprite(backSprite, _imageSprite, _textSprite);
            _sprite.Position = Hidden;

            _spriteBatch = new SpriteBatch(game.GraphicsDevice);

            TransitionOnTime = 0f;
            TransitionOffTime = 0f;
            ShowBeneath = true;
        }

        public override void Draw()
        {
            _spriteBatch.Begin(SpriteBlendMode.AlphaBlend, SpriteSortMode.Immediate, SaveStateMode.None);
            _sprite.Draw(_spriteBatch);
            _spriteBatch.End();
        }

        protected override void UpdateActive(GameTime gameTime)
        {
            float time = gameTime.GetElapsedSeconds();

            if (_displayedAwardment != null)
            {
                _displayTime -= time;
                if (_displayTime <= 0)
                {
                    SetAwardment(string.Empty, Color.White);
                    _displayedAwardment = null;
                }
            }
            if (_displayedAwardment == null && _pendingAwardments.Count > 0)
            {
                _displayedAwardment = _pendingAwardments.Dequeue();
                SetAwardment(_displayedAwardment.Awardment.Name, Color.White);
                _displayTime = DisplayTime;
            }

            if (_animation != null)
            {
                if (!_animation.Update(time))
                {
                    _animation = null;
                }
            }
        }

        private SignedInGamer GetGamerForGamertag(string gamertag)
        {
            foreach (SignedInGamer gamer in SignedInGamer.SignedInGamers)
            {
                if (gamer.Gamertag == gamertag)
                {
                    return gamer;
                }
            }
            return null;
        }

        private void SetAwardment(string newText, Color newColor)
        {
            IAnimation setNewInstructions = new CompositeAnimation(
                new TextAnimation(_textSprite, newText),
                new ColorAnimation(_imageSprite, newColor, 0f, Interpolation.InterpolateColor(Easing.Uniform)));
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

        private Queue<AwardmentEventArgs> _pendingAwardments = new Queue<AwardmentEventArgs>();
        private AwardmentEventArgs _displayedAwardment = null;
        private float _displayTime;

        private Sprite _sprite;
        private TextSprite _textSprite;
        private ImageSprite _imageSprite;
        private IAnimation _animation;
        private SpriteBatch _spriteBatch;

        private readonly Vector2 Visible = new Vector2(650, 75);
        private readonly Vector2 Hidden = new Vector2(1280, 75);
        private const float DisplayTime = 3f;
    }
}
