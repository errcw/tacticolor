using System;
using System.Collections.Generic;
using System.Linq;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using Strategy.Gameplay;
using Strategy.Library;
using Strategy.Library.Extensions;
using Strategy.Library.Sprite;

namespace Strategy.Interface.Gameplay
{
    /// <summary>
    /// Shows a territory.
    /// </summary>
    public class TerritoryView
    {
        /// <summary>
        /// If a player has selected this territory.
        /// </summary>
        public bool IsSelected { get; set; }

        public TerritoryView(Territory territory, InterfaceContext context)
        {
            _territory = territory;
            _context = context;

            _lastOwner = _territory.Owner;

            CreateHolders();

            Texture2D tile = context.Content.Load<Texture2D>("Images/Tile");
            Texture2D tileHolder = context.Content.Load<Texture2D>("Images/TileHolder");

            // build the tiles in the territory
            IsometricView isoView = new IsometricView();
            foreach (Cell cell in territory.Area)
            {
                Texture2D spriteImage = IsHolder(cell) ? tileHolder : tile;
                Point spritePosition = context.IsoParams.GetPoint(cell);
                Sprite sprite = new ImageSprite(spriteImage);
                sprite.X = spritePosition.X;
                sprite.Y = spritePosition.Y;
                isoView.Add(sprite);
            }

            // build the territory sprite using the isometric draw order
            _sprite = new CompositeSprite();
            _sprite.Color = territory.Owner.GetTerritoryColor();
            foreach (Sprite sprite in isoView.GetSpritesInDrawOrder())
            {
                _sprite.Add(sprite);
            }
        }

        public void Update(float time)
        {
            _sprite.Color = _territory.Cooldown > 0 ? Color.Gray : Color.White;
            if (_colorAnimation != null)
            {
                if (!_colorAnimation.Update(time))
                {
                    _colorAnimation = null;
                }
            }
        }

        public void Draw(IsometricView isoView)
        {
            isoView.Add(_sprite);
        }

        /// <summary>
        /// Notifies this view that a piece was added to the territory.
        /// </summary>
        public Cell PieceAdded(PieceView piece)
        {
            piece.SetTerritory(this);
            Cell holder = _freeHolders.Pop();
            _usedHolders.Add(piece, holder);
            return new Cell(
                _territory.Location.Row + holder.Row,
                _territory.Location.Col + holder.Col);
        }

        /// <summary>
        /// Notifies this view that a piece was removed from the territory.
        /// </summary>
        public void PieceRemoved(PieceView piece)
        {
            Cell holder = _usedHolders[piece];
            _usedHolders.Remove(piece);
            _freeHolders.Push(holder);
        }

        /// <summary>
        /// Notifies this view that the territory might have changed owners.
        /// </summary>
        /// <param name="delay">The delay to apply before showing the ownership change.</param>
        public void MaybeChangedOwners(float delay)
        {
            if (_territory.Owner != _lastOwner)
            {
                Color newColor = _territory.Owner.GetTerritoryColor();
                _colorAnimation = new SequentialAnimation(
                    new DelayAnimation(delay),
                    new ColorAnimation(_sprite, newColor, 1f, Interpolation.InterpolateColor(Easing.Uniform)));

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

        private void CreateHolders()
        {
            _usedHolders = new Dictionary<PieceView, Cell>(9);
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

        private Territory _territory;
        private InterfaceContext _context;
        private PlayerId? _lastOwner;

        private Stack<Cell> _freeHolders;
        private Dictionary<PieceView, Cell> _usedHolders;

        private CompositeSprite _sprite;
        private IAnimation _colorAnimation;
    }
}
