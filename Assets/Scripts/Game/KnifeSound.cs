using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class KnifeSound : MonoBehaviour
{
    private AudioSource audioSource;

    void Start()
    {
        audioSource = GameObject.Find("FoleyInGame").GetComponent<AudioSource>();
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetMouseButtonDown(0)) {
            audioSource.Play();
        }
    }
}
