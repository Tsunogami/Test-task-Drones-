using UnityEngine;

public class Ore : MonoBehaviour
{
    private OreSpawner spawner;
    private bool isCollected = false;

    public void Initialize(OreSpawner spawner)
    {
        this.spawner = spawner;
        gameObject.name = "Ore_" + System.Guid.NewGuid().ToString().Substring(0, 4);
        isCollected = false;
    }

    public void Collect()
    {
        if (isCollected) return;

        Debug.Log($"Collecting ore: {name}");
        isCollected = true;

        // 1. Удаляем из спавнера
        if (spawner != null)
            spawner.RemoveOre(this);
        else
            Debug.LogError("Ore has no spawner reference!");

        // 2. Отключаем визуальные компоненты
        SpriteRenderer renderer = GetComponent<SpriteRenderer>();
        if (renderer != null) renderer.enabled = false;

        Collider2D collider = GetComponent<Collider2D>();
        if (collider != null) collider.enabled = false;

        // 3. Немедленно уничтожаем объект
        Destroy(gameObject);
    }
}