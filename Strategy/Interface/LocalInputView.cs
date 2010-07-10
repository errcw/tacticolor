﻿using System;
using System.Linq;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using Strategy.Gameplay;
using Strategy.Library.Sprite;

namespace Strategy.Interface
{
    /// <summary>
    /// Shows the input cursor.
    /// </summary>
    public class LocalInputView
    {
        public LocalInputView(LocalInput input, InterfaceContext context)
        {
            _input = input;
            _context = context;

            _input.HoveredChanged += OnHoveredChanged;
            _input.SelectedChanged += OnSelectedChanged;

            Texture2D cursorTex = context.Content.Load<Texture2D>("Cursor");

            _cursorHover = new ImageSprite(cursorTex);
            _cursorHover.Color = GetPlayerColor(input.Player);
            _cursorHover.Origin = new Vector2(0, 14);

            _cursorSelect = new ImageSprite(cursorTex);
            _cursorSelect.Color = new Color(GetPlayerColor(input.Player), 128);
            _cursorSelect.Origin = new Vector2(0, 14);

            // fake the events to show the initial state
            OnHoveredChanged(null, EventArgs.Empty);
            OnSelectedChanged(null, EventArgs.Empty);
        }

        public void Update(float time)
        {
        }

        public void Draw(IsometricBatch spriteBatch)
        {
            spriteBatch.Draw(_cursorHover);
            if (_showSelect)
            {
                spriteBatch.Draw(_cursorSelect);
            }
        }

        private void OnHoveredChanged(object input, EventArgs args)
        {
            Cell cell = ChooseCell(_input.Hovered);
            _cursorHover.Position = GetPosition(cell);

            _showSelect = (_input.Selected != null && _input.Hovered != _input.Selected);
        }

        private void OnSelectedChanged(object input, EventArgs args)
        {
            // only show the selection cursor once the hover changes
            _showSelect = false;

            if (_input.Selected != null)
            {
                Cell cell = ChooseCell(_input.Selected);
                _cursorSelect.Position = GetPosition(cell);
            }
        }

        /// <summary>
        /// Chooses the cell on which to place the cursor.
        /// </summary>
        private Cell ChooseCell(Territory territory)
        {
            int playerIndex = (int)_input.Player;
            int cellIndex = 0;
            foreach (Cell cell in territory.Area)
            {
                int dr = cell.Row - territory.Location.Row;
                int dc = cell.Col - territory.Location.Col;
                if (Math.Abs(dr) <= 1 && Math.Abs(dc) <= 1)
                {
                    continue; // piece placement
                }
                if (cellIndex == playerIndex)
                {
                    return cell;
                }
                cellIndex += 1;
            }
            // should never reach here
            return territory.Area.First();
        }

        /// <summary>
        /// Returns the pixel position mapped for the given tile.
        /// </summary>
        private Vector2 GetPosition(Cell cell)
        {
            Point point = _context.IsoParams.GetPoint(cell);
            Vector2 position = new Vector2(point.X, point.Y);
            position += new Vector2(9, 9); // offset in tile
            return position;
        }

        /// <summary>
        /// Returns the color of the given player.
        /// </summary>
        private Color GetPlayerColor(PlayerId player)
        {
            switch (player)
            {
                case PlayerId.A: return new Color(222, 35, 136);
                case PlayerId.B: return new Color(33, 157, 221);
                case PlayerId.C: return new Color(0, 168, 67);
                case PlayerId.D: return new Color(251, 223, 0);
                default: throw new ArgumentException("Invalid player id " + player);
            }
        }

        private LocalInput _input;
        private InterfaceContext _context;

        private Sprite _cursorHover;
        private Sprite _cursorSelect;
        private bool _showSelect;
    }
}
