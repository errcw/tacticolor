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

            MapGenerator generator = new MapGenerator();
            _map = generator.Generate(16, 4);
        }

        protected override void LoadContent()
        {
            base.LoadContent();

            _colourable = Content.Load<Texture2D>("Colourable");
            _tile = Content.Load<Texture2D>("Tile");
            _piece = Content.Load<Texture2D>("Piece");

            _spriteBatch = new SpriteBatch(GraphicsDevice);
            _isoBatch = new IsometricBatch(_spriteBatch, Content.Load<SpriteFont>("Debug"));
        }

        private void ShowTerritory()
        {
            const int ROX = -38;
            const int ROY = 19;
            const int COX = 40;
            const int COY = 20;
            bool[,] territory = GenerateTerritoryLayout();

            _tiles.Clear();
            for (int r = 0; r < territory.GetLength(0); r++)
            {
                for (int c = 0; c < territory.GetLength(1); c++)
                {
                    if (territory[r, c])
                    {
                        IsometricSprite sprite = new IsometricSprite(_tile);
                        sprite.X = r * ROX + c * COX + 500;
                        sprite.Y = r * ROY + c * COY + 150;
                        _tiles.Add(sprite);
                    }
                }
            }
        }

        private bool[,] GenerateTerritoryLayout()
        {
            Random random = new Random();
            bool[,] layout = new bool[9, 9];

            // fill in the center for the piece tiles
            for (int r = 3; r < 6; r++)
            {
                for (int c = 3; c < 6; c++)
                {
                    layout[r, c] = true;
                }
            }
            
            // add randomized decoration
            for (int d = 0; d < 5; d++)
            {
                int baseRow = random.Next(3, 6);
                int deltaRows = random.Next(-5, 5);
                int startRow = Math.Min(baseRow, baseRow + deltaRows);
                int endRow = Math.Max(baseRow, baseRow + deltaRows);

                int baseCol = random.Next(3, 6);
                int deltaCols = random.Next(-5, 5);
                int startCol = Math.Min(baseCol, baseCol + deltaCols);
                int endCol = Math.Max(baseCol, baseCol + deltaCols);

                for (int r = startRow; r <= endRow; r++)
                {
                    for (int c = startCol; c <= endCol; c++)
                    {
                        if (r >= 0 && r < 9 && c >= 0 && c < 9)
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
                ShowTerritory();
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
            foreach (IsometricSprite sprite in _tiles)
            {
                _isoBatch.Draw(sprite);
            }
            _isoBatch.End();

            base.Draw(gameTime);
        }

        private void DrawMap()
        {
            _spriteBatch.Begin(SpriteBlendMode.AlphaBlend, SpriteSortMode.Texture, SaveStateMode.None);
            foreach (Territory t in _map.Territories)
            {
                foreach (Territory tt in t.Adjacent)
                {
                    float angle = (float)Math.Atan2(tt.Position.Y - t.Position.Y, tt.Position.X - t.Position.X);
                    float distance = Vector2.Distance(t.Position, tt.Position);
                    _spriteBatch.Draw(
                        _colourable,
                        t.Position + new Vector2(100, 100),
                        null,
                        Color.Black,
                        angle,
                        Vector2.Zero,
                        new Vector2(distance, 1),
                        SpriteEffects.None,
                        0f);
                }
                _spriteBatch.Draw(
                    _colourable,
                    t.Position + new Vector2(95, 95),
                    null,
                    GetPlayerColor(t.Owner),
                    0f,
                    Vector2.Zero,
                    new Vector2(10, 10),
                    SpriteEffects.None,
                    0f);
            }
            _spriteBatch.End();
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

        private Map _map;

        private SpriteBatch _spriteBatch;
        private IsometricBatch _isoBatch;
        private Texture2D _colourable;
        private Texture2D _tile;
        private Texture2D _piece;
        private List<IsometricSprite> _tiles = new List<IsometricSprite>();

        private bool _rWasDown;
    }
}
