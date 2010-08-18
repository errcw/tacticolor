﻿using System;
using System.Diagnostics;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Media;

using Strategy.Library.Storage;

namespace Strategy.Interface
{
    /// <summary>
    /// Game interface options.
    /// </summary>
    public class Options
    {
        /// <summary>
        /// Turns sound effects on or off.
        /// </summary>
        public bool SoundEffectsToggle
        {
            get { return _options.SoundEffectsToggle; }
            set
            {
                _options.SoundEffectsToggle = value;
                SoundEffect.MasterVolume = _options.SoundEffectsToggle ? 1f : 0f;
            }
        }

        /// <summary>
        /// Turns music on or off.
        /// </summary>
        public bool MusicToggle
        {
            get { return _options.MusicToggle; }
            set
            {
                _options.MusicToggle = value;
                MediaPlayer.Volume = _options.MusicToggle ? 1f : 0f;
            }
        }

        /// <summary>
        /// Creates a new set of options with default values.
        /// </summary>
        public Options(Storage storage)
        {
            _storage = storage;

            _options = new OptionsData();
            SoundEffectsToggle = true;
            MusicToggle = true;
        }

        /// <summary>
        /// Loads these options from storage.
        /// </summary>
        public void Load()
        {
            try
            {
                _storage.Load(_storeableOptions);
                _options = _storeableOptions.Data;
                SoundEffectsToggle = _options.SoundEffectsToggle;
                MusicToggle = _options.MusicToggle;
            }
            catch (Exception e)
            {
                // options unavailable, stick with the defaults
                Debug.WriteLine(e);
            }
        }

        /// <summary>
        /// Stores these options.
        /// </summary>
        public void Save()
        {
            try
            {
                _storeableOptions.Data = _options;
                _storage.Save(_storeableOptions);
            }
            catch (Exception e)
            {
                Debug.WriteLine(e);
            }
        }

        private Storage _storage;

        private OptionsData _options;
        private readonly XmlStoreable<OptionsData> _storeableOptions = new XmlStoreable<OptionsData>("StrategyOptions");
    }

    /// <summary>
    /// The saveable options data.
    /// </summary>
    public struct OptionsData
    {
        public bool SoundEffectsToggle;
        public bool MusicToggle;
    }
}