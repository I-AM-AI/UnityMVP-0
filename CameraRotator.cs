using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CameraRotator : MonoBehaviour
{

    public float speed;
    private bool xUp=true;
    private int dire = 0;
    public Toggle inputToggleAutoRotate;

         // Update is called once per frame
    void Update()
    {
        if (!inputToggleAutoRotate.isOn) return;
        dire++;
        if (xUp)
        {
            transform.Rotate(0.01f, 0, 0);            
            if (dire > 500)
            {
                dire = 0;
                xUp = false;
            }
        }
        else
        {
            transform.Rotate(-0.01f, 0, 0);
            if (dire >= 500)
            {
                dire = 0;
                xUp = true;
            }
        }
        transform.Rotate(0, speed * Time.deltaTime, 0);

        GetComponentInChildren<Camera>().transform.Translate(0, 0, Input.GetAxis("Mouse ScrollWheel") * 5.5f, Space.Self);
    }
}
