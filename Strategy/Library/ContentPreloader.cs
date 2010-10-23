using System;
using System.IO;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Storage;

namespace Strategy.Library
{
    /// <summary>
    /// Loads content but discards the result so subsequent loads return the cached result.
    /// </summary>
    public class ContentPreloader
    {
        public ContentPreloader(ContentManager content)
        {
            _content = content;
        }

        /// <summary>
        /// Recursively loads all the content in the given directory.
        /// </summary>
        /// <typeparam name="T">The type of content to load.</typeparam>
        /// <param name="directory">The content directory to load from.</param>
        public void Load<T>(string directory)
        {
            string directoryPath = Path.Combine(_content.RootDirectory, directory);
            foreach (string file in Directory.GetFiles(directoryPath))
            {
                string contentName = Path.GetFileNameWithoutExtension(file);
                string contentPath = Path.Combine(directory, contentName);
                _content.Load<T>(contentPath);
            }
            foreach (string dir in Directory.GetDirectories(directoryPath))
            {
                string dirName = Path.GetFileName(dir);
                string dirPath = Path.Combine(directory, dirName);
                Load<T>(dirPath);
            }
        }

        private ContentManager _content;
    }
}