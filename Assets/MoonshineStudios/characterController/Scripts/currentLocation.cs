using MoonshineStudios.CharacterInputController;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class currentLocation : MonoBehaviour
{
    public uiController uiController;
    public GameObject displayLocation;
    public TMP_Text location;
    private Coroutine currentCoroutine;
    private string lastLocation;

    private void OnTriggerEnter(Collider other)
    {
        PlayerController playerController = other.GetComponent<PlayerController>();
        if (other.gameObject.CompareTag("Player") && playerController.isCurrentPlayer)
        {
            updateLocation();
            Debug.Log("Player entered " + gameObject.transform.parent.name);
        }
    }

    public void updateLocation()
    {
        if (currentCoroutine != null)
        {
            StopCoroutine(currentCoroutine);
        }
        StartCoroutine(UpdateLocationWithDelay());
    }

    private IEnumerator UpdateLocationWithDelay()
    {
        if (lastLocation == gameObject.transform.parent.name) yield break;
       
        location.text = gameObject.transform.parent.name;  
        lastLocation = gameObject.transform.parent.name;
        yield return new WaitForSeconds(5);  
        location.text = "";
    }
}
