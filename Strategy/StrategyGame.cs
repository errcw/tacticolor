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
            Components.Add(new FPSOverlay(this));

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
            base.LoadContent();

            _tile = Content.Load<Texture2D>("TileSmall");
            _tileHolder = Content.Load<Texture2D>("TileSmallHolder");
            _piece = Content.Load<Texture2D>("PieceSmall");
            _conn = Content.Load<Texture2D>("Connection");

            _spriteBatch = new SpriteBatch(GraphicsDevice);
            _isoBatch = new IsometricBatch(_spriteBatch);
        }

        private void StartNewMatch()
        {
            _map = _generator.Generate(16, 4, 4, 10);
            _match = new Match(_map, _random);

            ShowMap(_map);

            _inputA = new LocalInput(PlayerId.A, _match, _context);
            _inputA.Controller = PlayerIndex.One;
            _inputViewA = new LocalInputView(_inputA, _context);
            _inputB = new LocalInput(PlayerId.B, _match, _context);
            _inputB.Controller = PlayerIndex.Two;
            _inputViewB = new LocalInputView(_inputB, _context);
        }

        private void ShowMap(Map map)
        {
            Rectangle r = CalculatePixelExtents(map);
            BASEX = (1280 - r.Width) / 2 - r.X;
            BASEY = (720 - r.Height) / 2 - r.Y;
            _context.IsoParams.OffsetX = BASEX;
            _context.IsoParams.OffsetY = BASEY;

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
            List<IsometricSprite> tileSprites = new List<IsometricSprite>(25);
            Color playerColor = GetPlayerColor(territory.Owner);

            foreach (Cell cell in territory.Area)
            {
                Texture2D spriteImage = IsHolder(territory, cell) ? _tileHolder : _tile;
                IsometricSprite tileSprite = new IsometricSprite(spriteImage);
                tileSprite.X = cell.Row * ROX + cell.Col * COX + BASEX;
                tileSprite.Y = cell.Row * ROY + cell.Col * COY + BASEY;
                tileSprite.Color = GetPlayerColor(territory.Owner);
                tileSprites.Add(tileSprite);
            }

            int ordinal = 0;
            foreach(Piece piece in territory.Pieces)
            {
                Cell offset = MapPieceOrdinalToOffset(ordinal++);
                Cell cell = new Cell(territory.Location.Row + offset.Row, territory.Location.Col + offset.Col);

                IsometricSprite pieceSprite = new IsometricSprite(_piece);
                pieceSprite.X = cell.Row * ROX + cell.Col * COX + BASEX;
                pieceSprite.Y = cell.Row * ROY + cell.Col * COY + BASEY;
                pieceSprite.Position += new Vector2(10, 10); // offset in tile
                pieceSprite.Origin = new Vector2(0, 14); // offset to bottom

                pieceSprite.Color = Interpolation.InterpolateColor(Easing.Uniform)(Color.White, playerColor, (float)piece.TimerValue / piece.TimerMax);

                tileSprites.Add(pieceSprite);
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

        private Cell MapPieceOrdinalToOffset(int ordinal)
        {
            switch (ordinal)
            {
                case 0: return new Cell(0, 0);
                case 1: return new Cell(-1, 0);
                case 2: return new Cell(1, 0);
                case 3: return new Cell(0, -1);
                case 4: return new Cell(0, 1);
                case 5: return new Cell(1, -1);
                case 6: return new Cell(-1, 1);
                case 7: return new Cell(1, 1);
                case 8: return new Cell(-1, -1);
                default: throw new ArgumentException("Invalid piece ordinal " + ordinal);
            }
        }

        private List<IsometricSprite> ShowConnection(Territory a, Territory b)
        {
            Cell closestA = a.Area.First(), closestB = b.Area.First();
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
                Exit();
            }

            _match.Update(gameTime.GetElapsedMilliseconds());

            float time = gameTime.GetElapsedSeconds();
            _inputA.Update(time);
            _inputViewA.Update(time);
            _inputB.Update(time);
            _inputViewB.Update(time);

            ShowMap(_map); // brute force the new map (oh so ugly)
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
            _inputViewA.Draw(_isoBatch);
            _inputViewB.Draw(_isoBatch);
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

        private Random _random;
        private MapGenerator _generator;
        private Match _match;
        private Map _map;
        private InterfaceContext _context;

        private LocalInput _inputA;
        private LocalInputView _inputViewA;
        private LocalInput _inputB;
        private LocalInputView _inputViewB;

        private SpriteBatch _spriteBatch;
        private IsometricBatch _isoBatch;
        private Texture2D _tile;
        private Texture2D _tileHolder;
        private Texture2D _piece;
        private Texture2D _conn;
        private List<IsometricSprite> _sprites = new List<IsometricSprite>();
        private List<IsometricSprite> _spritesLow = new List<IsometricSprite>();
    }
}
