using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class playercontroller : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        float rotation = Input.GetAxis("Horizontal");
        float speed = Input.GetAxis("Vertical");

        speed *= Time.deltaTime;
        rotation *= Time.deltaTime * 60.0f;

        transform.Translate(0, speed, 0);
        transform.Rotate(0, 0, rotation);
    }
}
