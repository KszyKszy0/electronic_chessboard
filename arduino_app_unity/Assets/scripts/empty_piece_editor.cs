using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class empty_piece_editor : MonoBehaviour
{
    // Start is called before the first frame update
    public GameManager GM;
    public int index;
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
        GetComponent<SpriteRenderer>().sprite = GM.active_sprite;
        GM.main.editor_state[index]=GM.main.current_editor_color*GM.main.current_editor_type;
    }
}
