using UnityEngine;

public class BaseController : MonoBehaviour
{
    [Header("Visualization")]
    public float deliveryRadius = 1f;

    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0, 0.5f, 1, 0.3f);
        Gizmos.DrawWireSphere(transform.position, deliveryRadius);
    }
}