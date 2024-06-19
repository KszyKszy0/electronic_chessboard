using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class template_equip : MonoBehaviour
{
    // Start is called before the first frame update
    public Sprite img;
    public GameManager GM;
    public int type;
    public int color;
    void Start()
    {
        img=GetComponent<SpriteRenderer>().sprite;
        GM=GameObject.Find("Game_Manager").GetComponent<GameManager>();
    }

    // Update is called once per frame
    void Update()
    {
        
    }


    void OnMouseDown()
    {
        Debug.Log("click");
        GM.active_sprite=img;
        GM.main.current_editor_type=type;
        GM.main.current_editor_color=color;
    }
}
