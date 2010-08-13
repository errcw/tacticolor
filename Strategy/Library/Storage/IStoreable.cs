using System;
using System.IO;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Storage;

namespace Strategy.Library.Storage
{
    /// <summary>
    /// Data stored on a storage device.
    /// </summary>
    public interface IStoreable
    {
        /// <summary>
        /// The name of the file in which to store the data.
        /// </summary>
        string FileName { get; }

        /// <summary>
        /// Saves the data to a storage device.
        /// </summary>
        /// <param name="stream">The stream to which to write the data.</param>
        void Save(Stream stream);

        /// <summary>
        /// Loads the data from a storage device.
        /// </summary>
        /// <param name="stream">The stream from which to load the data.</param>
        void Load(Stream stream);
    }
}