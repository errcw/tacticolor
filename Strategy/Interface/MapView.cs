using System;
using System.Collections.Generic;
using System.Linq;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using Strategy.Gameplay;
using Strategy.Library;
using Strategy.Library.Extensions;

namespace Strategy.Interface
{
    /// <summary>
    /// Shows a map.
    /// </summary>
    public class MapView
    {
        public MapView(Map map, Match match, InterfaceContext context)
        {
            _map = map;
            _context = context;

            match.PiecePlaced += OnPiecePlaced;
            match.PiecesMoved += OnPiecesMoved;
            match.TerritoryAttacked += OnTerritoryAttacked;

            Rectangle extents = CalculatePixelExtents();
            _context.IsoParams.OffsetX = (1280 - extents.Width) / 2 - extents.X;
            _context.IsoParams.OffsetY = (720 - extents.Height) / 2 - extents.Y;

            _territoryViews = new Dictionary<Territory, TerritoryView>(_map.Territories.Count);
            _pieceViews = new Dictionary<Piece, PieceView>(map.Territories.Count * 9);
            _connectionViews = new List<ConnectionView>(map.Territories.Count * 3);
            foreach (Territory territory in _map.Territories)
            {
                TerritoryView territoryView = new TerritoryView(territory, _context);
                _territoryViews.Add(territory, territoryView);
                foreach (Piece piece in territory.Pieces)
                {
                    Cell holder = territoryView.PieceAdded(piece);
                    PieceView pieceView = new PieceView(piece, holder, false, _context);
                    _pieceViews.Add(piece, pieceView);
                }
                foreach (Territory neighbor in territory.Neighbors)
                {
                    ConnectionView connectionView = new ConnectionView(territory, neighbor, _context);
                    _connectionViews.Add(connectionView);
                }
            }
            _removedPieces = new List<PieceView>(16);

            _isoBatch = new IsometricBatch(new SpriteBatch(context.Game.GraphicsDevice));
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
            _removedPieces.RemoveAll(view => !view.IsVisible);
        }

        /// <summary>
        /// Draws all the elements of the map.
        /// </summary>
        public void Draw()
        {
            // draw the connections beneath the map
            _isoBatch.Begin();
            _connectionViews.ForEach(view => view.Draw(_isoBatch));
            _isoBatch.End();

            // then draw the territories and pieces
            _isoBatch.Begin();
            _territoryViews.Values.ForEach(view => view.Draw(_isoBatch));
            _pieceViews.Values.ForEach(view => view.Draw(_isoBatch));
            _removedPieces.ForEach(view => view.Draw(_isoBatch));
            _isoBatch.End();
        }

        /// <summary>
        /// Notifies this view that a piece was placed on the map.
        /// </summary>
        public void OnPiecePlaced(object match, PiecePlacedEventArgs args)
        {
            TerritoryView territoryView = _territoryViews[args.Location];
            Cell holder = territoryView.PieceAdded(args.Piece);
            PieceView pieceView = new PieceView(args.Piece, holder, true, _context);
            _pieceViews.Add(args.Piece, pieceView);
        }

        /// <summary>
        /// Notifies this view that pieces were moved on the map.
        /// </summary>
        public void OnPiecesMoved(object match, PiecesMovedEventArgs args)
        {
            TerritoryView sourceView = _territoryViews[args.Source];
            TerritoryView destinationView = _territoryViews[args.Destination];
            foreach (Piece piece in args.Pieces)
            {
                PieceView pieceView = _pieceViews[piece];
                sourceView.PieceRemoved(piece);
                Cell cell = destinationView.PieceAdded(piece);
                pieceView.OnMoved(cell);
            }
            destinationView.MaybeChangedOwners(0f);
        }

        /// <summary>
        /// Notifies this view that a territory was attacked on the map.
        /// </summary>
        public void OnTerritoryAttacked(object match, TerritoryAttackedEventArgs args)
        {
            TerritoryView attackerView = _territoryViews[args.Attacker];
            TerritoryView defenderView = _territoryViews[args.Defender];

            const float PieceDelay = 0.1f;
            float delay = 0f;
            float totalDelay = (args.Attackers.Count + args.Defenders.Count) * PieceDelay;

            // handle the defenders
            delay = args.Attackers.Count * PieceDelay;
            foreach (PieceAttackData data in args.Defenders)
            {
                PieceView pieceView = _pieceViews[data.Piece];
                pieceView.OnAttacked(data.Roll, data.Survived, null, delay, totalDelay - delay + 0.5f);
                if (!data.Survived)
                {
                    defenderView.PieceRemoved(data.Piece);
                    _pieceViews.Remove(data.Piece);
                    _removedPieces.Add(pieceView);
                }
                delay += PieceDelay;
            }

            // handle the attackers
            delay = 0f;
            foreach (PieceAttackData data in args.Attackers)
            {
                PieceView pieceView = _pieceViews[data.Piece];
                Cell? destination = null;
                if (data.Survived && data.Moved) // moved to new territory
                {
                    attackerView.PieceRemoved(data.Piece);
                    destination = defenderView.PieceAdded(data.Piece);
                }
                else if (!data.Survived) // killed
                {
                    attackerView.PieceRemoved(data.Piece);
                    _pieceViews.Remove(data.Piece);
                    _removedPieces.Add(pieceView);
                }
                pieceView.OnAttacked(data.Roll, data.Survived, destination, delay, totalDelay - delay + 0.5f);
                delay += PieceDelay;
            }

            defenderView.MaybeChangedOwners(totalDelay + 0.5f);
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

        private Map _map;
        private InterfaceContext _context;

        private IsometricBatch _isoBatch;

        private IDictionary<Territory, TerritoryView> _territoryViews;
        private IDictionary<Piece, PieceView> _pieceViews;
        private ICollection<ConnectionView> _connectionViews;
        private List<PieceView> _removedPieces;
    }
}
