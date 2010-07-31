using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;

namespace Strategy.Library
{
    public class ContentPreloader
    {
        public ContentPreloader(ContentManager content)
        {
            _content = content;
        }

        public void LoadTextures(string directory)
        {
        }

        public void LoadSounds(string directory)
        {
        }

        public void LoadFonts(string directory)
        {
        }

        private ContentManager _content;
    }
}