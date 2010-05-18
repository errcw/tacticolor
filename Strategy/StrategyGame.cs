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
        }

        protected override void Initialize()
        {
            base.Initialize();

            _generator = new MapGenerator();
            ShowMap(_generator.Generate(12, 3));
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

        private void ShowMap(Map map)
        {
            _sprites.Clear();
            _spritesLow.Clear();
            foreach (Territory territory in map.Territories)
            {
                _sprites.AddRange(ShowTerritory(territory));
                foreach (Territory other in territory.Adjacent)
                {
                    _spritesLow.AddRange(ShowConnection(territory, other));
                }
            }
        }

        const int ROX = 22;
        const int ROY = 11;
        const int COX = 20;
        const int COY = -10;

        private List<IsometricSprite> ShowTerritory(Territory territory)
        {
            List<IsometricSprite> tileSprites = new List<IsometricSprite>(25);
            bool[,] layout = GenerateTerritoryLayout();

            int tr = (int)territory.Position.X - 2;
            int tc = (int)territory.Position.Y - 2;

            for (int r = 0; r < layout.GetLength(0); r++)
            {
                for (int c = 0; c < layout.GetLength(1); c++)
                {
                    if (layout[r, c])
                    {
                        IsometricSprite sprite = new IsometricSprite(_tile);
                        sprite.X = (tr + r) * ROX + (tc + c) * COX + 500;
                        sprite.Y = (tr + r) * ROY + (tc + c) * COY + 150;
                        tileSprites.Add(sprite);
                    }
                }
            }

            return tileSprites;
        }

        private List<IsometricSprite> ShowConnection(Territory a, Territory b)
        {
            List<IsometricSprite> sprites = new List<IsometricSprite>(25);
            foreach (Vector2 v in BresenhamIterator.GetPointsOnLine((int)a.Position.X, (int)a.Position.Y, (int)b.Position.X, (int)b.Position.Y))
            {
                IsometricSprite sprite = new IsometricSprite(_conn);
                sprite.X = (v.X) * ROX + (v.Y) * COX + 500;
                sprite.Y = (v.X) * ROY + (v.Y) * COY + 150;
                sprites.Add(sprite);
            }
            return sprites;
        }

        private bool[,] GenerateTerritoryLayout()
        {
            bool[,] layout = new bool[5, 5];

            const int CENTER_START = 1;
            const int CENTER_END = 4;

            // fill in the center for the piece tiles
            for (int r = CENTER_START; r < CENTER_END; r++)
            {
                for (int c = CENTER_START; c < CENTER_END; c++)
                {
                    layout[r, c] = true;
                }
            }
            
            // add randomized decoration
            for (int d = 0; d < 5; d++)
            {
                int baseRow = random.Next(CENTER_START, CENTER_END);
                int deltaRows = random.Next(-5, 5);
                int startRow = Math.Min(baseRow, baseRow + deltaRows);
                int endRow = Math.Max(baseRow, baseRow + deltaRows);

                int baseCol = random.Next(CENTER_START, CENTER_END);
                int deltaCols = random.Next(-5, 5);
                int startCol = Math.Min(baseCol, baseCol + deltaCols);
                int endCol = Math.Max(baseCol, baseCol + deltaCols);

                for (int r = startRow; r <= endRow; r++)
                {
                    for (int c = startCol; c <= endCol; c++)
                    {
                        if (r >= 0 && r < layout.GetLength(0) &&
                            c >= 0 && c < layout.GetLength(1))
                        {
                            layout[r, c] = true;
                        }
                    }
                }
            }

            return layout;
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
                ShowMap(_generator.Generate(12, 3));
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
                case PlayerId.A: return Color.Red;
                case PlayerId.B: return Color.Blue;
                case PlayerId.C: return Color.Green;
                case PlayerId.D: return Color.Orange;
                case null: return Color.Black;
                default: return Color.Black;
            }
        }

        Random random = new Random();

        private MapGenerator _generator;

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
