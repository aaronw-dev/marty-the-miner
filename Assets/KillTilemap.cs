using Unity.Cinemachine;
using UnityEngine;

public class KillTilemap : MonoBehaviour
{
    public CinemachineVirtualCameraBase cameraBase;
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            cameraBase.Follow = null;
            StartCoroutine(ResetAfterDelay(other.gameObject));
        }
    }

    private System.Collections.IEnumerator ResetAfterDelay(GameObject player)
    {
        yield return new WaitForSeconds(1f);
        player.GetComponent<PlatformerController2D>().DieAndReset();
    }
}
