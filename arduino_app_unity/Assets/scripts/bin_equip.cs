using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class bin_equip : MonoBehaviour
{
    // Start is called before the first frame update
    public GameManager GM;
    void Start()
    {
        GM=GameObject.Find("Game_Manager").GetComponent<GameManager>();
    }

    // Update is called once per frame
    void Update()
    {
        
    }


    void OnMouseDown()
    {
        Debug.Log("click");
        GM.active_sprite=null;
        GM.main.current_editor_type=0;
        GM.main.current_editor_color=0;
    }
}
