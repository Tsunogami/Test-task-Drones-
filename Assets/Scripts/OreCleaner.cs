using UnityEngine;

public class OreCleaner : MonoBehaviour
{
    void Update()
    {
        // ���� ���� �������, �� ������ ���� ����������
        if (gameObject.activeInHierarchy)
        {
            // ��������, ��� ���� "�������"
            if (GetComponent<Ore>() == null || GetComponent<Collider2D>() == null || !GetComponent<Collider2D>().enabled)
            {
                Debug.LogWarning($"Destroying stuck ore: {name}");
                Destroy(gameObject);
            }
        }
    }

    void OnDestroy()
    {
        // ������� �� �������� ��� �����������
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