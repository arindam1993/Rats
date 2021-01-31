using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Footsteps : MonoBehaviour
{
    private AudioSource audioSource;

    private bool isWalking = false;

    void Start()
    {
        audioSource = GetComponent<AudioSource>();
    }

    void Update()
    {
       GetWalkingState();
       PlayAudio();
    }

    private void GetWalkingState() {
        if (Input.GetButton("Horizontal") || Input.GetButton("Vertical")) {
            // left shift puts user in sneak mode
            if (Input.GetKey(KeyCode.LeftShift)) {
                isWalking = false;
            } else {
                isWalking = true;
            }
        } else {
            isWalking = false;
        }
    }

    private void PlayAudio() {
        if ( isWalking ) {
            if ( !audioSource.isPlaying ) {
                audioSource.Play();
            }
        } else {
            audioSource.Stop();
        }
    }
}
