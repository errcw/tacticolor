using System;
using System.Collections.Generic;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;

using Strategy.Library.Animation;

namespace Strategy.Library.Sound
{
    /// <summary>
    /// Plays a sound effect.
    /// </summary>
    /// <remarks>Calling Update is not required to drive the animation.</remarks>
    public class SoundAnimation : IAnimation
    {
        /// <summary>
        /// Creates a new sound animation.
        /// </summary>
        /// <param name="effect">The effect to play.</param>
        public SoundAnimation(SoundEffect effect) : this(effect, 1f, 1f, 0f, false)
        {
        }

        /// <summary>
        /// Creates a new sound animation.
        /// </summary>
        /// <param name="effect">The effect to play</param>
        /// <param name="volume">Volume, ranging from 0.0f (silence) to 1.0f (full volume)</param>
        /// <param name="pitch">Pitch adjustment, ranging from -1.0f (down one octave) to 1.0f (up one octave)</param>
        /// <param name="pan">Panning, ranging from -1.0f (full left) to 1.0f (full right).</param>
        /// <param name="loop">Whether to loop the sound indefinitely.</param>
        public SoundAnimation(SoundEffect effect, float volume, float pitch, float pan, bool loop)
        {
            _instance = effect.CreateInstance();
            _instance.Volume = volume;
            _instance.Pitch = pitch;
            _instance.Pan = pan;
            _instance.IsLooped = loop;
        }

        /// <summary>
        /// Starts playing the effect.
        /// </summary>
        public void Start()
        {
            _instance.Play();
        }

        /// <summary>
        /// Does nothing.
        /// </summary>
        /// <param name="time">The time elapsed, in seconds, since the last update.</param>
        /// <returns>True if the sound is still playing; otherwise, false.</returns>
        public bool Update(float time)
        {
            return (_instance.State == SoundState.Playing);
        }

        /// <summary>
        /// Stops the effect immediately.
        /// </summary>
        public void Stop()
        {
            _instance.Stop();
        }

        private SoundEffectInstance _instance;
    }
}
