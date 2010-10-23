using System;
using System.Collections.Generic;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.GamerServices;
using Microsoft.Xna.Framework.Graphics;

using Strategy.Gameplay;
using Strategy.Library;
using Strategy.Library.Extensions;
using Strategy.Library.Screen;
using Strategy.Library.Sprite;
using Strategy.Properties;

namespace Strategy.Interface
{
    public class SlidingPanel
    {
        /// <summary>
        /// If this panel is visible or not.
        /// </summary>
        public bool IsVisible
        {
            get { return _sprite.Position != Hidden; }
        }

        public SlidingPanel(string text, Texture2D image, int verticalPosition, ContentManager content)
        {
            Visible = new Vector2(650, verticalPosition);
            Hidden = new Vector2(1280, verticalPosition);

            Texture2D background = content.Load<Texture2D>("Images/SlidingPanelBackground");
            SpriteFont font = content.Load<SpriteFont>("Fonts/TextLarge");
            Vector2 charSize = font.MeasureString(text);

            ImageSprite backSprite = new ImageSprite(background);

            _imageSprite = new ImageSprite(image);
            _imageSprite.Position = new Vector2(5, (int)((40/*height w/o shadow*/ - _imageSprite.Size.Y) / 2));

            _textSprite = new TextSprite(font, text);
            _textSprite.Color = Color.Black;
            _textSprite.Position = new Vector2(_imageSprite.Size.X + 10, (int)((backSprite.Size.Y - charSize.Y) / 2));

            _sprite = new CompositeSprite(backSprite, _imageSprite, _textSprite);
            _sprite.Position = Hidden;
        }

        public void Update(float time)
        {
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
            _sprite.Draw(spriteBatch);
        }

        /// <summary>
        /// Changes the contents of the panel. Slides out the panel if it is hidden.
        /// </summary>
        /// <param name="newText">The new text to display.</param>
        /// <param name="newImage">The new image to display.</param>
        public void Show(string newText, Texture2D newImage)
        {
            IAnimation setNewInstructions = new CompositeAnimation(
                new TextAnimation(_textSprite, newText),
                new ImageAnimation(_imageSprite, newImage));
            if (IsVisible)
            {
                // replace the existing text
                _animation = new SequentialAnimation(
                    new CompositeAnimation(
                        new ColorAnimation(_textSprite, Color.Transparent, 0.2f, Interpolation.InterpolateColor(Easing.Uniform)),
                        new ColorAnimation(_imageSprite, Color.Transparent, 0.2f, Interpolation.InterpolateColor(Easing.Uniform))),
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
                // show the panel with the new content
                _animation = new SequentialAnimation(
                    setNewInstructions,
                    new PositionAnimation(_sprite, Visible, 1f, Interpolation.InterpolateVector2(Easing.QuadraticOut)));
            }
        }

        /// <summary>
        /// Hides the panel.
        /// </summary>
        public void Hide()
        {
            _animation = new PositionAnimation(_sprite, Hidden, 1f, Interpolation.InterpolateVector2(Easing.QuadraticIn));
        }

        private Sprite _sprite;
        private TextSprite _textSprite;
        private ImageSprite _imageSprite;
        private IAnimation _animation;

        private readonly Vector2 Visible;
        private readonly Vector2 Hidden;
    }
}
