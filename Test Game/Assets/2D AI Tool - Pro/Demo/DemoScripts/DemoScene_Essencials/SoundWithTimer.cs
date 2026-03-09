using System.Collections;
using UnityEngine;

namespace MaykerStudio
{
    public class SoundWithTimer : MonoBehaviour
    {
        public AudioSource audioSource;

        public IEnumerator PlaySound(Sound sound)
        {
            audioSource.loop = sound.loop;

            audioSource.clip = sound.clip;

            audioSource.volume = sound.volume * (1f + Random.Range(-sound.volumeVariance / 2f, sound.volumeVariance / 2f));
            audioSource.pitch = sound.pitch * (1f + Random.Range(-sound.pitchVariance / 2f, sound.pitchVariance / 2f));

            audioSource.Play();

            yield return new WaitForSeconds(audioSource.clip.length);

            Destroy(gameObject);
        }

    }
}