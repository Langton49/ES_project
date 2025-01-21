using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class openGate : MonoBehaviour
{
    private Animator animator;

    private static int open = Animator.StringToHash("passed");

    private passage playerPassage;

    private void Awake()
    {
        animator = GetComponent<Animator>();
    }

    private void OnTriggerEnter(Collider other)
    {
        if(other.gameObject.CompareTag("Player"))
        {
            playerPassage = other.gameObject.GetComponent<passage>();
            if (playerPassage.passageGranted)
            {
                animator.SetBool(open, true);
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.gameObject.CompareTag("Player"))
        {
            animator.SetBool(open, false);
        }
    }
}
