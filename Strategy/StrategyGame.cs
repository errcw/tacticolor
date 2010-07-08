using System;
using System.Collections.Generic;
using System.Linq;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

using Strategy.Gameplay;
using Strategy.Interface;
using Strategy.Library;
using Strategy.Library.Components;
using Strategy.Library.Extensions;
using Strategy.Library.Input;
using Strategy.Properties;

namespace Strategy
{
    /// <summary>
    /// This is the main type for your game
    /// </summary>
    public class StrategyGame : Game
    {
        public StrategyGame()
        {
            GraphicsDeviceManager graphics = new GraphicsDeviceManager(this);
            graphics.PreferredBackBufferWidth = 1280;
            graphics.PreferredBackBufferHeight = 720;
            Window.Title = Resources.StrategyGame;

            Content.RootDirectory = "Content";

            //Components.Add(new TitleSafeAreaOverlayComponent(this));
            //Components.Add(new FPSOverlay(this));

            _context = new InterfaceContext(this, Content, new IsometricParameters(20, 10, 20, -10));
        }

        protected override void Initialize()
        {
            base.Initialize();
            _random = new Random();
            _generator = new MapGenerator(_random);
            StartNewMatch();
        }

        protected override void LoadContent()
        {
            _isoBatch = new IsometricBatch(new SpriteBatch(GraphicsDevice));
            base.LoadContent();
        }

        private void StartNewMatch()
        {
            Map map = _generator.Generate(16, 4, 2, 10);
            _match = new Match(map, _random);
            _matchView = new MatchView(_match, _context);

            _inputA = new LocalInput(PlayerId.A, _match, _context);
            _inputA.Controller = PlayerIndex.One;
            _inputViewA = new LocalInputView(_inputA, _context);
            _inputB = new LocalInput(PlayerId.B, _match, _context);
            _inputB.Controller = PlayerIndex.Two;
            _inputViewB = new LocalInputView(_inputB, _context);
        }

        /// <summary>
        /// Updates the game state.
        /// </summary>
        /// <param name="gameTime">A snapshot of timing values.</param>
        protected override void Update(GameTime gameTime)
        {
            if (Keyboard.GetState().IsKeyDown(Keys.Escape))
            {
                Exit();
            }

            float time = gameTime.GetElapsedSeconds();

            _match.Update(gameTime.GetElapsedMilliseconds());
            _matchView.Update(time);

            _inputA.Update(time);
            _inputViewA.Update(time);
            _inputB.Update(time);
            _inputViewB.Update(time);

            base.Update(gameTime);
        }

        /// <summary>
        /// Draws the game to the screen.
        /// </summary>
        /// <param name="gameTime">A snapshot of timing values.</param>     
        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(new Color(45, 45, 45));

            _matchView.Draw();

            _isoBatch.Begin();
            _inputViewA.Draw(_isoBatch);
            _inputViewB.Draw(_isoBatch);
            _isoBatch.End();

            base.Draw(gameTime);
        }

        private InterfaceContext _context;

        private Random _random;
        private MapGenerator _generator;

        private Match _match;
        private MatchView _matchView;

        private LocalInput _inputA;
        private LocalInputView _inputViewA;
        private LocalInput _inputB;
        private LocalInputView _inputViewB;

        private IsometricBatch _isoBatch;
    }
}
