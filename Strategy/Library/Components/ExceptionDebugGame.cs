using System;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.GamerServices;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

using Strategy.Properties;

namespace Strategy.Library.Components
{
    /// <summary>
    /// Displays an exception.
    /// </summary>
    /// <remarks>Adapted from http://nickgravelyn.com/2008/10/catching-exceptions-on-xbox-360/. </remarks>
    public class ExceptionDebugGame : Game
    {
        /// <summary>
        /// The name of the font to display the exception with.
        /// </summary>
        public string FontName { get; set; }

        /// <summary>
        /// Creates a new execption debug game.
        /// </summary>
        /// <param name="exception">The exception to display.</param>
        public ExceptionDebugGame(Exception exception)
        {
            Exception = exception;
            FontName = "Fonts/TextSmall";

            new GraphicsDeviceManager(this)
            {
                PreferredBackBufferWidth = 1280,
                PreferredBackBufferHeight = 720
            };

            // do not add this as a component because we do not want it to be
            // reinitialized if the faulting game has already initialized it;
            // instead we only want to call Update on it
            _services = new GamerServicesComponent(this);

            Content.RootDirectory = "Content";
        }

        protected override void LoadContent()
        {
            _font = Content.Load<SpriteFont>(FontName);
            _spriteBatch = new SpriteBatch(GraphicsDevice);
        }

        protected override void Update(GameTime gameTime)
        {
            if (!_messageDisplayed)
            {
                try
                {
                    if (!Guide.IsVisible)
                    {
                        Guide.BeginShowMessageBox(
                            Resources.ExceptionMessageTitle,
                            Resources.ExceptionMessage,
                            new string[] { Resources.ExceptionMessageExit,
                                           Resources.ExceptionMessageDebug },
                            0,
                            MessageBoxIcon.Error,
                            result =>
                            {
                                int? choice = Guide.EndShowMessageBox(result);
                                if (choice.HasValue && choice.Value == 0)
                                {
                                    Exit();
                                }
                            },
                            null);
                        _messageDisplayed = true;
                    }
                }
                catch (Exception e)
                {
                    System.Diagnostics.Debug.WriteLine(e);
                }
            }
            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed)
            {
                Exit();
            }
            _services.Update(gameTime);
            base.Update(gameTime);
        }

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.Black);

            _spriteBatch.Begin();
            _spriteBatch.DrawString(
                 _font,
                 Resources.ExceptionHeader,
                 new Vector2(100f, 100f),
                 Color.White);
            _spriteBatch.DrawString(
                 _font,
                 Resources.ExceptionExitPrompt,
                 new Vector2(100f, 120f),
                 Color.White);
            _spriteBatch.DrawString(
                 _font,
                 string.Format(Resources.ExceptionException, Exception.Message),
                 new Vector2(100f, 140f),
                 Color.White);
            _spriteBatch.DrawString(
                 _font,
                 string.Format(Resources.ExceptionTrace, Exception.StackTrace),
                 new Vector2(100f, 160f),
                 Color.White);
            _spriteBatch.End();

            base.Draw(gameTime);
        }

        private bool _messageDisplayed = false;

        private SpriteBatch _spriteBatch;
        private SpriteFont _font;

        private GamerServicesComponent _services;

        private readonly Exception Exception;
    }
}
