using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AudioManager : MonoBehaviour
{
    private static AudioManager _instance;
    public static AudioManager Instance => _instance;

    [Header("Audio Clips")]
    public AudioClip bgMusic;
    public AudioClip pickupSound;
    public AudioClip successSound;

    [Header("Volume Controls")]
    [Range(0f, 1f)] public float bgVolume = 0.5f;
    [Range(0f, 1f)] public float sfxVolume = 0.85f;

    private AudioSource bgSource;
    private AudioSource sfxSource;

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;

        // Initialize BG audio source
        bgSource = gameObject.AddComponent<AudioSource>();
        bgSource.loop = true;
        bgSource.playOnAwake = false;
        bgSource.volume = bgVolume;
        bgSource.spatialBlend = 0f; // 2D stereo background music

        // Initialize SFX audio source
        sfxSource = gameObject.AddComponent<AudioSource>();
        sfxSource.loop = false;
        sfxSource.playOnAwake = false;
        sfxSource.volume = sfxVolume;
        sfxSource.spatialBlend = 0f; // 2D clean crisp sound effect playback
    }

    private void Start()
    {
        PlayBgMusic();
    }

    public void PlayBgMusic()
    {
        if (bgSource != null && bgMusic != null)
        {
            bgSource.clip = bgMusic;
            bgSource.volume = bgVolume;
            bgSource.Play();
        }
    }

    public void PlayPickup()
    {
        if (sfxSource != null && pickupSound != null)
        {
            sfxSource.PlayOneShot(pickupSound, sfxVolume);
        }
    }

    public void PlaySuccess()
    {
        if (sfxSource != null && successSound != null)
        {
            sfxSource.PlayOneShot(successSound, sfxVolume);
        }
    }
}