using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(Rigidbody2D))]
public class DroneAI : MonoBehaviour
{
    public enum DroneState { MovingToOre, Mining, ReturningToBase }

    public string faction = "Blue";
    public float baseMoveSpeed = 5f;

    [Header("Movement Settings")]
    public float moveSpeed = 5f;
    public float rotationSpeed = 300f;
    public float stoppingDistance = 0.5f;
    public float maxSpeed = 7f;

    [Header("Mining Settings")]
    public float miningDuration = 2f;
    public float collectionRange = 0.3f;

    [Header("Delivery Settings")]
    public float deliveryRange = 0.5f;

    [Header("Collision Avoidance")]
    public float avoidanceRadius = 2f;
    public float avoidanceForce = 8f;
    public float separationForce = 12f;
    public float minSeparationDistance = 1f;
    public LayerMask droneLayer;

    [Header("References")]
    public Transform homeBase;
    public OreSpawner oreManager;
    public GameObject miningEffect;

    // Private variables
    private DroneState currentState = DroneState.MovingToOre;
    private Ore targetOre;
    private float miningTimer;
    private Rigidbody2D rb;
    private bool hasOre = false;
    private float targetSearchCooldown = 0.5f;
    private float lastTargetSearchTime;
    private List<Collider2D> nearbyDrones = new List<Collider2D>();
    private Vector2 movementDirection;
    private bool isMining = false;
    private float oreCarryTime = 0f;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0;
        rb.linearDamping = 2f;

        InitializeReferences();
        baseMoveSpeed = moveSpeed;
        FindNewTarget();
    }

    void InitializeReferences()
    {
        if (oreManager == null)
        {
            oreManager = FindObjectOfType<OreSpawner>();
            if (oreManager == null) Debug.LogError("OreSpawner not found!");
        }

        if (homeBase == null)
        {
            GameObject baseObj = GameObject.FindGameObjectWithTag("Base");
            if (baseObj != null)
            {
                homeBase = baseObj.transform;
                Debug.Log("Base assigned: " + homeBase.name);
            }
            else
            {
                Debug.LogError("Base not found! Creating temporary base.");
                GameObject tempBase = new GameObject("Temp_Base");
                homeBase = tempBase.transform;
                homeBase.position = Vector2.zero;
                homeBase.tag = "Base";
            }
        }
    }

    void Update()
    {
        // Периодический поиск целей
        if (Time.time - lastTargetSearchTime > targetSearchCooldown)
        {
            if ((targetOre == null || !targetOre) && !hasOre && !isMining)
            {
                FindNewTarget();
            }
            lastTargetSearchTime = Time.time;
        }

        HandleMining();
        UpdateNearbyDrones();

        // Проверка достижения руды
        if (currentState == DroneState.MovingToOre && targetOre != null)
        {
            CheckOreProximity();
        }

        // Проверка прибытия на базу
        if (currentState == DroneState.ReturningToBase)
        {
            CheckBaseArrival();
        }

        // Защита от зависания с рудой
        if (hasOre)
        {
            oreCarryTime += Time.deltaTime;
            if (oreCarryTime > 15f)
            {
                Debug.LogWarning($"{name} forced ore delivery due to timeout!");
                DeliverOre();
            }
        }
        else
        {
            oreCarryTime = 0f;
        }
    }

    void UpdateNearbyDrones()
    {
        nearbyDrones.Clear();
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, avoidanceRadius, droneLayer);
        foreach (Collider2D hit in hits)
        {
            if (hit.gameObject != gameObject && hit.GetComponent<DroneAI>() != null)
            {
                nearbyDrones.Add(hit);
            }
        }
    }

    void FixedUpdate()
    {
        ApplyMovement();
    }

    void ApplyMovement()
    {
        if (isMining)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }

        Vector2 targetPosition = GetTargetPosition();
        Vector2 desiredDirection = (targetPosition - (Vector2)transform.position).normalized;

        Vector2 avoidance = CalculateAvoidance();
        movementDirection = (desiredDirection + avoidance).normalized;

        rb.AddForce(movementDirection * moveSpeed * 15f * Time.fixedDeltaTime);

        if (rb.linearVelocity.magnitude > maxSpeed)
        {
            rb.linearVelocity = rb.linearVelocity.normalized * maxSpeed;
        }

        if (rb.linearVelocity.magnitude > 0.1f)
        {
            float targetAngle = Mathf.Atan2(movementDirection.y, movementDirection.x) * Mathf.Rad2Deg;
            Quaternion targetRotation = Quaternion.Euler(0, 0, targetAngle);
            transform.rotation = Quaternion.RotateTowards(
                transform.rotation,
                targetRotation,
                rotationSpeed * Time.fixedDeltaTime
            );
        }
    }

    Vector2 CalculateAvoidance()
    {
        Vector2 avoidance = Vector2.zero;

        foreach (Collider2D droneCollider in nearbyDrones)
        {
            if (droneCollider != null)
            {
                Vector2 toDrone = (Vector2)(transform.position - droneCollider.transform.position);
                float distance = toDrone.magnitude;

                if (distance < minSeparationDistance)
                {
                    float forceFactor = 1 - (distance / minSeparationDistance);
                    avoidance += toDrone.normalized * separationForce * forceFactor;
                }
                else
                {
                    float forceFactor = 1 - (distance / avoidanceRadius);
                    avoidance += toDrone.normalized * avoidanceForce * forceFactor;
                }
            }
        }

        if (avoidance.magnitude > 0)
        {
            avoidance = avoidance.normalized * Mathf.Min(avoidance.magnitude, avoidanceForce * 1.5f);
        }

        return avoidance;
    }

    Vector2 GetTargetPosition()
    {
        if (hasOre)
        {
            return homeBase.position;
        }
        else if (targetOre != null && targetOre.gameObject != null)
        {
            return targetOre.transform.position;
        }
        else
        {
            return homeBase.position;
        }
    }

    void CheckOreProximity()
    {
        if (targetOre == null) return;

        float distance = Vector2.Distance(transform.position, targetOre.transform.position);
        if (distance <= collectionRange)
        {
            StartMining();
        }
    }

    void CheckBaseArrival()
    {
        if (hasOre && homeBase != null)
        {
            float distance = Vector2.Distance(transform.position, homeBase.position);
            if (distance <= deliveryRange)
            {
                DeliverOre();
            }
        }
    }

    public void DeliverOre()
    {
        if (!hasOre) return;

        GameManager.Instance.AddResources(faction, 1);

        Debug.Log($"{name} delivered ore to base!");
        hasOre = false;
        isMining = false;
        oreCarryTime = 0f;

        currentState = DroneState.MovingToOre;
        FindNewTarget();
    }

    void FindNewTarget()
    {
        if (hasOre || isMining)
        {
            return;
        }

        if (oreManager == null || oreManager.activeOres == null || oreManager.activeOres.Count == 0)
        {
            currentState = DroneState.ReturningToBase;
            return;
        }

        List<Ore> validOres = new List<Ore>();
        foreach (Ore ore in oreManager.activeOres)
        {
            if (ore != null && ore.gameObject != null)
            {
                validOres.Add(ore);
            }
        }

        if (validOres.Count == 0)
        {
            currentState = DroneState.ReturningToBase;
            return;
        }

        targetOre = validOres[Random.Range(0, validOres.Count)];
        currentState = DroneState.MovingToOre;
        Debug.Log($"{name} new target: {targetOre.name}");
    }

    void StartMining()
    {
        if (targetOre == null) return;

        currentState = DroneState.Mining;
        isMining = true;
        miningTimer = 0f;
        rb.linearVelocity = Vector2.zero;

        Debug.Log($"{name} started mining: {targetOre.name}");

        if (miningEffect != null)
        {
            miningEffect.SetActive(true);
        }
    }

    void HandleMining()
    {
        if (currentState != DroneState.Mining) return;

        miningTimer += Time.deltaTime;
        if (miningTimer >= miningDuration)
        {
            CompleteMining();
        }
    }

    void CompleteMining()
    {
        if (targetOre != null)
        {
            Debug.Log($"{name} collecting ore: {targetOre.name}");
            // Сохраняем ссылку перед вызовом Collect
            Ore oreToCollect = targetOre;
            targetOre = null; // Немедленно очищаем ссылку

            oreToCollect.Collect();
            hasOre = true;
            oreCarryTime = 0f;
        }

        if (miningEffect != null)
        {
            miningEffect.SetActive(false);
        }

        currentState = DroneState.ReturningToBase;
        isMining = false;

        Debug.Log($"{name} mining complete. Returning to base.");
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Base"))
        {
            if (hasOre)
            {
                DeliverOre();
            }
        }
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Drone"))
        {
            Vector2 repelDirection = (transform.position - collision.transform.position).normalized;
            rb.AddForce(repelDirection * separationForce * 20f, ForceMode2D.Impulse);
        }
    }

    void OnDrawGizmosSelected()
    {
        if (currentState == DroneState.MovingToOre && targetOre != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(transform.position, targetOre.transform.position);
            Gizmos.DrawWireSphere(targetOre.transform.position, collectionRange);
        }
        else if (currentState == DroneState.ReturningToBase && homeBase != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawLine(transform.position, homeBase.position);
            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(homeBase.position, deliveryRange);
        }

        Gizmos.color = new Color(1, 0, 0, 0.2f);
        Gizmos.DrawWireSphere(transform.position, avoidanceRadius);

        Gizmos.color = new Color(1, 0.5f, 0, 0.3f);
        Gizmos.DrawWireSphere(transform.position, minSeparationDistance);
    }
}