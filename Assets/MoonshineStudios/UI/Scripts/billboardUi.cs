using UnityEngine;

public class billboardUi : MonoBehaviour
{
    private GameObject playerCam;

    private void Awake()
    {
        playerCam = GameObject.FindGameObjectWithTag("3rdPersonCam");
    }

    void LateUpdate()
    {
        if (playerCam != null)
        {
            // Make the UI element face the camera while maintaining upright orientation
            transform.rotation = Quaternion.LookRotation(
                transform.position - playerCam.transform.position,
                Vector3.up
            );
        }
    }
}