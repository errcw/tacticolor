using System;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.GamerServices;
using Microsoft.Xna.Framework.Net;

using Strategy.Net;
using Strategy.Properties;
using Strategy.Library.Components;
using Strategy.Library.Extensions;
using Strategy.Library.Screen;
using Strategy.Library.Sprite;

namespace Strategy.Interface.Screens
{
    /// <summary>
    /// Displays controller button action mappings.
    /// </summary>
    public class ControlsScreen : MenuScreen
    {
        public ControlsScreen(Game game) : base(game)
        {
            SpriteFont font = game.Content.Load<SpriteFont>("Fonts/TextLight");
            Sprite aText = new TextSprite(font, Resources.ControlsA) { Position = new Vector2(436, 237) };
            Sprite bText = new TextSprite(font, Resources.ControlsB) { Position = new Vector2(436, 156) };
            Sprite xText = new TextSprite(font, Resources.ControlsX) { Position = new Vector2(436, 79) };
            Sprite lText = new TextSprite(font, Resources.ControlsLStick) { Position = new Vector2(50, 30) };
            Sprite sText = new TextSprite(font, Resources.ControlsStart) { Position = new Vector2(260, 30) };
            Sprite controller = new ImageSprite(game.Content.Load<Texture2D>("Images/Controls"));
            Sprite controls = new CompositeSprite(controller, aText, bText, xText, lText, sText);

            new MenuBuilder(this, game).CreateImageEntry(controls);

            TransitionOnTime = 0.01f;
            BasePosition = new Vector2(170f, 160f);
        }
    }
}
