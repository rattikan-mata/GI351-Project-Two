using UnityEngine;

/// <summary>
/// วัตถุของบนพื้น 2 กรณีการใช้งาน:
/// 1) วางไว้ในแมพล่วงหน้า -> ตั้งค่า Item Data ใน Inspector ตั้งแต่ตอน Design
/// 2) ผู้เล่นโยนของทิ้ง (กด G) -> PlayerController จะ Instantiate แล้วเรียก Setup() ใส่ข้อมูลให้ทีหลัง
/// ผู้เล่นกด F ตอนอยู่ในระยะ (PlayerController ใช้ Physics2D.OverlapCircleAll หา WorldItem) เพื่อเก็บ
/// สำหรับไอเทมประเภทอาวุธที่มีความคงทน จะจำค่าความคงทนที่เหลืออยู่ไว้ด้วย เผื่อโยนของพังครึ่งทางแล้วเก็บกลับมาใหม่
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class WorldItem : MonoBehaviour
{
    #region Item Data
    [Tooltip("ตั้งไว้ล่วงหน้าได้เลยถ้าเป็นของที่วางในแมพ ถ้าเป็นของที่ผู้เล่นโยนทิ้งจะถูกตั้งผ่าน Setup() แทน")]
    [SerializeField] private ItemData itemData;
    public ItemData Data => itemData;

    private int currentDurability;
    public int CurrentDurability => currentDurability;
    #endregion

    #region Visual
    [SerializeField] private SpriteRenderer spriteRenderer;
    #endregion

    #region Unity Lifecycle
    private void Awake()
    {
        // Collider ต้องเป็น Trigger เพื่อกันไม่ให้ไปชนกายภาพกับผู้เล่น/มอน (แค่ให้ตรวจระยะเก็บของเท่านั้น)
        GetComponent<Collider2D>().isTrigger = true;

        if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();

        // ของที่วางไว้ล่วงหน้าใน Editor -> เริ่มด้วยความคงทนเต็ม
        if (itemData != null) currentDurability = itemData.maxDurability;

        UpdateVisual();
    }
    #endregion

    #region Setup (สำหรับของที่ถูกโยนทิ้ง/สลับ ตอน Runtime)
    /// <summary>
    /// ตั้งค่าไอเทมของวัตถุนี้ใหม่ ใช้ตอนผู้เล่นโยนของทิ้ง (G) หรือสลับของ (F ตอนกระเป๋าเต็ม)
    /// durability: ค่าความคงทนที่เหลืออยู่ตอนโยนทิ้ง ถ้าไม่ระบุ (-1) จะเริ่มด้วยความคงทนเต็มของไอเทมนั้น
    /// </summary>
    public void Setup(ItemData data, int durability = -1)
    {
        itemData = data;
        currentDurability = (durability >= 0) ? durability : (data != null ? data.maxDurability : 0);
        UpdateVisual();
    }

    private void UpdateVisual()
    {
        if (spriteRenderer == null || itemData == null) return;

        // มี Icon จริงจากฝ่าย Art แล้ว -> ใช้เลย
        // ยังไม่มี (icon == null) -> ใช้ Placeholder แบบ Gray Box ที่ generate เองตาม itemType ไปพลางๆ
        spriteRenderer.sprite = itemData.icon != null
            ? itemData.icon
            : PlaceholderIconFactory.GetPlaceholder(itemData.itemType);
    }
    #endregion
}