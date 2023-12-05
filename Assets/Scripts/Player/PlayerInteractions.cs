using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class PlayerInteractions : MonoBehaviour
{
    [Tooltip("The \"Ekey Visual\" player child object")]
    public SpriteRenderer eKey;

    // Start is called before the first frame update
    void Start()
    {
        eKey.enabled = false;
    }

    // Update is called once per frame
    void Update()
    {
        //When the player releases the "use button" currently "e".
        // Can use "Input.GetKeyUp("e")
        if (Input.GetButtonUp("Use Button"))
        {
            Vector2 position = (Vector2)transform.position + GetComponent<CircleCollider2D>().offset;
            float radius = GetComponent<CircleCollider2D>().radius;

            //Get all interactables within range
            Collider2D[] things = Physics2D.OverlapCircleAll(position, radius);
            foreach (Collider2D item in things)
            {
                if (item.CompareTag("Interactable"))
                {
                    item.GetComponent<Interactable>().Trigger();
                }
            }
        }
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        if (other.CompareTag("Interactable"))
        {
            eKey.enabled = true;
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Interactable"))
        {
            eKey.enabled = false;
        }
    }
}
