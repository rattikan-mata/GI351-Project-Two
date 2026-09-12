using UnityEngine;

//ต้นไม้สำหรับแลกของ: ผู้เล่นเดินเข้าใกล้แล้วกดปุ่ม Interact เพื่อแลกของเป็นเลือด
[RequireComponent(typeof(Collider2D))]
public class TradeTree : MonoBehaviour
{
    #region Trade Settings
    [Header("Trade Settings")]
    [SerializeField] private int itemCostPerTrade = 2;//จำนวนของที่ต้องใช้ต่อการแลก 1 ครั้ง

    [SerializeField] private int healAmountPerTrade = 20;//เลือดที่ได้ต่อการแลก 1 ครั้ง

    [SerializeField] private KeyCode interactKey = KeyCode.E;//ปุ่มที่ใช้กดแลกของตอนยืนอยู่ในระยะต้นไม้
    #endregion

    #region State
    private bool playerInRange = false;//ผู้เล่นอยู่ในระยะต้นไม้หรือยัง
    private PlayerController playerInRangeRef;//เก็บ reference ผู้เล่นที่ยืนอยู่ในระยะ จะได้เรียกใช้ตอนแลกของ
    #endregion

    #region Unity Lifecycle
    private void Awake()
    {
        GetComponent<Collider2D>().isTrigger = true; //ตั้งเป็น Trigger เพื่อเช็คผู้เล่นเดินเข้า-ออกระยะ
    }

    private void Update()
    {
        if (!playerInRange || playerInRangeRef == null) return; //ผู้เล่นไม่อยู่ในระยะ ไม่ต้องเช็คปุ่ม

        if (Input.GetKeyDown(interactKey))
        {
            TryTrade();
        }
    }
    #endregion

    #region Range Detection
    //ตวรจจับ player เดินเข้ามาในระยะต้นไม้
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.TryGetComponent<PlayerController>(out var player))
        {
            playerInRange = true;
            playerInRangeRef = player;
        }
    }

    //ผู้เล่นเดินออกจากระยะ -> ลืม reference ทิ้ง กันกดแลกจากทางไกล
    private void OnTriggerExit2D(Collider2D other)
    {
        if (playerInRangeRef != null && other.gameObject == playerInRangeRef.gameObject)
        {
            playerInRange = false;
            playerInRangeRef = null;
        }
    }
    #endregion

    #region Trade Logic
    private void TryTrade()
    {
        if (playerInRangeRef.SpendTradeItems(itemCostPerTrade))
        {
            playerInRangeRef.Heal(healAmountPerTrade);
            Debug.Log($"[TradeTree] แลกสำเร็จ: -{itemCostPerTrade} ของ, +{healAmountPerTrade} เลือด");
        }
        else
        {
            Debug.Log("[TradeTree] ของไม่พอสำหรับแลก");
        }
    }
    #endregion
}
