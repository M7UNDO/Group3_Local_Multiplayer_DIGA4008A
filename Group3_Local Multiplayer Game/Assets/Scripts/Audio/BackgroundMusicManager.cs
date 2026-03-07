using System.Collections;
using UnityEngine;

public class BackgroundMusicManager : MonoBehaviour
{
    [Header("Audio Settings")]
    public AudioSource audioSource;
    public AudioClip[] backgroundTracks;
    public AudioClip[] suspensfulTracks;
    public bool playRandomly = false;
    public bool loopAll = true;

    [Header("Transition Settings")]
    public float fadeDuration = 2f;

    private int currentTrackIndex = 0;
    private Coroutine fadeCoroutine;

    // Chase tracking
    private int enemiesChasing = 0;
    private bool isSuspensePlaying = false;

    void Start()
    {
        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();

        if (backgroundTracks.Length > 0)
        {
            if (playRandomly)
            {
                currentTrackIndex = Random.Range(0, backgroundTracks.Length);
                print("Playing random song!");
            }
            else
            {
                currentTrackIndex = 0;
                print("Playing first song!");
            }

            PlayTrack(currentTrackIndex);
        }
        else
        {
            Debug.LogWarning("No background tracks assigned to the Music Manager!");
        }
    }

    void Update()
    {
        if (!audioSource.isPlaying && loopAll && !isSuspensePlaying)
        {
            PlayNextTrack();
        }
    }

    void PlayTrack(int index)
    {
        if (index < 0 || index >= backgroundTracks.Length)
            return;

        audioSource.clip = backgroundTracks[index];
        audioSource.Play();
    }

    void PlayNextTrack()
    {
        if (backgroundTracks.Length == 0)
            return;

        if (playRandomly)
        {
            int randomIndex = Random.Range(0, backgroundTracks.Length);
            while (randomIndex == currentTrackIndex && backgroundTracks.Length > 1)
            {
                randomIndex = Random.Range(0, backgroundTracks.Length);
            }
            currentTrackIndex = randomIndex;
        }
        else
        {
            currentTrackIndex = (currentTrackIndex + 1) % backgroundTracks.Length;
        }

        PlayTrack(currentTrackIndex);
    }

    public void EnemyStartedChase()
    {
        enemiesChasing++;

        if (!isSuspensePlaying)
        {
            PlaySuspenseTrack();
            isSuspensePlaying = true;
        }
    }


    public void EnemyStoppedChase()
    {
        enemiesChasing = Mathf.Max(0, enemiesChasing - 1);

        if (enemiesChasing == 0 && isSuspensePlaying)
        {
            ReturnToBackgroundMusic();
            isSuspensePlaying = false;
        }
    }

    void PlaySuspenseTrack()
    {
        if (suspensfulTracks.Length == 0)
        {
            Debug.LogWarning("No suspense tracks assigned!");
            return;
        }

        int randomIndex = Random.Range(0, suspensfulTracks.Length);
        AudioClip suspenseClip = suspensfulTracks[randomIndex];

        if (fadeCoroutine != null)
            StopCoroutine(fadeCoroutine);

        fadeCoroutine = StartCoroutine(FadeToNewTrack(suspenseClip));
    }

    void ReturnToBackgroundMusic()
    {
        if (backgroundTracks.Length == 0)
        {
            Debug.LogWarning("No background tracks assigned!");
            return;
        }

        int nextTrack = playRandomly ? Random.Range(0, backgroundTracks.Length) : currentTrackIndex;

        if (fadeCoroutine != null)
            StopCoroutine(fadeCoroutine);

        fadeCoroutine = StartCoroutine(FadeToNewTrack(backgroundTracks[nextTrack]));
    }

    IEnumerator FadeToNewTrack(AudioClip newClip)
    {
        float startVolume = audioSource.volume;


        while (audioSource.volume > 0)
        {
            audioSource.volume -= startVolume * Time.deltaTime / fadeDuration;
            yield return null;
        }

        audioSource.clip = newClip;
        audioSource.Play();


        while (audioSource.volume < startVolume)
        {
            audioSource.volume += startVolume * Time.deltaTime / fadeDuration;
            yield return null;
        }

        audioSource.volume = startVolume;
    }
}