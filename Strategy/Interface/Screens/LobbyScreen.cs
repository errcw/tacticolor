using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.GamerServices;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Net;

using Strategy.AI;
using Strategy.Gameplay;
using Strategy.Net;
using Strategy.Properties;
using Strategy.Library.Extensions;
using Strategy.Library.Input;
using Strategy.Library.Screen;

namespace Strategy.Interface.Screens
{
    /// <summary>
    /// Sets up the networking.
    /// </summary>
    public class LobbyScreen : Screen
    {
        public LobbyScreen(StrategyGame game, NetworkSession session)
        {
            _game = game;
            _input = _game.Services.GetService<MenuInput>();
            _players = new List<Player>();

            _session = session;
        }

        protected override void UpdateActive(GameTime gameTime)
        {
            _session.Update();
            HandleNetworkInput();
            HandleLocalInput();

            if (_session.IsHost && _session.IsEveryoneReady && _session.SessionState == NetworkSessionState.Lobby)
            {
                _session.StartGame();
            }
        }

        protected override void UpdateInactive(GameTime gameTime)
        {
            // continue updating the network session even if other temporary screens are on top
            if (_session.SessionState == NetworkSessionState.Lobby)
            {
                _session.Update();
                HandleNetworkInput();
            }
        }

        protected internal override void Show(bool pushed)
        {
            _session.GamerJoined += OnGamerJoined;
            _session.GamerLeft += OnGamerLeft;
            _session.HostChanged += OnHostChanged;
            _session.GameStarted += OnGameStarted;
            _session.GameEnded += OnGameEnded;
            _session.SessionEnded += OnSessionEnded;
            base.Show(pushed);
        }

        protected internal override void Hide(bool popped)
        {
            _session.GamerJoined -= OnGamerJoined;
            _session.GamerLeft -= OnGamerLeft;
            _session.HostChanged -= OnHostChanged;
            _session.GameStarted -= OnGameStarted;
            _session.GameEnded -= OnGameEnded;
            _session.SessionEnded -= OnSessionEnded;
            if (popped)
            {
                _session.Dispose();
                _session = null;
            }
            base.Hide(popped);
        }

        private void OnGamerJoined(object sender, GamerJoinedEventArgs args)
        {
            Debug.WriteLine(args.Gamer.Gamertag + " joined");
            AddPlayer(args.Gamer);
            if (_session.IsHost)
            {
                if (_seed == 0)
                {
                    _seed = _random.Next(1, int.MaxValue);
                }
                SendSeed(_seed, args.Gamer);
            }
        }

        private void OnGamerLeft(object sender, GamerLeftEventArgs args)
        {
            Debug.WriteLine(args.Gamer.Gamertag + " left");
            RemovePlayer(args.Gamer);
        }

        private void OnHostChanged(object sender, HostChangedEventArgs args)
        {
            Debug.WriteLine(args.NewHost.Gamertag + " is now host (was " + args.OldHost.Gamertag + ")");

            // the host might have backed out before every player received
            // the seed data so broadcast a new seed to every player
            if (_session.IsHost)
            {
                _seed = _random.Next(1, int.MaxValue);
                foreach (NetworkGamer gamer in _session.RemoteGamers)
                {
                    SendSeed(_seed, gamer);
                }
            }
        }

        private void OnGameStarted(object sender, GameStartedEventArgs args)
        {
            Debug.Assert(_seed != 0);
            Debug.WriteLine("Game starting");

            // assign ids to players by sorting based on unique id
            // this assignment guarantees identical assignments across machines
            _players.Sort((a, b) => a.Gamer.Id.CompareTo(b.Gamer.Id));
            for (int p = 0; p < _players.Count; p++)
            {
                _players[p].Id = (PlayerId)p;
            }

            Random gameRandom = new Random(_seed);
            MapGenerator generator = new MapGenerator(gameRandom);
            Map map = generator.Generate(MapType.Filled, MapSize.Normal);

            GameplayScreen gameplayScreen = new GameplayScreen((StrategyGame)Stack.Game, _session, _players, map, gameRandom);
            Stack.Push(gameplayScreen);
        }

        private void OnGameEnded(object sender, GameEndedEventArgs args)
        {
            Debug.WriteLine("Game ended");
        }

        private void OnSessionEnded(object sender, NetworkSessionEndedEventArgs args)
        {
            // if the session ended before the game started then we encountered an error
            MessageScreen messageScreen = new MessageScreen(Stack.Game, Resources.NetworkError);
            Stack.Push(messageScreen);
        }

        private void AddPlayer(NetworkGamer gamer)
        {
            Player player = new Player();
            player.Gamer = gamer;

            // for local players find the local controller
            if (gamer.IsLocal)
            {
                for (PlayerIndex p = PlayerIndex.One; p <= PlayerIndex.Four; p++)
                {
                    if (Gamer.SignedInGamers[p].Gamertag == gamer.Gamertag)
                    {
                        player.Controller = p;
                        break;
                    }
                }
                if (!player.Controller.HasValue)
                {
                    Debug.WriteLine("Local player with no controller! Falling back to controller one.");
                    player.Controller = PlayerIndex.One;
                }
            }

            _players.Add(player);
        }

        private void RemovePlayer(NetworkGamer gamer)
        {
            _players.RemoveAll(player => player.Gamer == gamer);
            if (_players.Count == 0)
            {
                // lost all the players, back out to the main menu
                Stack.Pop();
            }
        }

        private Player FindPlayerByController(PlayerIndex index)
        {
            return _players.Find(player => player.Controller == index);
        }

        /// <summary>
        /// Broadcasts the game seed to every player in the session.
        /// </summary>
        private void SendSeed(int seed, NetworkGamer gamer)
        {
            LocalNetworkGamer sender = (LocalNetworkGamer)_session.Host;
            CommandWriter writer = new CommandWriter();
            writer.Write(new InitializeMatchCommand(seed, MapType.LandRush, MapSize.Normal, AIDifficulty.Normal));
            sender.SendData(writer, SendDataOptions.Reliable);
        }

        /// <summary>
        /// Receives seeds.
        /// </summary>
        private void HandleNetworkInput()
        {
            CommandReader reader = new CommandReader();
            foreach (LocalNetworkGamer gamer in _session.LocalGamers)
            {
                while (gamer.IsDataAvailable)
                {
                    NetworkGamer sender;
                    gamer.ReceiveData(reader, out sender);
                    InitializeMatchCommand command = reader.ReadCommand() as InitializeMatchCommand;
                    if (command != null)
                    {
                        _seed = command.RandomSeed;
                    }
                }
            }
        }

        /// <summary>
        /// Handle input for every local player in the lobby.
        /// </summary>
        private void HandleLocalInput()
        {
            for (PlayerIndex p = PlayerIndex.One; p <= PlayerIndex.Four; p++)
            {
                Player player = FindPlayerByController(p);
                if (_input.Join[(int)p].Released)
                {
                    if (player != null)
                    {
                        // mark this player as ready
                        if (_seed != 0)
                        {
                            Debug.WriteLine(player.Gamer.Gamertag + " is ready");
                            player.Gamer.IsReady = true;
                        }
                    }
                    else
                    {
                        // first time we saw this player
                        if (!p.IsSignedIn() && !Guide.IsVisible)
                        {
                            try
                            {
                                // prompt the player to sign in
                                Guide.ShowSignIn(1, _session.IsOnlineSession());
                            }
                            catch
                            {
                                // ignore whatever guide errors occur
                            }
                        }
                        if (p.IsSignedIn())
                        {
                            if (_session.IsOnlineSession() && !p.CanPlayOnline())
                            {
                                // cannot join this online session
                                MessageScreen messageScreen = new MessageScreen(Stack.Game, Resources.NetworkErrorCannotPlayOnline);
                                Stack.Push(messageScreen);
                            }
                            else
                            {
                                // join the existing session
                                _session.AddLocalGamer(p.GetSignedInGamer());
                            }
                        }
                    }
                }
                else if (_input.Leave[(int)p].Released)
                {
                    // ignore leave requests from non-players
                    if (player != null)
                    {
                        if (player.Gamer.IsReady)
                        {
                            // back out of the ready state
                            Debug.WriteLine(player.Gamer.Gamertag + " is unready");
                            player.Gamer.IsReady = false;
                        }
                        else
                        {
                            // leave the session if the request came from the main controller
                            if (p == _input.Controller)
                            {
                                Stack.Pop();
                            }
                        }
                    }
                }
            }
        }

        private StrategyGame _game;
        private NetworkSession _session = null;

        private List<Player> _players;

        private MenuInput _input;

        private int _seed = 0;
        private Random _random = new Random();
    }
}