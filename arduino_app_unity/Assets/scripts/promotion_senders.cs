using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class promotion_senders : MonoBehaviour
{
    // Start is called before the first frame update
    public int send_value;
    public GameManager GM;
    void Start()
    {
        GM = GameObject.Find("Game_Manager").GetComponent<GameManager>();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void OnMouseDown()
    {
        GM.type_of_promotion = send_value;
    }
}
