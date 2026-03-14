// ------------------------------------------------------------
//  IGameAudioService.cs  -  _Project.Scripts.Infrastructure
//  Contract for one-shot sound effect playback.
// ------------------------------------------------------------

using UnityEngine;

namespace _Project.Scripts.Infrastructure
{
    /// <summary>
    /// Plays one-shot sound effects with optional pitch variation.
    /// Accepts a single <see cref="AudioClip"/> or an array
    /// (one chosen at random, avoiding immediate repetition).
    /// </summary>
    public interface IGameAudioService
    {
        /// <summary>Plays a clip with random pitch variation at full volume.</summary>
        void PlayOneShot(AudioClip clip);

        /// <summary>Plays a clip with random pitch variation at the given volume.</summary>
        void PlayOneShot(AudioClip clip, float volumeScale);

        /// <summary>Picks a random clip from the array and plays it.</summary>
        void PlayOneShot(AudioClip[] clips);

        /// <summary>Picks a random clip from the array and plays it at the given volume.</summary>
        void PlayOneShot(AudioClip[] clips, float volumeScale);
    }
}
