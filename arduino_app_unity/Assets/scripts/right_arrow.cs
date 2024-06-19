using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class right_arrow : MonoBehaviour
{
    // Start is called before the first frame update
    public GameObject editor_fields;
    public GameObject ingame_fields;
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    void OnMouseDown()
    {
        GameObject.Find("Main Camera").transform.position+=new Vector3(25,0,0);
        editor_fields.SetActive(true);
        ingame_fields.SetActive(false);
    }
}
