using System;
using System.Collections.Generic;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using Strategy.Gameplay;
using Strategy.Library;
using Strategy.Library.Sprite;

namespace Strategy.Interface
{
    /// <summary>
    /// Shows a territory.
    /// </summary>
    public class TerritoryView
    {
        public TerritoryView(Territory territory, InterfaceContext context)
        {
            _territory = territory;
            _context = context;

            _lastOwner = _territory.Owner;

            InitHolders();

            Texture2D tile = context.Content.Load<Texture2D>("Tile");
            Texture2D tileHolder = context.Content.Load<Texture2D>("TileHolder");
            Color color = GetPlayerColor(territory.Owner);

            _sprites = new Sprite[_territory.Area.Count];
            int s = 0;
            foreach (Cell cell in territory.Area)
            {
                Texture2D spriteImage = IsHolder(cell) ? tileHolder : tile;
                Point spritePosition = context.IsoParams.GetPoint(cell);
                _sprites[s] = new ImageSprite(spriteImage);
                _sprites[s].X = spritePosition.X;
                _sprites[s].Y = spritePosition.Y;
                _sprites[s].Color = color;
                s += 1;
            }

        }

        public void Update(float time)
        {


            // update the color animations, if any
            if (_colorAnims != null)
            {
                bool running = true;
                for (int i = 0; i < _colorAnims.Length; i++)
                {
                    running &= _colorAnims[i].Update(time);
                }
                if (!running)
                {
                    _colorAnims = null;
                }
            }
        }

        public void Draw(IsometricBatch isoBatch)
        {
            foreach (Sprite sprite in _sprites)
            {
                isoBatch.Draw(sprite);
            }
        }

        /// <summary>
        /// Notifies this view that a piece was added to the territory.
        /// </summary>
        public Cell PieceAdded(Piece piece)
        {
            Cell holder = _freeHolders.Pop();
            _usedHolders.Add(piece, holder);
            return new Cell(
                _territory.Location.Row + holder.Row,
                _territory.Location.Col + holder.Col);
        }

        /// <summary>
        /// Notifies this view that a piece was removed from the territory.
        /// </summary>
        public void PieceRemoved(Piece piece)
        {
            Cell holder = _usedHolders[piece];
            _usedHolders.Remove(piece);
            _freeHolders.Push(holder);
        }

        /// <summary>
        /// Notifies this view that the territory might have changed owners.
        /// </summary>
        public void MaybeChangedOwners(float delay)
        {
            // detect when the territory changes owners
            if (_territory.Owner != _lastOwner)
            {
                Color newColor = GetPlayerColor(_territory.Owner);
                _colorAnims = new IAnimation[_sprites.Length];
                for (int i = 0; i < _sprites.Length; i++)
                {
                    _colorAnims[i] = new SequentialAnimation(
                        new DelayAnimation(delay),
                        new ColorAnimation(_sprites[i], newColor, 1f, Interpolation.InterpolateColor(Easing.Uniform)));
                }

                _lastOwner = _territory.Owner;
            }
        }

        /// <summary>
        /// Checks if the given cell acts as a piece holder for the territory.
        /// </summary>
        private bool IsHolder(Cell cell)
        {
            int dr = cell.Row - _territory.Location.Row;
            int dc = cell.Col - _territory.Location.Col;
            if (_territory.Capacity == 9 && Math.Abs(dr) <= 1 && Math.Abs(dc) <= 1)
            {
                return true;
            }
            if (_territory.Capacity == 7 && (dc == -1 && dr == 1 || dc == 1 && dr == -1))
            {
                return true;
            }
            if (dc == 0 && Math.Abs(dr) <= 1 || dr == 0 && Math.Abs(dc) <= 1)
            {
                return true;
            }
            return false;
        }

        /// <summary>
        /// Maps a piece number to a position.
        /// </summary>
        private void InitHolders()
        {
            _usedHolders = new Dictionary<Piece, Cell>(9);
            _freeHolders = new Stack<Cell>(9);
            _freeHolders.Push(new Cell(-1, -1));
            _freeHolders.Push(new Cell(1, 1));
            _freeHolders.Push(new Cell(-1, 1));
            _freeHolders.Push(new Cell(1, -1));
            _freeHolders.Push(new Cell(0, 1));
            _freeHolders.Push(new Cell(0, -1));
            _freeHolders.Push(new Cell(1, 0));
            _freeHolders.Push(new Cell(-1, 0));
            _freeHolders.Push(new Cell(0, 0));
        }

        /// <summary>
        /// Returns the color of the given player.
        /// </summary>
        private Color GetPlayerColor(PlayerId? player)
        {
            switch (player)
            {
                case PlayerId.A: return new Color(222, 35, 136);
                case PlayerId.B: return new Color(33, 157, 221);
                case PlayerId.C: return new Color(0, 168, 67);
                case PlayerId.D: return new Color(251, 223, 0);
                case null: return Color.White;
                default: throw new ArgumentException("Invalid player id " + player);
            }
        }

        private Territory _territory;
        private InterfaceContext _context;
        private PlayerId? _lastOwner;

        private Sprite[] _sprites;

        private Stack<Cell> _freeHolders;
        private Dictionary<Piece, Cell> _usedHolders;

        private IAnimation[] _colorAnims;
    }
}
