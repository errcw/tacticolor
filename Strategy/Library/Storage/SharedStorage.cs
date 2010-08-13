using System;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.GamerServices;

namespace Strategy.Library.Storage
{
    /// <summary>
    /// Storage used for data not specific to a player.
    /// </summary>
    public sealed class SharedStorage : Storage
    {
        public SharedStorage(Game game, string storageContainerName) : base(game, storageContainerName)
        {
        }

        protected override void GetStorageDevice(AsyncCallback callback)
        {
            Guide.BeginShowStorageDeviceSelector(callback, null);
        }
    }
}