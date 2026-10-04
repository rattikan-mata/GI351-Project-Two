using UnityEngine;

public class SceneBGM : MonoBehaviour
{
    [Tooltip("ใส่ชื่อ ID เพลงที่ต้องการให้เล่นในซีนนี้")]
    public string bgmId;

    private void Start()
    {
        // สั่งให้ AudioManager เล่นเพลงทันทีที่โหลดซีนนี้ขึ้นมา
        if (!string.IsNullOrEmpty(bgmId))
        {
            AudioManager.Instance?.PlayMusic(bgmId);
        }
    }
}