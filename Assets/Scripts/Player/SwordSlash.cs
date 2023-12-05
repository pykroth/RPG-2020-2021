using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SwordSlash : MonoBehaviour
{
    List<GameObject> alreadyHit;
    public GameObject player;
    public int damage = 10;
    public float knockback = 5.0f;

    private SpriteRenderer spriteRenderer;
    private float offsetY = -0.1f;


    // Start is called before the first frame update
    void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();

        //Optionally destroy after 1 second
        //Destroy(this.gameObject, 1.0f);

        //Make a new List
        alreadyHit = new List<GameObject>();

    } //end Start()

    // Update is called once per frame
    void Update()
    {
        //Update the layerOrder
        spriteRenderer.sortingOrder = 30000 - (int)((spriteRenderer.bounds.min.y + offsetY) * 100);
       
        
    } //end Update()

    private void FixedUpdate()
    {
        //Follow the player around
        if (player != null)
            transform.position = player.transform.position;
    }

    //Called by the animation
    public void EndAnimation()
    {
        Destroy(this.gameObject);
    }

    public void DisableCollider()
    {
        GetComponent<Collider2D>().enabled = false;
    }

    //Code for hitting things
    public void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Doodad"))
        {
            
            
            TreeTransition script = other.GetComponent<TreeTransition>();
            script.Hit(damage);

            alreadyHit.Add(other.gameObject);



            SpawnText(damage, other.ClosestPoint(transform.position), false);
        }

        if (other.CompareTag("Enemy") && !alreadyHit.Contains(other.gameObject))
        {
            Vector2 sourcePosition = player.transform.position;
            Vector2 swordPosition = transform.position;
            Vector2 enemyPosition = other.transform.position;

            //Also try enemyPosition - swordPosition
            Vector2 hitForce = (enemyPosition - sourcePosition).normalized * knockback * 10.0f;

            //Get the enemy script
            Enemy script = other.GetComponent<Enemy>();
            script.Hit(damage, hitForce);

            //Add the enemy to the "already hit" list
            alreadyHit.Add(other.gameObject);

            //[Extra] Spawn Damage Text
            SpawnText(damage, other.ClosestPoint(transform.position), false);

        }
        
    } //end OnTriggerEnter2D

    //[Extra] Method for Spawning Popup text
    //Use anytime Hit() is called
    //Usage: SpawnText(damage, other.ClosestPoint(transform.position), false);
    public void SpawnText(int damageAmount, Vector2 location, bool isCritical)
    {
        //Set the color for this object (Light Gray)
        Color textColor = new Color(0.7f, 0.7f, 0.7f);

        //Modify the position of the text spawn (if desired)
        Vector2 textSpawnLocation = new Vector2(location.x, location.y);

        //Spawn the text
        DamagePopup.Create(textSpawnLocation, damageAmount, textColor, isCritical);
    }//end SpawnText
 
}
