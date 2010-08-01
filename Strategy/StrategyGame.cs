using System;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.GamerServices;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

using Strategy.Interface;
using Strategy.Interface.Screens;
using Strategy.Library;
using Strategy.Library.Components;
using Strategy.Library.Screen;
using Strategy.Properties;

namespace Strategy
{
    /// <summary>
    /// This is the main type for your game
    /// </summary>
    public class StrategyGame : Game
    {
        public StrategyGame()
        {
            GraphicsDeviceManager graphics = new GraphicsDeviceManager(this);
            graphics.PreferredBackBufferWidth = 1280;
            graphics.PreferredBackBufferHeight = 720;
            Window.Title = Resources.StrategyGame;

            Content.RootDirectory = "Content";

            Components.Add(_screens = new ScreenStack(this));
            Components.Add(_trial = new TrialModeObserverComponent(this));

            //Components.Add(new TitleSafeAreaOverlayComponent(this));
            //Components.Add(new FPSOverlay(this));
            Components.Add(new GamerServicesComponent(this));
        }

        protected override void LoadContent()
        {
            base.LoadContent();

            TitleScreen titleScreen = new TitleScreen(this);
            GameplayScreen gameplayScreen = new GameplayScreen(this);
            LobbyScreen lobbyScreen = new LobbyScreen(this, false);

            _screens.Push(titleScreen);
            //_screens.Push(lobbyScreen);
            //_screens.Push(gameplayScreen);
        }


        /// <summary>
        /// Updates the game state.
        /// </summary>
        /// <param name="gameTime">A snapshot of timing values.</param>
        protected override void Update(GameTime gameTime)
        {
            base.Update(gameTime);
            if (_screens.ActiveScreen == null)
            {
                Exit();
            }

#if DEBUG
            if (Keyboard.GetState().IsKeyDown(Keys.Escape))
            {
                Exit();
            }
#endif
        }

        /// <summary>
        /// Draws the game to the screen.
        /// </summary>
        /// <param name="gameTime">A snapshot of timing values.</param>     
        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(new Color(45, 45, 45));
            base.Draw(gameTime);
        }

        private ScreenStack _screens;
        private TrialModeObserverComponent _trial;
    }
}
