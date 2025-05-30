using UnityEngine;

public class OreCleaner : MonoBehaviour
{
    void Update()
    {
        // Если руда активна, но должна быть уничтожена
        if (gameObject.activeInHierarchy)
        {
            // Проверка, что руда "зависла"
            if (GetComponent<Ore>() == null || GetComponent<Collider2D>() == null || !GetComponent<Collider2D>().enabled)
            {
                Debug.LogWarning($"Destroying stuck ore: {name}");
                Destroy(gameObject);
            }
        }
    }

    void OnDestroy()
    {
        // Удаляем из спавнера при уничтожении
        OreSpawner spawner = FindObjectOfType<OreSpawner>();
        if (spawner != null)
        {
            Ore ore = GetComponent<Ore>();
            if (ore != null)
            {
                spawner.RemoveOre(ore);
            }
        }
    }
}