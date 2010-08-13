using System;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.GamerServices;

namespace Strategy.Library.Storage
{
    /// <summary>
    /// Storage used for player-specific data.
    /// </summary>
    public sealed class PlayerStorage : Storage
    {
        /// <summary>
        /// The index of the player for which the data will be saved.
        /// </summary>
        public PlayerIndex Player { get; private set; }

        public PlayerStorage(Game game, string storageContainerName, PlayerIndex player) : base(game, storageContainerName)
        {
            Player = player;
        }

        protected override void GetStorageDevice(AsyncCallback callback)
        {
            Guide.BeginShowStorageDeviceSelector(Player, callback, null);
        }

        protected override void PrepareEventArgs(StorageEventArgs args)
        {
            base.PrepareEventArgs(args);
            args.PlayerToPrompt = Player;
        }
    }
}