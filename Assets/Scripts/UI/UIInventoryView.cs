using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UIInventoryView : MonoBehaviour
{
    [System.Serializable]
    public class SlotUI
    {
        public Image slotFrame;
        public Image iconImage;
        public TextMeshProUGUI countText;
    }

    [SerializeField] private SlotUI[] slots = new SlotUI[4];
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color selectedColor = Color.yellow;

    public void UpdateSlotDisplay(int index, Sprite icon, string countText, bool isSelected)
    {
        if (index < 0 || index >= slots.Length || slots[index] == null) return;

        var slot = slots[index];

        if (slot.slotFrame != null)
            slot.slotFrame.color = isSelected ? selectedColor : normalColor;

        if (slot.iconImage != null)
        {
            if (icon != null)
            {
                slot.iconImage.sprite = icon;
                slot.iconImage.enabled = true;
            }
            else
            {
                slot.iconImage.enabled = false;
            }
        }

        if (slot.countText != null)
        {
            slot.countText.text = countText;
            slot.countText.gameObject.SetActive(!string.IsNullOrEmpty(countText));
        }
    }
}