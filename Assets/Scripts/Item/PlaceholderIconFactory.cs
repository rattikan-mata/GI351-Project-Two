using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// สร้าง Sprite ชั่วคราว (Gray Box + สี/รูปทรงแยกตามประเภทไอเทม) ตอนที่ ItemData ยังไม่มี Icon จริง
/// ใช้ระหว่างรอฝ่าย Art ทำรูปให้เสร็จ ไม่ต้องมีไฟล์รูปใดๆ ในโปรเจกต์เลย (วาดจาก pixel ล้วนๆ)
/// พอ ItemData ใส่ Icon จริงเมื่อไหร่ WorldItem จะสลับไปใช้รูปจริงให้เองอัตโนมัติ ไม่ต้องแก้โค้ดตรงนี้เพิ่ม
///
/// วิธีใช้: PlaceholderIconFactory.GetPlaceholder(itemData.itemType)
/// </summary>
public static class PlaceholderIconFactory
{
    private const int Size = 64;          // ความละเอียดของ placeholder (px)
    private const int PixelsPerUnit = 32; // ให้ขนาดในโลกเกมพอๆ กับ sprite อื่น (ปรับได้ตามเกม)

    // แคช Sprite ที่สร้างแล้วไว้ จะได้ไม่ต้องวาดใหม่ทุกครั้งที่มีของชนิดเดิมโผล่ในฉาก
    private static readonly Dictionary<ItemData.ItemType, Sprite> cache = new Dictionary<ItemData.ItemType, Sprite>();

    // สีประจำแต่ละประเภท เอาไว้แยกด้วยตาตอนเทส (ปรับสีได้ตามชอบ)
    private static readonly Dictionary<ItemData.ItemType, Color> typeColors = new Dictionary<ItemData.ItemType, Color>
    {
        { ItemData.ItemType.Ammo,          new Color(0.75f, 0.65f, 0.35f) }, // น้ำตาล-ทอง (ข้าวสาร)
        { ItemData.ItemType.HealPotion,    new Color(1.00f, 0.40f, 0.60f) }, // ชมพู
        { ItemData.ItemType.TradeItem,     new Color(0.90f, 0.80f, 0.20f) }, // ทอง
        { ItemData.ItemType.SummonToken,   new Color(0.60f, 0.30f, 0.90f) }, // ม่วง
        { ItemData.ItemType.MeleeSpin,     new Color(0.60f, 0.60f, 0.65f) }, // เงิน (มีดพร้า)
        { ItemData.ItemType.ShotgunArc,    new Color(0.90f, 0.55f, 0.10f) }, // ส้ม (ข้าวสารเสก)
        { ItemData.ItemType.SniperShot,    new Color(0.20f, 0.70f, 0.30f) }, // เขียว (หนังสติ๊)
        { ItemData.ItemType.HealOverTime,  new Color(0.40f, 0.90f, 0.60f) }, // เขียวอ่อน (หงส์เขียว)
        { ItemData.ItemType.MeleePunch,    new Color(0.85f, 0.25f, 0.25f) }, // แดง (หมัดพระ)
    };

    private enum Shape { Square, Circle, Diamond, Triangle }

    // รูปทรงประจำแต่ละประเภท (เผื่อสีซ้ำ/ตาบอดสีจะได้ยังแยกออกจากรูปทรง)
    private static readonly Dictionary<ItemData.ItemType, Shape> typeShapes = new Dictionary<ItemData.ItemType, Shape>
    {
        { ItemData.ItemType.MeleeSpin,     Shape.Circle },   // หมุนรอบตัว
        { ItemData.ItemType.ShotgunArc,    Shape.Triangle }, // กระจายมุม
        { ItemData.ItemType.SniperShot,    Shape.Diamond },  // ยิงไกล
        { ItemData.ItemType.HealOverTime,  Shape.Circle },   // เยียวยา
        { ItemData.ItemType.MeleePunch,    Shape.Square },   // หมัดตรง
    };

    public static Sprite GetPlaceholder(ItemData.ItemType type)
    {
        if (cache.TryGetValue(type, out var cached) && cached != null) return cached;

        Sprite sprite = BuildSprite(type);
        cache[type] = sprite;
        return sprite;
    }

    private static Sprite BuildSprite(ItemData.ItemType type)
    {
        Color fill = typeColors.TryGetValue(type, out var c) ? c : Color.gray;
        Shape shape = typeShapes.TryGetValue(type, out var s) ? s : Shape.Square;
        Color border = new Color(0.1f, 0.1f, 0.1f, 1f);

        var pixels = new Color[Size * Size];
        int pad = 4; // เว้นขอบใสรอบนอก กันสับสนตอน sprite ซ้อนพื้นหญ้า/พื้นหลัง

        float cx = Size * 0.5f;
        float cy = Size * 0.5f;
        float half = Size * 0.5f - pad - 6;

        for (int y = 0; y < Size; y++)
        {
            for (int x = 0; x < Size; x++)
            {
                Color pixel = new Color(0f, 0f, 0f, 0f); // โปร่งใสนอกกรอบ

                bool insideBox = x >= pad && x < Size - pad && y >= pad && y < Size - pad;
                if (insideBox)
                {
                    bool onEdge = x == pad || x == Size - pad - 1 || y == pad || y == Size - pad - 1;
                    bool insideShape = IsInsideShape(x, y, cx, cy, half, shape);

                    if (onEdge) pixel = border;
                    else if (insideShape) pixel = fill;
                    else pixel = new Color(fill.r, fill.g, fill.b, 0.25f); // พื้นหลังจางๆ ในกรอบ ให้เห็นขอบ item ชัด
                }

                pixels[y * Size + x] = pixel;
            }
        }

        var tex = new Texture2D(Size, Size, TextureFormat.RGBA32, false)
        {
            filterMode = FilterMode.Point, // คมชัดแบบ pixel art ไม่เบลอ
            wrapMode = TextureWrapMode.Clamp
        };
        tex.SetPixels(pixels);
        tex.Apply();

        return Sprite.Create(tex, new Rect(0, 0, Size, Size), new Vector2(0.5f, 0.5f), PixelsPerUnit);
    }

    private static bool IsInsideShape(int x, int y, float cx, float cy, float half, Shape shape)
    {
        switch (shape)
        {
            case Shape.Circle:
                float dist = Mathf.Sqrt((x - cx) * (x - cx) + (y - cy) * (y - cy));
                return dist <= half;

            case Shape.Diamond:
                return Mathf.Abs(x - cx) + Mathf.Abs(y - cy) <= half;

            case Shape.Triangle:
                // สามเหลี่ยมชี้ขึ้น: กว้างสุดที่ฐานล่าง แคบลงเรื่อยๆ จนถึงยอดบน
                float t = (y - (cy - half)) / (half * 2f); // 0 = ล่าง, 1 = บน
                t = Mathf.Clamp01(t);
                float widthAtY = half * 2f * (1f - t);
                return Mathf.Abs(x - cx) <= widthAtY * 0.5f && y >= cy - half && y <= cy + half;

            default: // Square
                return Mathf.Abs(x - cx) <= half && Mathf.Abs(y - cy) <= half;
        }
    }
}
