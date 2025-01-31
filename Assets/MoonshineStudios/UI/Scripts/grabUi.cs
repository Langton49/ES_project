using MoonshineStudios.CharacterInputController;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
public class grabUi : MonoBehaviour
{
    private riddleManager riddleManager;
    private GameObject canvas;
    private riddleManager.HidingPlaceDetails[] hidingPlaces;
    private PlayerInputActions playerInputActions;
    private void Awake()
    {
        canvas = GameObject.FindGameObjectWithTag("eKeyActionUI");
        riddleManager = FindObjectOfType<riddleManager>();
        riddleManager.onHidingPlacesFilled += getHidingPlaces;
    }
    private void Start()
    {
        canvas?.SetActive(false);
    }
    public void getHidingPlaces()
    {
        hidingPlaces = riddleManager.hidingPlaces;
    }
    private void OnTriggerEnter(Collider other)
    {
        if (riddleManager.currentIndex < hidingPlaces.Length)
        {
            if (other.gameObject.tag == "Player" && gameObject == hidingPlaces[riddleManager.currentIndex].artifact)
            {
                canvas.SetActive(true);
                canvas.transform.SetParent(transform);
                float diff = 2f;
                canvas.transform.localPosition = new Vector3(0, 0f + diff, 0f);
                canvas.transform.localScale = new Vector3(0.01f, 0.01f, 0.001f);
            }
        }

    }
    private void OnTriggerStay(Collider other)
    {
        if (riddleManager.currentIndex < hidingPlaces.Length) 
        {
            if (other.gameObject.tag == "Player")
            {
                playerInputActions = other.gameObject.GetComponent<PlayerInputActions>();
                if (gameObject == hidingPlaces[riddleManager.currentIndex].artifact && playerInputActions.collectPressed)
                {
                    Debug.Log("collected");
                    canvas.SetActive(false);
                    riddleManager.updateIndex();
                }
                else
                {
                    Debug.Log($"tag: {other.gameObject.tag}\n, gameObject: {gameObject}\n, hidingPlaces[riddleManager.currentIndex].artifact: {hidingPlaces[riddleManager.currentIndex].artifact}\n, playerInputActions.collectPressed: {playerInputActions.collectPressed}\n");
                }
            }
            
        }

    }
    private void OnTriggerExit(Collider other)
    {
         if (other.gameObject.tag == "Player")
            canvas.SetActive(false);
    }
    private void OnDestroy()
    {
        riddleManager.onHidingPlacesFilled -= getHidingPlaces;
    }
}