using UnityEngine;
using System.Collections.Generic;

public class OreSpawner : MonoBehaviour
{
    [Header("Spawn Settings")]
    public GameObject orePrefab;
    public Vector2 spawnArea = new Vector2(50f, 50f);
    public int maxOres = 10;
    public float spawnInterval = 3f;
    public LayerMask spawnCollisionLayer;
    public float minDroneDistance = 3f;

    [Header("Debug")]
    public List<Ore> activeOres = new List<Ore>();

    private float timer;
    private Vector2 spawnCenter;

    void Start()
    {
        spawnCenter = transform.position;
        CleanNullOres();
        PrewarmOres();
    }

    void PrewarmOres()
    {
        while (activeOres.Count < Mathf.Min(maxOres, 5))
        {
            SpawnOre();
        }
    }

    void Update()
    {
        timer += Time.deltaTime;
        CleanNullOres();

        if (timer >= spawnInterval && activeOres.Count < maxOres)
        {
            SpawnOre();
            timer = 0f;
        }
    }

    void CleanNullOres()
    {
        for (int i = activeOres.Count - 1; i >= 0; i--)
        {
            if (activeOres[i] == null)
            {
                activeOres.RemoveAt(i);
            }
        }
    }

    void SpawnOre()
    {
        Vector2 spawnPos = FindValidSpawnPosition();
        if (spawnPos != Vector2.zero)
        {
            GameObject newOre = Instantiate(orePrefab, spawnPos, Quaternion.identity);
            newOre.transform.rotation = Quaternion.identity;

            Ore oreComponent = newOre.GetComponent<Ore>();
            if (oreComponent != null)
            {
                activeOres.Add(oreComponent);
                oreComponent.Initialize(this);
                Debug.Log($"Spawned ore at: {spawnPos}");
            }
            else
            {
                Debug.LogError("Ore prefab missing Ore component!");
                Destroy(newOre);
            }
        }
        else
        {
            Debug.LogWarning("Failed to find valid spawn position");
        }
    }

    Vector2 FindValidSpawnPosition()
    {
        int attempts = 0;
        Vector2 spawnPos;
        bool validPosition;

        do
        {
            validPosition = true;
            spawnPos = spawnCenter + new Vector2(
                Random.Range(-spawnArea.x / 2, spawnArea.x / 2),
                Random.Range(-spawnArea.y / 2, spawnArea.y / 2)
            );

            Collider2D hit = Physics2D.OverlapCircle(spawnPos, 0.5f, spawnCollisionLayer);
            if (hit != null)
            {
                validPosition = false;
            }

            if (validPosition)
            {
                DroneAI[] allDrones = FindObjectsOfType<DroneAI>();
                foreach (DroneAI drone in allDrones)
                {
                    if (Vector2.Distance(spawnPos, drone.transform.position) < minDroneDistance)
                    {
                        validPosition = false;
                        break;
                    }
                }
            }

            attempts++;
        }
        while (!validPosition && attempts < 20);

        return validPosition ? spawnPos : Vector2.zero;
    }

    public void RemoveOre(Ore ore)
    {
        if (ore == null)
        {
            Debug.LogWarning("Trying to remove null ore!");
            return;
        }

        Debug.Log($"Removing ore from list: {ore.name}");

        if (activeOres.Contains(ore))
        {
            activeOres.Remove(ore);
        }
        else
        {
            Debug.LogWarning($"Ore {ore.name} not found in active ores list");
        }

        // ƒополнительна€ страховка на уничтожение
        if (ore != null && ore.gameObject != null)
        {
            Destroy(ore.gameObject);
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireCube(transform.position, new Vector3(spawnArea.x, spawnArea.y, 0.1f));
    }
}