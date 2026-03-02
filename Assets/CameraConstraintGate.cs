using UnityEngine;
using UnityEngine.Events;

public class CameraConstraintGate : MonoBehaviour
{
    public LayerMask playerMask;

    [SerializeField]
    private Vector2 boundingBoxSize;

    [SerializeField]
    private Collider2D leftCollider;

    [SerializeField]
    private Collider2D rightCollider;

    [SerializeField]
    private UnityEvent onPlayerEnterLeft;

    [SerializeField]
    private UnityEvent onPlayerEnterRight;

    [SerializeField]
    private UnityEvent onPlayerExitLeft;

    [SerializeField]
    private UnityEvent onPlayerExitRight;

    [SerializeField]
    private UnityEvent onColliderEventChange;

    private bool lastStateLeft;
    private bool lastStateRight;

    void Update()
    {
        bool leftBoxTriggeredThisFrame = Physics2D.OverlapBox(
            new Vector2(transform.position.x - boundingBoxSize.x / 4, transform.position.y),
            new Vector2(boundingBoxSize.x / 2, boundingBoxSize.y),
            0,
            playerMask
        );
        bool rightBoxTriggeredThisFrame = Physics2D.OverlapBox(
            new Vector2(transform.position.x + boundingBoxSize.x / 4, transform.position.y),
            new Vector2(boundingBoxSize.x / 2, boundingBoxSize.y),
            0,
            playerMask
        );
        bool inAnyZoneNow = leftBoxTriggeredThisFrame || rightBoxTriggeredThisFrame;
        bool wasInAnyZone = lastStateLeft || lastStateRight;

        // Only fire events when transitioning between "any zone" and "no zone"
        if (inAnyZoneNow && !wasInAnyZone)
        {
            // Entering a zone from outside
            if (leftBoxTriggeredThisFrame)
            {
                if (leftCollider && rightCollider)
                {
                    leftCollider.gameObject.SetActive(false);
                    rightCollider.gameObject.SetActive(true);
                }
                onPlayerEnterLeft.Invoke();
                onColliderEventChange.Invoke();
            }
            else if (rightBoxTriggeredThisFrame)
            {
                if (leftCollider && rightCollider)
                {
                    leftCollider.gameObject.SetActive(true);
                    rightCollider.gameObject.SetActive(false);
                }
                onPlayerEnterRight.Invoke();
                onColliderEventChange.Invoke();
            }
        }
        else if (!inAnyZoneNow && wasInAnyZone)
        {
            // Exiting all zones
            if (lastStateLeft)
            {
                if (leftCollider && rightCollider)
                {
                    leftCollider.gameObject.SetActive(true);
                    rightCollider.gameObject.SetActive(false);
                }
                onPlayerExitLeft.Invoke();
                onColliderEventChange.Invoke();
            }
            else if (lastStateRight)
            {
                if (leftCollider && rightCollider)
                {
                    leftCollider.gameObject.SetActive(false);
                    rightCollider.gameObject.SetActive(true);
                }
                onPlayerExitRight.Invoke();
                onColliderEventChange.Invoke();
            }
        }
        lastStateLeft = leftBoxTriggeredThisFrame;
        lastStateRight = rightBoxTriggeredThisFrame;
    }

    void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(
            new Vector2(transform.position.x - boundingBoxSize.x / 4, transform.position.y),
            new Vector2(boundingBoxSize.x / 2, boundingBoxSize.y)
        );

        Gizmos.color = Color.blue;
        Gizmos.DrawWireCube(
            new Vector2(transform.position.x + boundingBoxSize.x / 4, transform.position.y),
            new Vector2(boundingBoxSize.x / 2, boundingBoxSize.y)
        );
    }
}
