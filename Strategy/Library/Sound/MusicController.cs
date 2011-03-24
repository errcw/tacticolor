using System;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Media;

using Strategy.Library.Animation;
using Strategy.Library.Extensions;

namespace Strategy.Library.Sound
{
    /// <summary>
    /// Provides game-wide control of music.
    /// </summary>
    public class MusicController : GameComponent
    {
        public MusicController(Game game) : base(game)
        {
            MediaPlayer.IsRepeating = true;
        }

        public override void Update(GameTime gameTime)
        {
            float time = gameTime.GetElapsedSeconds();
            if (_fadeOutAnimation != null)
            {
                if (!_fadeOutAnimation.Update(time))
                {
                    _fadeOutAnimation = null;
                    MediaPlayer.Play(_nextSongToPlay);
                }
            }
            if (_fadeInAnimation != null && _fadeOutAnimation == null)
            {
                if (!_fadeInAnimation.Update(time))
                {
                    _fadeInAnimation = null;
                }
            }
        }

        /// <summary>
        /// Starts playing the given song immediately.
        /// </summary>
        public void Play(Song song)
        {
            MediaPlayer.Play(song);
            _fadeOutAnimation = null;
            _fadeInAnimation = null;
        }

        /// <summary>
        /// Starts playing the given song by fading between tracks.
        /// </summary>
        /// <param name="song">The new song to play.</param>
        /// <param name="fadeOutDuration">The time, in milliseconds, to fade out the current song.</param>
        /// <param name="fadeInDuration">The time, in milliseconds, to fade in the new song.</param>
        public void FadeTo(Song song, float fadeOutDuration, float fadeInDuration)
        {
            _nextSongToPlay = song;
            _fadeOutAnimation = new VolumeAnimation(0f, fadeOutDuration, Interpolation.InterpolateFloat(Easing.Uniform));
            _fadeInAnimation = new VolumeAnimation(MusicVolume, fadeInDuration, Interpolation.InterpolateFloat(Easing.Uniform));
        }

        /// <summary>
        /// Animates the media player volume.
        /// </summary>
        private class VolumeAnimation : IAnimation
        {
            public VolumeAnimation(float target, float duration, Interpolate<float> interpolate)
            {
                _target = target;
                _duration = duration;
                _interpolate = interpolate;
                Start();
            }

            public void Start()
            {
                _start = MediaPlayer.Volume;
                _elapsed = 0f;
            }

            public bool Update(float time)
            {
                _elapsed += time;
                if (_elapsed < _duration)
                {
                    MediaPlayer.Volume = _interpolate(_start, _target, _elapsed / _duration);
                    return true;
                }
                else
                {
                    MediaPlayer.Volume = _target;
                    return false;
                }
            }

            protected float _start;
            protected float _target;
            protected Interpolate<float> _interpolate;

            protected float _duration;
            protected float _elapsed;
        }

        private VolumeAnimation _fadeOutAnimation;
        private VolumeAnimation _fadeInAnimation;

        private Song _nextSongToPlay;

        private const float MusicVolume = 0.3f;
    }
}
