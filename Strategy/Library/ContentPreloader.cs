using System;
using System.IO;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Storage;

namespace Strategy.Library
{
    public class ContentPreloader
    {
        public ContentPreloader(ContentManager content)
        {
            _content = content;
            _contentPath = Path.Combine(StorageContainer.TitleLocation, _content.RootDirectory);
        }

        public void Load<T>(string directory)
        {
            string contentFullPath = Path.Combine(_contentPath, directory);
            foreach (string file in Directory.GetFiles(contentFullPath))
            {
                string filePath = Path.Combine(directory, file);
                _content.Load<T>(filePath);
            }
        }

        private ContentManager _content;
        private string _contentPath;
    }
}