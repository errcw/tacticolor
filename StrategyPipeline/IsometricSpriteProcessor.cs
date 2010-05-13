using System;
using System.Collections.Generic;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Content.Pipeline;
using Microsoft.Xna.Framework.Content.Pipeline.Graphics;
using Microsoft.Xna.Framework.Content.Pipeline.Processors;

using TInput = Microsoft.Xna.Framework.Content.Pipeline.Graphics.Texture2DContent;
using TOutput = Microsoft.Xna.Framework.Content.Pipeline.Graphics.Texture2DContent;

namespace StrategyPipeline
{
    /// <summary>
    /// A no-op processor until 
    /// </summary>
    [ContentProcessor(DisplayName = "Isometric Sprite Processor")]
    public class IsometricSpriteProcessor : ContentProcessor<TInput, TOutput>
    {
        public override TOutput Process(TInput input, ContentProcessorContext context)
        {
            return input;
        }
    }
}