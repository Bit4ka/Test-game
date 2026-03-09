using UnityEngine;
using UnityEngine.Audio;

namespace MaykerStudio
{
    [CreateAssetMenu(fileName = "Sound_Asset", menuName = "SoundAsset/Sound Asset")]
    public class Sound : ScriptableObject
    {
        public AudioClip clip;

        [Range(0f, 1f)]
        public float volume = .75f;
        [Range(0f, 1f)]
        public float volumeVariance = .1f;

        [Range(.1f, 3f)]
        public float pitch = 1f;
        [Range(0f, 1f)]
        public float pitchVariance = .1f;

        public bool loop = false;

        public AudioMixerGroup mixerGroup;
    }
}