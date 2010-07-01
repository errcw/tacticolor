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

            _inputStateA = new LocalInputState();
            _inputA = new LocalInput(this);
            _inputA.Controller = PlayerIndex.One;
            Components.Add(_inputA);
            _inputStateB = new LocalInputState();
            _inputB = new LocalInput(this);
            _inputB.Controller = PlayerIndex.Two;
            Components.Add(_inputB);
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

            _cursorA = new IsometricSprite(_piece);
            _cursorA.Color = Color.SlateGray;
            _cursorA.Origin = new Vector2(0, 14);
            _cursorB = new IsometricSprite(_piece);
            _cursorB.Color = Color.SaddleBrown;
            _cursorB.Origin = new Vector2(0, 14);
        }

        private void StartNewMatch()
        {
            _map = _generator.Generate(16, 4, 4, 10);
            _match = new Match(_map, _random);

            ShowMap(_map);

            _inputStateA.Hovered = _map.Territories.First();
            _inputStateB.Hovered = _map.Territories.First();
            ShowSelected();

            _pendingAction = false;
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

        private void HandleInput(PlayerId player, LocalInput input, LocalInputState state)
        {
            if (input.Action.Pressed)
            {
                if (state.ActionPending)
                {
                    if (_match.CanMove(player, state.Selected, state.Hovered))
                    {
                        _match.Move(state.Selected, state.Hovered);
                        state.ActionPending = false;
                    }
                    else if (_match.CanAttack(player, state.Selected, state.Hovered))
                    {
                        _match.Attack(state.Selected, state.Hovered);
                        state.ActionPending = false;
                    }
                }
                else
                {
                    state.Selected = state.Hovered;
                    state.ActionPending = true;
                }
            }
            else if (input.Cancel.Pressed)
            {
                state.ActionPending = false;
                state.Selected = null;
            }
            else if (input.Place.Pressed)
            {
                if (_match.CanPlacePiece(player, state.Hovered))
                {
                    _match.PlacePiece(state.Hovered);
                }
            }
            else if (input.Move.Pressed)
            {
                const float THRESHOLD = MathHelper.PiOver2;

                Vector2 direction = input.MoveDirection.Position;
                direction.Y = -direction.Y;

                Territory hovered = state.Hovered;
                Territory newHovered = null;

                float minAngle = float.MaxValue;

                Vector2 curLoc = new Vector2(
                    hovered.Location.Row * ROX + hovered.Location.Col * COX + BASEX,
                    hovered.Location.Row * ROY + hovered.Location.Col * COY + BASEY);

                foreach (Territory other in hovered.Neighbors)
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
                        newHovered = other;
                    }
                }

                if (newHovered != null)
                {
                    state.Hovered = newHovered;
                    ShowSelected();
                }
            }
            else if (input.Debug.Pressed)
            {
                StartNewMatch();
            }
        }

        private void ShowSelected()
        {
            Cell cella = _inputStateA.Hovered.Area.First();
            _cursorA.X = cella.Row * ROX + cella.Col * COX + BASEX;
            _cursorA.Y = cella.Row * ROY + cella.Col * COY + BASEY;
            _cursorA.Position += new Vector2(10, 10); // offset in tile

            Cell cellb = _inputStateB.Hovered.Area.ElementAt(1);
            _cursorB.X = cellb.Row * ROX + cellb.Col * COX + BASEX;
            _cursorB.Y = cellb.Row * ROY + cellb.Col * COY + BASEY;
            _cursorB.Position += new Vector2(10, 10); // offset in tile
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
            _match.Update(gameTime.GetElapsedSeconds());
            HandleInput(PlayerId.A, _inputA, _inputStateA);
            HandleInput(PlayerId.B, _inputB, _inputStateB);
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
            _isoBatch.Draw(_cursorA);
            _isoBatch.Draw(_cursorB);
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

        private LocalInput _inputA;
        private LocalInputState _inputStateA;
        private IsometricSprite _cursorA;
        private LocalInput _inputB;
        private LocalInputState _inputStateB;
        private IsometricSprite _cursorB;

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
