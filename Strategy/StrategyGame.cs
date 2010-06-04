using System;
using System.Collections.Generic;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.GamerServices;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Media;
using Microsoft.Xna.Framework.Net;
using Microsoft.Xna.Framework.Storage;

using Strategy.Gameplay;
using Strategy.Interface;
using Strategy.Library;
using Strategy.Properties;

namespace Strategy
{
    /// <summary>
    /// This is the main type for your game
    /// </summary>
    public class StrategyGame : Microsoft.Xna.Framework.Game
    {
        public StrategyGame()
        {
            GraphicsDeviceManager graphics = new GraphicsDeviceManager(this);
            graphics.PreferredBackBufferWidth = 1280;
            graphics.PreferredBackBufferHeight = 720;
            Window.Title = Resources.StrategyGame;

            Content.RootDirectory = "Content";

            //Components.Add(new TitleSafeAreaOverlayComponent(this));
        }

        protected override void Initialize()
        {
            base.Initialize();

            _generator = new GridMapGenerator();
            ShowMap(_generator.Generate(20, 5));
        }

        protected override void LoadContent()
        {
            base.LoadContent();

            _colourable = Content.Load<Texture2D>("Colourable");
            _tile = Content.Load<Texture2D>("TileSmall");
            _piece = Content.Load<Texture2D>("Piece");
            _conn = Content.Load<Texture2D>("Connection");

            _spriteBatch = new SpriteBatch(GraphicsDevice);
            _isoBatch = new IsometricBatch(_spriteBatch);
        }

        private void ShowMap(GridMap map)
        {
            _sprites.Clear();
            _spritesLow.Clear();
            foreach (GridTerritory territory in map.Territories)
            {
                _sprites.AddRange(ShowTerritory(territory));
                foreach (GridTerritory other in territory.Adjacent)
                {
                    _spritesLow.AddRange(ShowConnection(territory, other));
                }
            }
        }

        const int ROX = 22;
        const int ROY = 11;
        const int COX = 20;
        const int COY = -10;
        const int BASEX = 0;
        const int BASEY = 300;

        private List<IsometricSprite> ShowTerritory(GridTerritory territory)
        {
            List<IsometricSprite> tileSprites = new List<IsometricSprite>(25);
            foreach (Point p in territory.Area)
            {
                IsometricSprite sprite = new IsometricSprite(_tile);
                sprite.X = p.Y * ROX + p.X * COX + BASEX;
                sprite.Y = p.Y * ROY + p.X * COY + BASEY;
                sprite.Tint = GetPlayerColor(territory.Owner);
                tileSprites.Add(sprite);
            }
            return tileSprites;
        }

        private List<IsometricSprite> ShowConnection(GridTerritory a, GridTerritory b)
        {
            List<IsometricSprite> sprites = new List<IsometricSprite>(25);
            foreach (Point p in BresenhamIterator.GetPointsOnLine(a.Location.Y, a.Location.X, b.Location.Y, b.Location.X))
            {
                IsometricSprite sprite = new IsometricSprite(_conn);
                sprite.X = p.X * ROX + p.Y * COX + BASEX;
                sprite.Y = p.X * ROY + p.Y * COY + BASEY;
                sprites.Add(sprite);
            }
            return sprites;
        }

        /// <summary>
        /// Updates the game state.
        /// </summary>
        /// <param name="gameTime">A snapshot of timing values.</param>
        protected override void Update(GameTime gameTime)
        {
            if (Keyboard.GetState().IsKeyDown(Keys.Escape))
            {
                for (PlayerIndex p = PlayerIndex.One; p <= PlayerIndex.Four; p++)
                {
                    System.Console.WriteLine(GamePad.GetState(p).IsConnected);
                }
                Exit();
            }
            if (Keyboard.GetState().IsKeyDown(Keys.R))
            {
                _rWasDown = true;
            }
            else if (Keyboard.GetState().IsKeyUp(Keys.R) && _rWasDown)
            {
                ShowMap(_generator.Generate(16, 4));
                _rWasDown = false;
            }
            base.Update(gameTime);
        }

        /// <summary>
        /// Draws the game to the screen.
        /// </summary>
        /// <param name="gameTime">A snapshot of timing values.</param>     
        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(new Color(45, 45, 45));

            _isoBatch.Begin();
            foreach (IsometricSprite sprite in _spritesLow)
            {
                _isoBatch.Draw(sprite);
            }
            _isoBatch.End();

            _isoBatch.Begin();
            foreach (IsometricSprite sprite in _sprites)
            {
                _isoBatch.Draw(sprite);
            }
            _isoBatch.End();

            base.Draw(gameTime);
        }

        private Color GetPlayerColor(PlayerId? player)
        {
            switch (player)
            {
                case PlayerId.A: return Color.Tomato;
                case PlayerId.B: return Color.RoyalBlue;
                case PlayerId.C: return Color.SeaGreen;
                case PlayerId.D: return Color.Crimson;
                case null: return Color.White;
                default: return Color.White;
            }
        }

        Random random = new Random();

        private GridMapGenerator _generator;

        private SpriteBatch _spriteBatch;
        private IsometricBatch _isoBatch;
        private Texture2D _colourable;
        private Texture2D _tile;
        private Texture2D _piece;
        private Texture2D _conn;
        private List<IsometricSprite> _sprites = new List<IsometricSprite>();
        private List<IsometricSprite> _spritesLow = new List<IsometricSprite>();

        private bool _rWasDown;
    }
}
