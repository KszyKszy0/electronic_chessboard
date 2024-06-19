using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class dot_glowing : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame

    public float dot_timing;
    public float timer = 0;
    void Update()
    {
        timer+=Time.deltaTime;
        if(timer > dot_timing)
        {
            transform.localScale = new Vector3(0.05f,0.05f,0.05f);
            GetComponent<SpriteRenderer>().color = new Color32(255,255,255,1);
            timer=0;
        }else
        {
            transform.localScale += new Vector3(0.00005f,0.00005f,0.00005f);
            GetComponent<SpriteRenderer>().color = new Color32(255,255,255,(byte)(256*(dot_timing-timer)));
        }
    }
}
