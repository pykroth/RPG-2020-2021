using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Heal : MonoBehaviour
{
    public int heal = 50;
    public GameObject Player;
    // Start is called before the first frame update
    void Start()
    {
        Player = GameObject.Find("Player");

    }
        // Update is called once per frame
        void Update()
    {
      

    }
    public void OnTriggerEnter2D(Collider2D other)
    {
        if (other.gameObject.CompareTag("SwordSlash"))
        {
            Debug.Log("Collides");
            Player script = Player.GetComponent<Player>();
            script.Restore(heal);
            
        }
    }
}
