using MoonshineStudios.CharacterInputController;
using UnityEngine;

public class winCondition : MonoBehaviour
{
    public serverConnect serverConnect;

    private void Awake()
    {
        serverConnect = FindObjectOfType<serverConnect>();
    }

    private void OnTriggerEnter(Collider other)
    {
        PlayerController player = other.GetComponent<PlayerController>();
        if (player != null && player.isCurrentPlayer)
        {
            serverConnect.NotifyGameWon();
        }
    }
}