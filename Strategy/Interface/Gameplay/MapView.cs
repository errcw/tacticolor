﻿using System;
using System.Collections.Generic;
using System.Linq;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;

using Strategy.Gameplay;
using Strategy.Library;
using Strategy.Library.Extensions;

namespace Strategy.Interface.Gameplay
{
    /// <summary>
    /// Shows a map.
    /// </summary>
    public class MapView
    {
        public MapView(Match match, InterfaceContext context)
        {
            _map = match.Map;
            _context = context;

            match.PiecePlaced += OnPiecePlaced;
            match.PiecesMoved += OnPiecesMoved;
            match.TerritoryAttacked += OnTerritoryAttacked;

            _context.IsoParams.OffsetX = _context.IsoParams.OffsetY = 0; // reset the values
            Rectangle extents = CalculatePixelExtents();
            _context.IsoParams.OffsetX = (1280 - extents.Width) / 2 - extents.X;
            _context.IsoParams.OffsetY = (720 - extents.Height) / 2 - extents.Y;

            _territoryViews = new Dictionary<Territory, TerritoryView>(_map.Territories.Count);
            _pieceViews = new Dictionary<Piece, PieceView>(_map.Territories.Count * 9);
            _connectionViews = new List<ConnectionView>(_map.Territories.Count * 3);
            foreach (Territory territory in _map.Territories)
            {
                TerritoryView territoryView = new TerritoryView(territory, _context);
                _territoryViews.Add(territory, territoryView);
                foreach (Piece piece in territory.Pieces)
                {
                    PieceView pieceView = new PieceView(piece, _context);
                    pieceView.OnPlaced(territoryView.PieceAdded(pieceView), false);
                    _pieceViews.Add(piece, pieceView);
                }
                foreach (Territory neighbor in territory.Neighbors)
                {
                    ConnectionView connectionView = new ConnectionView(territory, neighbor, _context);
                    _connectionViews.Add(connectionView);
                }
            }
            _removedPieces = new List<PieceView>(16);

            _moveEffect = context.Content.Load<SoundEffect>("Sounds/OtherMove");
            _placeEffect = context.Content.Load<SoundEffect>("Sounds/OtherPlace");
            _attackEffect = context.Content.Load<SoundEffect>("Sounds/OtherAttack");
        }

        /// <summary>
        /// Updates the view of the map.
        /// </summary>
        /// <param name="time">The elapsed time, in seconds, since the last update.</param>
        public void Update(float time)
        {
            _territoryViews.Values.ForEach(view => view.Update(time));
            _pieceViews.Values.ForEach(view => view.Update(time));

            _removedPieces.ForEach(view => view.Update(time));
            _removedPieces = _removedPieces.Where(view => view.IsVisible).ToList();
        }

        /// <summary>
        /// Draws all the elements of the map.
        /// </summary>
        public void Draw(IsometricView isoView)
        {
            _connectionViews.ForEach(view => view.Draw(isoView));
            _territoryViews.Values.ForEach(view => view.Draw(isoView));
            _pieceViews.Values.ForEach(view => view.Draw(isoView));
            _removedPieces.ForEach(view => view.Draw(isoView));
        }

        /// <summary>
        /// Notifies this view that a local selection changed.
        /// </summary>
        public void ShowSelectionChanged(Territory previousSelection, Territory currentSelection)
        {
            if (previousSelection != null)
            {
                _territoryViews[previousSelection].IsSelected = false;
            }
            if (currentSelection != null)
            {
                _territoryViews[currentSelection].IsSelected = true;
            }
        }

        /// <summary>
        /// Notifies this view that a piece was placed on the map.
        /// </summary>
        private void OnPiecePlaced(object match, PiecePlacedEventArgs args)
        {
            TerritoryView territoryView = _territoryViews[args.Location];
            PieceView pieceView = new PieceView(args.Piece, _context);
            pieceView.OnPlaced(territoryView.PieceAdded(pieceView), true);
            _pieceViews.Add(args.Piece, pieceView);

            if (!IsLocalHumanPlayer(args.Location.Owner.Value))
            {
                _placeEffect.Play();
            }
        }

        /// <summary>
        /// Notifies this view that pieces were moved on the map.
        /// </summary>
        private void OnPiecesMoved(object match, PiecesMovedEventArgs args)
        {
            TerritoryView sourceView = _territoryViews[args.Source];
            TerritoryView destinationView = _territoryViews[args.Destination];
            foreach (Piece piece in args.Pieces)
            {
                PieceView pieceView = _pieceViews[piece];
                sourceView.PieceRemoved(pieceView);
                Cell cell = destinationView.PieceAdded(pieceView);
                pieceView.OnMoved(cell);
            }
            destinationView.MaybeChangedOwners(0f);

            if (!IsLocalHumanPlayer(args.Source.Owner.Value))
            {
                _moveEffect.Play();
            }
        }

        /// <summary>
        /// Notifies this view that a territory was attacked on the map.
        /// </summary>
        private void OnTerritoryAttacked(object match, TerritoryAttackedEventArgs args)
        {
            TerritoryView attackerView = _territoryViews[args.Attacker];
            TerritoryView defenderView = _territoryViews[args.Defender];

            const float PerPieceTime = 0.25f;
            float delay = 0f;
            float totalDelay = (args.Attackers.Count + args.Defenders.Count) * PerPieceTime + 0.25f;

            // handle the defenders
            delay = args.Attackers.Count * PerPieceTime + 0.4f;
            foreach (PieceAttackData data in args.Defenders)
            {
                PieceView pieceView = _pieceViews[data.Piece];
                if (!data.Survived)
                {
                    defenderView.PieceRemoved(pieceView);
                    _pieceViews.Remove(data.Piece);
                    _removedPieces.Add(pieceView);
                }
                pieceView.OnAttacked(data.Survived, null, delay, totalDelay - delay);
                delay += PerPieceTime;
            }
            defenderView.OnAttacked(false, args.Defenders.Select(d => d.Roll), args.Attackers.Count * PerPieceTime + 0.25f, 1f);
            defenderView.MaybeChangedOwners(totalDelay + 0.25f);

            // handle the attackers
            delay = 0.4f;
            foreach (PieceAttackData data in args.Attackers)
            {
                PieceView pieceView = _pieceViews[data.Piece];
                Cell? destination = null;
                if (data.Survived && data.Moved) // moved to new territory
                {
                    attackerView.PieceRemoved(pieceView);
                    destination = defenderView.PieceAdded(pieceView);
                }
                else if (!data.Survived) // killed
                {
                    attackerView.PieceRemoved(pieceView);
                    _pieceViews.Remove(data.Piece);
                    _removedPieces.Add(pieceView);
                }
                pieceView.OnAttacked(data.Survived, destination, delay, totalDelay - delay);
                delay += PerPieceTime;
            }
            attackerView.OnAttacked(true, args.Attackers.Select(d => d.Roll), 0.25f, args.Defenders.Count * PerPieceTime + 1f);

            if (!IsLocalHumanPlayer(args.Attacker.Owner.Value))
            {
                _attackEffect.Play();
            }
        }

        /// <summary>
        /// Calculates the pixel extents of drawing this map at (0,0).
        /// </summary>
        private Rectangle CalculatePixelExtents()
        {
            int minX = int.MaxValue, minY = int.MaxValue;
            int maxX = int.MinValue, maxY = int.MinValue;
            foreach (Territory territory in _map.Territories)
            {
                foreach (Cell cell in territory.Area)
                {
                    int x = _context.IsoParams.GetX(cell.Row, cell.Col);
                    int y = _context.IsoParams.GetY(cell.Row, cell.Col);
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
            // hard-coded numbers for tile width and height (ugh)
            return new Rectangle(minX, minY, maxX - minX + 42, maxY - minY + 27);
        }

        /// <summary>
        /// Returns whether the given logical player is a local, human player.
        /// </summary>
        private bool IsLocalHumanPlayer(PlayerId playerId)
        {
            Player player = _context.Players.Single(p => p.Id == playerId);
            return player.Gamer != null && player.Gamer.IsLocal;
        }

        private Map _map;

        private InterfaceContext _context;
        private Dictionary<Territory, TerritoryView> _territoryViews;
        private Dictionary<Piece, PieceView> _pieceViews;
        private List<ConnectionView> _connectionViews;
        private List<PieceView> _removedPieces;

        private SoundEffect _placeEffect;
        private SoundEffect _moveEffect;
        private SoundEffect _attackEffect;
    }
}
