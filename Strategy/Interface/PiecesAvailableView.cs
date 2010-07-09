﻿using System;
using System.Collections.Generic;
using System.Linq;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using Strategy.Gameplay;
using Strategy.Library;

namespace Strategy.Interface
{
    public class PiecesAvailableView
    {
        public PiecesAvailableView(Match match, PlayerId player, InterfaceContext context)
        {
            _match = match;
            _match.PiecePlaced += OnPiecePlaced;
            _player = player;
            _context = context;

            Texture2D pieceSprite = context.Content.Load<Texture2D>("Piece");

            _unused = new Stack<IsometricSprite>(_match.MaxPiecesAvailable);
            _created = new Stack<IsometricSprite>(_match.MaxPiecesAvailable);
            for (int p = 0; p < _match.MaxPiecesAvailable; p++)
            {
                IsometricSprite sprite = new IsometricSprite(pieceSprite);
                sprite.Y = BasePosition.Y + PlayerSpacing.Y * (int)_player;
                _unused.Push(sprite);
            }

            SetUpCreatingSprite();

            _lastAvailable = 0;
        }

        public void Update(float time)
        {
            // check if a new piece was created this frame
            int available = _match.PiecesAvailable[(int)_player];
            if (available != _lastAvailable)
            {
                if (available > _lastAvailable)
                {
                    OnPieceCreated();
                }
                _lastAvailable = available;
            }

            if (_creatingSprite != null)
            {
                float progress = _match.PieceCreationProgress[(int)_player];
                _creatingSprite.Color = new Color(_creatingSprite.Color, (byte)(progress * 255));
                _creatingSprite.X = Interpolation.InterpolateFloat(Easing.Uniform)(_creatingSprite.X, _creatingTargetX, 8f * time);
            }
        }

        public void Draw(SpriteBatch spriteBatch)
        {
            foreach(IsometricSprite sprite in _created)
            {
                sprite.Draw(spriteBatch);
            }
            if (_creatingSprite != null)
            {
                _creatingSprite.Draw(spriteBatch);
            }
        }

        /// <summary>
        /// Notifies this view that a piece was created.
        /// </summary>
        private void OnPieceCreated()
        {
            _creatingSprite.Color = Color.White;
            _creatingSprite.X = _creatingTargetX;
            _created.Push(_creatingSprite);

            _creatingSprite = null;
            if (_match.PiecesAvailable[(int)_player] < _match.MaxPiecesAvailable)
            {
                SetUpCreatingSprite();
            }
        }

        /// <summary>
        /// Notifies this view that a piece was placed and might need to be removed.
        /// </summary>
        private void OnPiecePlaced(object match, PiecePlacedEventArgs args)
        {
            if (args.Location.Owner == _player)
            {
                // hide the old sprite
                IsometricSprite used = _created.Pop();
                used.Color = Color.TransparentWhite;
                _unused.Push(used);

                if (_creatingSprite != null)
                {
                    // slide into the now-vacant position
                    _creatingTargetX = BasePosition.X + _match.PiecesAvailable[(int)_player] * PieceSpacing.X;
                }
                else
                {
                    // made room for a new piece
                    SetUpCreatingSprite();
                }
            }
        }

        /// <summary>
        /// Grabs and initializes a new creating sprite.
        /// </summary>
        private void SetUpCreatingSprite()
        {
            _creatingSprite = _unused.Pop();
            _creatingSprite.Color = Color.TransparentWhite;
            _creatingSprite.X = BasePosition.X + _match.PiecesAvailable[(int)_player] * PieceSpacing.X;
            _creatingTargetX = _creatingSprite.X;
        }

        private Match _match;
        private PlayerId _player;
        private InterfaceContext _context;

        private int _lastAvailable;

        private Stack<IsometricSprite> _unused;
        private Stack<IsometricSprite> _created;
        private IsometricSprite _creatingSprite;
        private float _creatingTargetX;

        private readonly Vector2 BasePosition = new Vector2(50, 50);
        private readonly Vector2 PlayerSpacing = new Vector2(0, 50);
        private readonly Vector2 PieceSpacing = new Vector2(30, 0);
    }
}
