using MoonshineStudios.CharacterInputController;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class heraldScript : MonoBehaviour
{
    public static event Action<bool, GameObject> atHerald;
    private uiController uiController;

    private void Awake()
    {
        uiController = FindObjectOfType<uiController>();
    }
    private void OnTriggerEnter(Collider other)
    {
        PlayerController playerController = other.GetComponent<PlayerController>();
        if (other.gameObject.CompareTag("Player") && playerController.isCurrentPlayer)
        {
            atHerald?.Invoke(true, other.gameObject);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        PlayerController playerController = other.GetComponent<PlayerController>();
        if (other.gameObject.CompareTag("Player") && playerController.isCurrentPlayer)
        {
            atHerald?.Invoke(false, other.gameObject);
            uiController.HideHeraldMessage();
        }
    }
}
