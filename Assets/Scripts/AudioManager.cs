using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// ตัวจัดการเสียงกลางของเกม เรียกใช้จากสคริปต์ไหนก็ได้ผ่าน AudioManager.Instance
/// วาง Component นี้ไว้บน GameObject เดียวในซีนแรก (Empty GameObject ชื่อ AudioManager ก็พอ)
///
/// วิธีใช้:
/// 1) เพิ่มรายการเสียงใน "Sound Effects" หรือ "Music" ใน Inspector ตั้งชื่อ (id) ให้จำง่าย เช่น "shoot", "hit", "pickup"
/// 2) เรียกเล่นจากสคริปต์อื่นด้วย AudioManager.Instance?.PlaySFX("shoot")
///    (ใช้ ?. กันไว้เผื่อยังไม่มี AudioManager ในซีน จะได้ไม่ error)
///
/// รองรับเล่นเสียงซ้อนกันได้หลายตัวพร้อมกัน (เช่น ยิงรัวๆ ไม่ตัดเสียงกัน) ผ่าน AudioSource Pool
/// แยกเพลงพื้นหลัง (Music) ออกจากเสียงเอฟเฟกต์ (SFX) ให้ปรับเสียงรวมแยกกันได้
/// </summary>
public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [System.Serializable]
    public class Sound
    {
        [Tooltip("ชื่อที่ใช้เรียกเล่นเสียงนี้ (ห้ามซ้ำกันในลิสต์เดียวกัน) เช่น \"shoot\", \"hit\", \"pickup\"")]
        public string id;

        public AudioClip clip;

        [Range(0f, 1f)] public float volume = 1f;

        [Tooltip("ระดับเสียงสูง-ต่ำพื้นฐาน (1 = ปกติ, มากกว่า 1 = สูง/เร็วขึ้น, น้อยกว่า 1 = ต่ำ/ช้าลง)")]
        public float pitch = 1f;

        [Tooltip("ติ๊กไว้ = สุ่มเพี้ยน Pitch เล็กน้อยทุกครั้งที่เล่น กันเสียงซ้ำๆ ฟังจำเจ (เหมาะกับเสียงที่เล่นบ่อยๆ เช่น เสียงยิง/เสียงโดนตี)")]
        public bool randomizePitch = false;

        [Tooltip("ช่วงสุ่ม Pitch บวก/ลบจากค่า Pitch ด้านบน (ใช้ร่วมกับ Randomize Pitch)")]
        public float pitchVariance = 0.1f;
    }

    [Header("Sound Effects (เล่นซ้อนกันได้ เช่น ยิง/โดนตี/เก็บของ)")]
    [SerializeField] private List<Sound> sfxList = new List<Sound>();

    [Header("Music (เพลงพื้นหลัง เล่นทีละเพลง วนลูป)")]
    [SerializeField] private List<Sound> musicList = new List<Sound>();

    [Header("Audio Source Pool")]
    [Tooltip("จำนวน AudioSource สำหรับเล่น SFX พร้อมกัน (มากไป = กินทรัพยากรฟรีๆ, น้อยไป = เสียงอาจโดนแย่งตัดถ้าเล่นถี่มาก)")]
    [SerializeField] private int sfxSourceCount = 8;

    [Header("Master Volume (ควบคุมรวม แยกจาก Volume ต่อเสียงในลิสต์ด้านบน)")]
    [Range(0f, 1f)] [SerializeField] private float masterSfxVolume = 1f;
    [Range(0f, 1f)] [SerializeField] private float masterMusicVolume = 1f;

    [Tooltip("ติ๊กไว้ = ข้ามซีนแล้วตัวนี้ไม่หาย (ควรติ๊กไว้ถ้ามี AudioManager แค่ตัวเดียวของทั้งเกม)")]
    [SerializeField] private bool dontDestroyOnLoad = true;

    private readonly Dictionary<string, Sound> sfxLookup = new Dictionary<string, Sound>();
    private readonly Dictionary<string, Sound> musicLookup = new Dictionary<string, Sound>();

    private AudioSource[] sfxSources;
    private int nextSfxSourceIndex = 0;

    private AudioSource musicSource;
    private Sound currentMusic;

    #region Unity Lifecycle
    private void Awake()
    {
        // Singleton ธรรมดา: ถ้ามีตัวเก่าอยู่แล้ว (เช่นย้อนกลับมาซีนที่มี AudioManager อีกตัว) ให้ตัวใหม่ทำลายตัวเองทิ้ง
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        if (dontDestroyOnLoad) DontDestroyOnLoad(gameObject);

        BuildLookup(sfxList, sfxLookup);
        BuildLookup(musicList, musicLookup);

        // สร้าง AudioSource สำหรับ SFX ไว้ล่วงหน้าตามจำนวนที่ตั้งไว้ (Pool) จะได้เล่นซ้อนกันได้โดยไม่ตัดเสียงเดิมทิ้ง
        sfxSources = new AudioSource[Mathf.Max(1, sfxSourceCount)];
        for (int i = 0; i < sfxSources.Length; i++)
        {
            AudioSource src = gameObject.AddComponent<AudioSource>();
            src.playOnAwake = false;
            src.loop = false;
            sfxSources[i] = src;
        }

        // AudioSource แยกต่างหากสำหรับเพลงพื้นหลัง เล่นวนลูป
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

            if (lookup.ContainsKey(s.id))
            {
                Debug.LogWarning($"[AudioManager] Sound id ซ้ำกัน: \"{s.id}\" -> ใช้ตัวแรกที่เจอในลิสต์เท่านั้น ไปแก้ id ให้ไม่ซ้ำกัน");
                continue;
            }
            lookup[s.id] = s;
        }
    }
    #endregion

    #region SFX
    /// <summary>เล่นเสียงเอฟเฟกต์ตาม id ที่ตั้งไว้ใน Inspector เรียกจากสคริปต์ไหนก็ได้ เช่น AudioManager.Instance?.PlaySFX("shoot")</summary>
    public void PlaySFX(string id)
    {
        if (string.IsNullOrEmpty(id)) return;

        if (!sfxLookup.TryGetValue(id, out Sound sound) || sound.clip == null)
        {
            Debug.LogWarning($"[AudioManager] ไม่พบเสียง SFX id: \"{id}\" -> เช็คว่าใส่ไว้ในลิสต์ Sound Effects แล้วตั้ง Clip ไว้หรือยัง");
            return;
        }

        AudioSource src = sfxSources[nextSfxSourceIndex];
        nextSfxSourceIndex = (nextSfxSourceIndex + 1) % sfxSources.Length;

        src.pitch = sound.randomizePitch
            ? sound.pitch + Random.Range(-sound.pitchVariance, sound.pitchVariance)
            : sound.pitch;

        src.PlayOneShot(sound.clip, sound.volume * masterSfxVolume);
    }

    /// <summary>
    /// เล่นเสียงเอฟเฟกต์ ณ ตำแหน่งในโลกเกม (สร้าง AudioSource ชั่วคราวของ Unity เอง แล้วลบตัวเองหลังเล่นจบ)
    /// ใช้เมื่ออยากให้เสียงมีตำแหน่ง/ระยะ (ต้องตั้ง Spatial Blend ที่ AudioSource ปลายทางเป็น 3D เอง) เช่น เสียงมอนตายตรงจุดที่มันอยู่
    /// หมายเหตุ: วิธีนี้ปรับ Pitch เองไม่ได้ ถ้าต้องการสุ่ม Pitch ให้ใช้ PlaySFX ธรรมดาแทน
    /// </summary>
    public void PlaySFXAtPoint(string id, Vector3 position)
    {
        if (string.IsNullOrEmpty(id)) return;

        if (!sfxLookup.TryGetValue(id, out Sound sound) || sound.clip == null)
        {
            Debug.LogWarning($"[AudioManager] ไม่พบเสียง SFX id: \"{id}\" -> เช็คว่าใส่ไว้ในลิสต์ Sound Effects แล้วตั้ง Clip ไว้หรือยัง");
            return;
        }

        AudioSource.PlayClipAtPoint(sound.clip, position, sound.volume * masterSfxVolume);
    }
    #endregion

    #region Music
    /// <summary>เล่นเพลงพื้นหลังตาม id ถ้าเพลงเดิมเล่นอยู่แล้วจะไม่เริ่มเล่นซ้ำ</summary>
    public void PlayMusic(string id)
    {
        if (string.IsNullOrEmpty(id)) return;
        if (currentMusic != null && currentMusic.id == id && musicSource.isPlaying) return; // เพลงเดิมเล่นอยู่แล้ว ไม่ต้องเริ่มใหม่

        if (!musicLookup.TryGetValue(id, out Sound sound) || sound.clip == null)
        {
            Debug.LogWarning($"[AudioManager] ไม่พบเพลง Music id: \"{id}\" -> เช็คว่าใส่ไว้ในลิสต์ Music แล้วตั้ง Clip ไว้หรือยัง");
            return;
        }

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
    #endregion

    #region Volume Control (เผื่อทำ Settings Menu ทีหลัง)
    public void SetMasterSfxVolume(float volume)
    {
        masterSfxVolume = Mathf.Clamp01(volume);
    }

    public void SetMasterMusicVolume(float volume)
    {
        masterMusicVolume = Mathf.Clamp01(volume);

        // ปรับเสียงเพลงที่กำลังเล่นอยู่ทันทีด้วย (คำนวณจาก Volume เดิมของเพลงนั้น x Master ใหม่)
        if (musicSource != null && currentMusic != null)
        {
            musicSource.volume = currentMusic.volume * masterMusicVolume;
        }
    }
    #endregion
}
