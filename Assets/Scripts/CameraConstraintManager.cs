using Unity.Cinemachine;
using UnityEngine;

public class CameraConstraintManager : MonoBehaviour
{
    PolygonCollider2D activeCollider;
    private CinemachineConfiner2D confiner;
    public CinemachineVirtualCameraBase cam;
    public static CameraConstraintManager global;

    void Awake()
    {
        global = this;
    }

    public void DisableAllAndSetFirst()
    {
        foreach (Transform t in transform)
        {
            PolygonCollider2D collider = t.GetComponent<PolygonCollider2D>();
            if (collider != null)
            {
                collider.gameObject.SetActive(false);
            }
        }

        if (transform.childCount > 0)
        {
            Transform firstChild = transform.GetChild(0);
            PolygonCollider2D firstCollider = firstChild.GetComponent<PolygonCollider2D>();
            if (firstCollider != null)
            {
                firstChild.gameObject.SetActive(true);
                activeCollider = firstCollider;
                confiner.BoundingShape2D = activeCollider;
                confiner.BakeBoundingShape(cam, 10);
            }
        }
    }
    void Start()
    {
        confiner = cam.GetComponent<CinemachineConfiner2D>();
    }

    public void RefreshConfiner()
    {

        foreach (Transform t in transform)
        {
            GameObject g = t.gameObject;
            if (g.activeInHierarchy && g.GetComponent<PolygonCollider2D>())
            {
                activeCollider = g.GetComponent<PolygonCollider2D>();
                break;
            }
        }
        confiner.BoundingShape2D = activeCollider;
        confiner.BakeBoundingShape(cam, 10);
    }

    public void SetActiveGate(CameraConstraintGate gate)
    {
        // Disable all colliders first
        foreach (Transform t in transform)
        {
            PolygonCollider2D collider = t.GetComponent<PolygonCollider2D>();
            if (collider != null)
            {
                collider.gameObject.SetActive(false);
            }
        }

        // Find and activate the collider associated with this gate
        // We'll use the right collider from the gate to find the matching constraint
        if (gate.rightCollider != null)
        {
            foreach (Transform t in transform)
            {
                PolygonCollider2D polygonCollider = t.GetComponent<PolygonCollider2D>();
                if (polygonCollider == gate.rightCollider)
                {
                    t.gameObject.SetActive(true);
                    activeCollider = polygonCollider;
                    confiner.BoundingShape2D = activeCollider;
                    confiner.BakeBoundingShape(cam, 10);
                    return;
                }
            }
        }

        // Fallback: if no matching collider found, use first available
        DisableAllAndSetFirst();
    }
}
