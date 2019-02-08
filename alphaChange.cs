using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class alphaChange : MonoBehaviour
{
    // Start is called before the first frame update
    public float alphaLevel = 1.0f;
    public KeyCode decreaseAlpha;
    public KeyCode increaseAlpha;

    GameObject[] respawns;

    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        respawns =GameObject.FindGameObjectsWithTag("cell"); 
        
        if (Input.GetKeyDown(increaseAlpha))  //Debug.Log("im a " + transform.parent.gameObject.name);
        {

            alphaLevel += 0.1f;
            foreach (GameObject go in respawns)
            {
                Color c = go.GetComponent<MeshRenderer>().material.color;
                go.GetComponent<MeshRenderer>().material.color = new Color(c.r, c.g, c.b, alphaLevel);
            }
        }
        if (Input.GetKeyDown(decreaseAlpha)) 
        {
            alphaLevel -= 0.1f;
            foreach (GameObject go in respawns)
            {
                Color c = go.GetComponent<MeshRenderer>().material.color;
                go.GetComponent<MeshRenderer>().material.color = new Color(c.r, c.g, c.b, alphaLevel);
            }
        }



    }
}
