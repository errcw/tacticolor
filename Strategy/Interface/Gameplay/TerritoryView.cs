using System;
using System.Collections.Generic;
using System.Linq;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;

using Strategy.Gameplay;
using Strategy.Library;
using Strategy.Library.Animation;
using Strategy.Library.Extensions;
using Strategy.Library.Sound;
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

            // build the attack sprites
            Point territoryPosition = context.IsoParams.GetPoint(territory.Location);

            _attackTex = context.Content.Load<Texture2D>("Images/TerritoryAttack");
            _defendTex = context.Content.Load<Texture2D>("Images/TerritoryDefend");

            _attackPartySprite = new ImageSprite(_attackTex);
            _attackPartySprite.Position = new Vector2(
                (int)(territoryPosition.X - _attackPartySprite.Size.X + 15f),
                (int)(territoryPosition.Y - _attackPartySprite.Size.Y / 2));
            _attackPartySprite.Color = Color.White;

            _attackRollSprite = new TextSprite(context.Content.Load<SpriteFont>("Fonts/TextSmallBold"), "1");
            _attackRollSprite.Origin = new Vector2(0, (int)(_attackRollSprite.Size.Y / 2));
            _attackRollSprite.Position = _attackPartySprite.Position + _attackRollSprite.Origin + new Vector2(_attackPartySprite.Size.X + 3, 1);
            _attackRollSprite.Color = Color.White;
            _attackRollSprite.Effect = TextSprite.TextEffect.Shadow;
            _attackRollSprite.EffectColor = new Color(30, 30, 30, 160);
            _attackRollSprite.EffectSize = 1;

            _attackSprite = new CompositeSprite(_attackPartySprite, _attackRollSprite);
            _attackSprite.Color = Color.Transparent;
            _attackSprite.Layer = 0f;
        }

        public void Update(float time)
        {
            Color selectionColor = IsSelected
                ? GetSelectionColor(_territory.Owner.Value)
                : _territory.Owner.GetTerritoryColor();
            if (_sprite.Color != selectionColor && _colorAnimation == null)
            {
                _colorAnimation = new ColorAnimation(_sprite, selectionColor, 0.1f, Interpolation.InterpolateColor(Easing.Uniform));
            }

            if (_colorAnimation != null)
            {
                if (!_colorAnimation.Update(time))
                {
                    _colorAnimation = null;
                }
            }
            if (_attackAnimation != null)
            {
                if (!_attackAnimation.Update(time))
                {
                    _attackAnimation = null;
                }
            }
        }

        public void Draw(IsometricView isoView)
        {
            isoView.Add(_sprite);
            isoView.Add(_attackSprite);
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
        /// Notifies this view that it participated in an attack.
        /// </summary>
        public void OnAttacked(bool wasAttacker, IEnumerable<int> pieceRolls, float showRollDelay, float hideRollDelay)
        {
            _attackPartySprite.Texture = wasAttacker ? _attackTex : _defendTex;
            _attackRollSprite.Text = "";

            List<IAnimation> animations = new List<IAnimation>();
            animations.Add(new CompositeAnimation(
                new ColorAnimation(_attackSprite, Color.White, 0.25f, Interpolation.InterpolateColor(Easing.Uniform)),
                new DelayAnimation(showRollDelay)));
            int sum = 0;
            foreach (int roll in pieceRolls)
            {
                sum += roll;
                animations.Add(new SequentialAnimation(
                    new ScaleAnimation(_attackRollSprite, Vector2.UnitX, 0.1f, Interpolation.InterpolateVector2(Easing.Uniform)),
                    new TextAnimation(_attackRollSprite, sum.ToString()),
                    new DelayAnimation(0.05f),
                    new ScaleAnimation(_attackRollSprite, Vector2.One, 0.1f, Interpolation.InterpolateVector2(Easing.Uniform))));
            }
            animations.Add(new DelayAnimation(hideRollDelay));
            animations.Add(new ColorAnimation(_attackSprite, Color.Transparent, 0.25f, Interpolation.InterpolateColor(Easing.Uniform)));

            _attackAnimation = new SequentialAnimation(animations.ToArray());
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

        private Color GetSelectionColor(PlayerId playerId)
        {
            switch (playerId)
            {
                case PlayerId.A: return new Color(249, 39, 155);
                case PlayerId.B: return new Color(112, 207, 255);
                case PlayerId.C: return new Color(66, 206, 119);
                case PlayerId.D: return new Color(255, 242, 147);
                default: throw new ArgumentException("Invalid player id " + playerId);
            }
        }

        private Territory _territory;
        private InterfaceContext _context;
        private PlayerId? _lastOwner;

        private Stack<Cell> _freeHolders;
        private Dictionary<PieceView, Cell> _usedHolders;

        private CompositeSprite _sprite;
        private IAnimation _colorAnimation;

        private ImageSprite _attackPartySprite;
        private TextSprite _attackRollSprite;
        private Texture2D _attackTex, _defendTex;
        private Sprite _attackSprite;
        private IAnimation _attackAnimation;
    }
}
