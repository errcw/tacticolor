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
        }

        protected override void Initialize()
        {
            base.Initialize();

            _generator = new MapGenerator();
            ShowMap(_generator.Generate(20, 5));
        }

        protected override void LoadContent()
        {
            base.LoadContent();

            _tile = Content.Load<Texture2D>("TileSmall");
            _tileHolder = Content.Load<Texture2D>("TileSmallHolder");
            _piece = Content.Load<Texture2D>("PieceSmall");
            _conn = Content.Load<Texture2D>("Connection");

            _spriteBatch = new SpriteBatch(GraphicsDevice);
            _isoBatch = new IsometricBatch(_spriteBatch);
        }

        private void ShowMap(Map map)
        {
            Rectangle r = CalculatePixelExtents(map);
            BASEX = (1280 - r.Width) / 2 - r.X;
            BASEY = (720 - r.Height) / 2 - r.Y;

            _sprites.Clear();
            _spritesLow.Clear();
            foreach (Territory territory in map.Territories)
            {
                _sprites.AddRange(ShowTerritory(territory));
                foreach (Territory other in territory.Neighbors)
                {
                    _spritesLow.AddRange(ShowConnection(territory, other));
                }
            }
        }

        const int ROX = 20;
        const int ROY = 10;
        const int COX = 20;
        const int COY = -10;
        int BASEX = 0;
        int BASEY = 300;

        private Rectangle CalculatePixelExtents(Map map)
        {
            int minX = int.MaxValue, minY = int.MaxValue;
            int maxX = int.MinValue, maxY = int.MinValue;
            foreach (Territory territory in map.Territories)
            {
                foreach (Cell cell in territory.Area)
                {
                    int x = cell.Row * ROX + cell.Col * COX;
                    int y = cell.Row * ROY + cell.Col * COY;
                    if (x < minX)
                    {
                        minX = x;
                    }
                    if (x > maxX)
                    {
                        maxX = x;
                    }
                    if (y < minY)
                    {
                        minY = y;
                    }
                    if (y > maxY)
                    {
                        maxY = y;
                    }
                }
            }
            return new Rectangle(minX, minY, maxX - minX + _tile.Width, maxY - minY + _tile.Height);
        }

        private List<IsometricSprite> ShowTerritory(Territory territory)
        {
            int piecesToShow = territory.Occupancy;
            List<IsometricSprite> tileSprites = new List<IsometricSprite>(25);

            foreach (Cell cell in territory.Area)
            {
                bool isHolder = IsHolder(territory, cell);

                Texture2D spriteImage = isHolder ? _tileHolder : _tile;
                IsometricSprite tileSprite = new IsometricSprite(spriteImage);
                tileSprite.X = cell.Row * ROX + cell.Col * COX + BASEX;
                tileSprite.Y = cell.Row * ROY + cell.Col * COY + BASEY;
                tileSprite.Color = GetPlayerColor(territory.Owner);
                tileSprites.Add(tileSprite);

                if (isHolder && piecesToShow > 0)
                {
                    IsometricSprite pieceSprite = new IsometricSprite(_piece);
                    pieceSprite.X = cell.Row * ROX + cell.Col * COX + BASEX;
                    pieceSprite.Y = cell.Row * ROY + cell.Col * COY + BASEY;
                    pieceSprite.Position += new Vector2(10, 10); // offset to tile
                    pieceSprite.Origin = new Vector2(0, 14); // base
                    tileSprites.Add(pieceSprite);

                    piecesToShow -= 1;
                }
            }

            return tileSprites;
        }

        private bool IsHolder(Territory territory, Cell cell)
        {
            int dr = cell.Row - territory.Location.Row;
            int dc = cell.Col - territory.Location.Col;
            if (territory.Capacity == 9 && Math.Abs(dr) <= 1 && Math.Abs(dc) <= 1)
            {
                return true;
            }
            if (territory.Capacity == 7 && (dc == -1 && dr == 1 || dc == 1 && dr == -1))
            {
                return true;
            }
            if (dc == 0 && Math.Abs(dr) <= 1 || dr == 0 && Math.Abs(dc) <= 1)
            {
                return true;
            }
            return false;
        }

        private List<IsometricSprite> ShowConnection(Territory a, Territory b)
        {
            Cell closestA = a.Area[0], closestB = b.Area[0];
            int closestDist2 = int.MaxValue;

            foreach (Cell ca in a.Area)
            {
                foreach (Cell cb in b.Area)
                {
                    int d2 = (ca.Row - cb.Row) * (ca.Row - cb.Row) + (ca.Col - cb.Col) * (ca.Col - cb.Col);
                    if (d2 < closestDist2)
                    {
                        closestA = ca;
                        closestB = cb;
                        closestDist2 = d2;
                    }
                }
            }

            List<IsometricSprite> sprites = new List<IsometricSprite>(25);
            foreach (Point p in BresenhamIterator.GetPointsOnLine(closestA.Row, closestA.Col, closestB.Row, closestB.Col))
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
                ShowMap(_generator.Generate(20, 2));
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

        private MapGenerator _generator;

        private SpriteBatch _spriteBatch;
        private IsometricBatch _isoBatch;
        private Texture2D _tile;
        private Texture2D _tileHolder;
        private Texture2D _piece;
        private Texture2D _conn;
        private List<IsometricSprite> _sprites = new List<IsometricSprite>();
        private List<IsometricSprite> _spritesLow = new List<IsometricSprite>();

        private bool _rWasDown;
    }
}
