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

            _input = new PlayerInput(this);
            _input.Controller = PlayerIndex.One;
            Components.Add(_input);
        }

        protected override void Initialize()
        {
            base.Initialize();

            _generator = new MapGenerator();
            _map = _generator.Generate(16, 4, 10);
            ShowMap(_map);
            _selected = _map.Territories.First();
            Navigate();
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

            _cursor = new IsometricSprite(_piece);
            _cursor.Color = Color.SlateGray;
            _cursor.Origin = new Vector2(0, 14);
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
                    pieceSprite.Position += new Vector2(10, 10); // offset in tile
                    pieceSprite.Origin = new Vector2(0, 14); // offset to bottom
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

        private void Navigate()
        {
            float THRESHOLD = MathHelper.ToRadians(90);
            if (_input.Move.Pressed)
            {
                Vector2 direction = _input.MoveDirection.Position;
                direction.Y = -direction.Y;

                Territory newSelected = null;

                float minAngle = float.MaxValue;

                Vector2 curLoc = new Vector2(
                    _selected.Location.Row * ROX + _selected.Location.Col * COX + BASEX,
                    _selected.Location.Row * ROY + _selected.Location.Col * COY + BASEY);

                foreach (Territory other in _selected.Neighbors)
                {
                    Vector2 otherLoc = new Vector2(
                        other.Location.Row * ROX + other.Location.Col * COX + BASEX,
                        other.Location.Row * ROY + other.Location.Col * COY + BASEY);
                    Vector2 toOtherLoc = otherLoc - curLoc;

                    float dot = Vector2.Dot(direction, toOtherLoc);
                    float crossMag = direction.X * toOtherLoc.Y - direction.Y * toOtherLoc.X;
                    float angle = (float)Math.Abs(Math.Atan2(crossMag, dot));

                    if (angle < THRESHOLD && angle < minAngle)
                    {
                        minAngle = angle;
                        newSelected = other;
                    }
                }

                if (newSelected != null)
                {
                    _selected = newSelected;

                    Cell ccell = _selected.Area[0];
                    _cursor.X = ccell.Row * ROX + ccell.Col * COX + BASEX;
                    _cursor.Y = ccell.Row * ROY + ccell.Col * COY + BASEY;
                    _cursor.Position += new Vector2(10, 10); // offset in tile
                }
            }
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
            if (_input.Debug.Pressed)
            {
                ShowMap(_generator.Generate(20, 1, 2));
            }
            Navigate();
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
            _isoBatch.Draw(_cursor);
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

        private Territory _selected;
        private IsometricSprite _cursor;

        private MapGenerator _generator;
        private Map _map;

        private PlayerInput _input;

        private SpriteBatch _spriteBatch;
        private IsometricBatch _isoBatch;
        private Texture2D _tile;
        private Texture2D _tileHolder;
        private Texture2D _piece;
        private Texture2D _conn;
        private List<IsometricSprite> _sprites = new List<IsometricSprite>();
        private List<IsometricSprite> _spritesLow = new List<IsometricSprite>();
    }

    public class PlayerInput : Input
    {
        public readonly ControlState Move = new ControlState() { RepeatEnabled = true };
        public readonly ControlPosition MoveDirection = new ControlPosition();

        public readonly ControlState Debug = new ControlState();

        public PlayerInput(Game game) : base(game)
        {
            Register(Move, (state) => state.ThumbSticks.Left.LengthSquared() >= MoveTolerance);
            Register(MoveDirection, Polling.LeftThumbStick);
            Register(Debug, Polling.All(Polling.One(Buttons.LeftShoulder), Polling.One(Buttons.RightShoulder)));
        }

        /// <summary>
        /// Polls for the controller with the Start button pressed.
        /// </summary>
        /// <returns>True if a controller was found; otherwise, false.</returns>
        public bool FindActiveController()
        {
            return FindActiveController(Polling.One(Buttons.Start));
        }

        private const float MoveTolerance = 0.5f * 0.5f;
    }
}
