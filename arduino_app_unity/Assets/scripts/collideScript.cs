using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class collideScript : MonoBehaviour
{
    public int sq;
    public GameManager GM;
    public Sprite default_sprite;
    // Start is called before the first frame update
    void Start()
    {
        default_sprite=GetComponent<SpriteRenderer>().sprite;
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    
    void OnMouseDown()
    {
        GM.recieveField(sq);
    }
}
