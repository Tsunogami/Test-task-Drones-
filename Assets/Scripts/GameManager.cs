using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [Header("Faction Settings")]
    public int blueDroneCount = 5;
    public int redDroneCount = 5;
    public float droneSpeedMultiplier = 1f;
    public float resourceSpawnInterval = 3f;

    [Header("Resources")]
    public int blueResources = 0;
    public int redResources = 0;

    [Header("UI References")]
    public Slider blueDroneSlider;
    public Slider redDroneSlider;
    public Slider droneSpeedSlider;
    public TMP_InputField spawnIntervalInput;
    public TMP_Text blueResourcesText;
    public TMP_Text redResourcesText;
    public TMP_Text blueDroneCountText;
    public TMP_Text redDroneCountText;

    [Header("Prefabs")]
    public GameObject blueDronePrefab;
    public GameObject redDronePrefab;
    public Transform blueBase;
    public Transform redBase;

    private List<DroneAI> blueDrones = new List<DroneAI>();
    private List<DroneAI> redDrones = new List<DroneAI>();
    private OreSpawner oreSpawner;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        oreSpawner = FindObjectOfType<OreSpawner>();
        InitializeUI();
        SpawnInitialDrones();
    }

    void InitializeUI()
    {
        // Синие дроны
        blueDroneSlider.minValue = 1;
        blueDroneSlider.maxValue = 20;
        blueDroneSlider.value = blueDroneCount;
        blueDroneSlider.onValueChanged.AddListener(UpdateBlueDroneCount);

        // Красные дроны
        redDroneSlider.minValue = 1;
        redDroneSlider.maxValue = 20;
        redDroneSlider.value = redDroneCount;
        redDroneSlider.onValueChanged.AddListener(UpdateRedDroneCount);

        // Скорость дронов
        droneSpeedSlider.minValue = 0.5f;
        droneSpeedSlider.maxValue = 3f;
        droneSpeedSlider.value = droneSpeedMultiplier;
        droneSpeedSlider.onValueChanged.AddListener(UpdateDroneSpeed);

        // Частота ресурсов
        spawnIntervalInput.text = resourceSpawnInterval.ToString();
        spawnIntervalInput.onEndEdit.AddListener(UpdateSpawnInterval);

        UpdateResourceUI();
    }

    void SpawnInitialDrones()
    {
        // Создаем синих дронов
        for (int i = 0; i < blueDroneCount; i++)
        {
            SpawnDrone(blueDronePrefab, blueBase.position, "Blue");
        }

        // Создаем красных дронов
        for (int i = 0; i < redDroneCount; i++)
        {
            SpawnDrone(redDronePrefab, redBase.position, "Red");
        }
    }

    void SpawnDrone(GameObject prefab, Vector3 position, string faction)
    {
        GameObject droneObj = Instantiate(prefab, position, Quaternion.identity);
        DroneAI drone = droneObj.GetComponent<DroneAI>();
        drone.faction = faction;
        drone.homeBase = faction == "Blue" ? blueBase : redBase;

        if (faction == "Blue") blueDrones.Add(drone);
        else redDrones.Add(drone);
    }

    void DestroyDrones(List<DroneAI> drones)
    {
        foreach (DroneAI drone in drones)
        {
            if (drone != null) Destroy(drone.gameObject);
        }
        drones.Clear();
    }

    // ===== UI Update Methods =====

    public void UpdateBlueDroneCount(float value)
    {
        int newCount = Mathf.RoundToInt(value);
        blueDroneCountText.text = $"Blue Drones: {newCount}";

        if (newCount > blueDroneCount)
        {
            // Добавляем дронов
            for (int i = 0; i < newCount - blueDroneCount; i++)
            {
                SpawnDrone(blueDronePrefab, blueBase.position, "Blue");
            }
        }
        else if (newCount < blueDroneCount)
        {
            // Удаляем дронов
            int toRemove = blueDroneCount - newCount;
            for (int i = 0; i < toRemove && blueDrones.Count > 0; i++)
            {
                DroneAI drone = blueDrones[0];
                if (drone != null) Destroy(drone.gameObject);
                blueDrones.RemoveAt(0);
            }
        }

        blueDroneCount = newCount;
    }

    public void UpdateRedDroneCount(float value)
    {
        int newCount = Mathf.RoundToInt(value);
        redDroneCountText.text = $"Red Drones: {newCount}";

        if (newCount > redDroneCount)
        {
            for (int i = 0; i < newCount - redDroneCount; i++)
            {
                SpawnDrone(redDronePrefab, redBase.position, "Red");
            }
        }
        else if (newCount < redDroneCount)
        {
            int toRemove = redDroneCount - newCount;
            for (int i = 0; i < toRemove && redDrones.Count > 0; i++)
            {
                DroneAI drone = redDrones[0];
                if (drone != null) Destroy(drone.gameObject);
                redDrones.RemoveAt(0);
            }
        }

        redDroneCount = newCount;
    }

    public void UpdateDroneSpeed(float value)
    {
        droneSpeedMultiplier = value;

        // Обновляем скорость всех дронов
        UpdateAllDronesSpeed(blueDrones);
        UpdateAllDronesSpeed(redDrones);
    }

    void UpdateAllDronesSpeed(List<DroneAI> drones)
    {
        foreach (DroneAI drone in drones)
        {
            if (drone != null)
            {
                drone.moveSpeed = drone.baseMoveSpeed * droneSpeedMultiplier;
            }
        }
    }

    public void UpdateSpawnInterval(string value)
    {
        if (float.TryParse(value, out float interval) && interval > 0.1f)
        {
            resourceSpawnInterval = interval;
            if (oreSpawner != null)
            {
                oreSpawner.spawnInterval = resourceSpawnInterval;
            }
        }
    }

    public void AddResources(string faction, int amount)
    {
        if (faction == "Blue")
            blueResources += amount;
        else if (faction == "Red")
            redResources += amount;

        UpdateResourceUI();
    }

    void UpdateResourceUI()
    {
        blueResourcesText.text = $"Blue Resources: {blueResources}";
        redResourcesText.text = $"Red Resources: {redResources}";
    }
}