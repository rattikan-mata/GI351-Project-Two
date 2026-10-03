using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class HouseKeyPickup : MonoBehaviour
{
    private void Awake() => GetComponent<Collider2D>().isTrigger = true;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            StoryManager.Instance.hasHouseKey = true;
            Debug.Log("เก็บกุญแจบ้านได้แล้ว!");
            AudioManager.Instance?.PlaySFX("item_pickup"); // เล่นเสียงเก็บของ
            Destroy(gameObject);
        }
    }
}