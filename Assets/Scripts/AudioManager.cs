using System.Collections.Generic;
using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [System.Serializable]
    public class Sound
    {
        public string id;
        public AudioClip clip;


        [Tooltip("ถ้าใส่ไฟล์เสียงหลายอันตรงนี้ ระบบจะสุ่มเลือกให้เอง (เหมาะกับเสียงเดิน/เสียงโดนตี)")]
        public AudioClip[] randomClips;
     

        [Range(0f, 1f)] public float volume = 1f;
        public float pitch = 1f;
        public bool randomizePitch = false;
        public float pitchVariance = 0.1f;
    }

    [Header("Sound Effects")]
    [SerializeField] private List<Sound> sfxList = new List<Sound>();

    [Header("Music")]
    [SerializeField] private List<Sound> musicList = new List<Sound>();

    [Header("Audio Source Pool")]
    [SerializeField] private int sfxSourceCount = 8;

    [Header("Master Volume")]
    [Range(0f, 1f)][SerializeField] private float masterSfxVolume = 1f;
    [Range(0f, 1f)][SerializeField] private float masterMusicVolume = 1f;

    [SerializeField] private bool dontDestroyOnLoad = true;

    private readonly Dictionary<string, Sound> sfxLookup = new Dictionary<string, Sound>();
    private readonly Dictionary<string, Sound> musicLookup = new Dictionary<string, Sound>();

    private AudioSource[] sfxSources;
    private int nextSfxSourceIndex = 0;

    private AudioSource musicSource;
    private Sound currentMusic;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        if (dontDestroyOnLoad) DontDestroyOnLoad(gameObject);

        BuildLookup(sfxList, sfxLookup);
        BuildLookup(musicList, musicLookup);

        sfxSources = new AudioSource[Mathf.Max(1, sfxSourceCount)];
        for (int i = 0; i < sfxSources.Length; i++)
        {
            AudioSource src = gameObject.AddComponent<AudioSource>();
            src.playOnAwake = false;
            src.loop = false;
            sfxSources[i] = src;
        }

        musicSource = gameObject.AddComponent<AudioSource>();
        musicSource.playOnAwake = false;
        musicSource.loop = true;
    }

    private void BuildLookup(List<Sound> list, Dictionary<string, Sound> lookup)
    {
        lookup.Clear();
        foreach (var s in list)
        {
            if (s == null || string.IsNullOrEmpty(s.id)) continue;
            if (lookup.ContainsKey(s.id)) continue;
            lookup[s.id] = s;
        }
    }

    public void PlaySFX(string id)
    {
        if (string.IsNullOrEmpty(id)) return;
        if (!sfxLookup.TryGetValue(id, out Sound sound)) return;

        // --- เพิ่มระบบสุ่มไฟล์เสียงตรงนี้ ---
        AudioClip clipToPlay = sound.clip;
        if (sound.randomClips != null && sound.randomClips.Length > 0)
        {
            // สุ่มเลือก 1 ไฟล์จากใน Array
            clipToPlay = sound.randomClips[Random.Range(0, sound.randomClips.Length)];
        }
        if (clipToPlay == null) return; // ถ้าไม่ใส่เสียงไว้เลย ให้ข้ามไป
        // -------------------------------

        AudioSource src = sfxSources[nextSfxSourceIndex];
        nextSfxSourceIndex = (nextSfxSourceIndex + 1) % sfxSources.Length;
        src.pitch = sound.randomizePitch ? sound.pitch + Random.Range(-sound.pitchVariance, sound.pitchVariance) : sound.pitch;

        // เปลี่ยนมาเล่นเสียง clipToPlay แทน sound.clip
        src.PlayOneShot(clipToPlay, sound.volume * masterSfxVolume);
    }

    public void PlaySFXAtPoint(string id, Vector3 position)
    {
        if (string.IsNullOrEmpty(id)) return;
        if (!sfxLookup.TryGetValue(id, out Sound sound)) return;

        // --- เพิ่มระบบสุ่มไฟล์เสียงเหมือนด้านบน ---
        AudioClip clipToPlay = sound.clip;
        if (sound.randomClips != null && sound.randomClips.Length > 0)
        {
            clipToPlay = sound.randomClips[Random.Range(0, sound.randomClips.Length)];
        }
        if (clipToPlay == null) return;
        // -------------------------------

        AudioSource.PlayClipAtPoint(clipToPlay, position, sound.volume * masterSfxVolume);
    }

    public void PlayMusic(string id)
    {
        if (string.IsNullOrEmpty(id)) return;
        if (currentMusic != null && currentMusic.id == id && musicSource.isPlaying) return;

        if (!musicLookup.TryGetValue(id, out Sound sound) || sound.clip == null) return;

        currentMusic = sound;
        musicSource.clip = sound.clip;
        musicSource.pitch = sound.pitch;
        musicSource.volume = sound.volume * masterMusicVolume;
        musicSource.Play();
    }

    public void StopMusic()
    {
        musicSource.Stop();
        currentMusic = null;
    }

    public void SetMasterSfxVolume(float volume)
    {
        masterSfxVolume = Mathf.Clamp01(volume);
    }

    public void SetMasterMusicVolume(float volume)
    {
        masterMusicVolume = Mathf.Clamp01(volume);
        if (musicSource != null && currentMusic != null)
        {
            musicSource.volume = currentMusic.volume * masterMusicVolume;
        }
    }
}