using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerFireball : MonoBehaviour
{
    //Fireball stats
    [Tooltip("Speed of the the fireball (Recommended >= 7)")]
    public float moveSpeed = 8.0f;
    [Tooltip("[1-3] Level of the fireball")]
    public int level = 1;
    [Tooltip("The damage to the target the fireball hits")]
    public int damage = 5;
    [Tooltip("The knockback force to the target the fireball hits")]
    public float knockback = 3.0f;
    [Tooltip("Number of enemies can the fireball hit before disappearing")]
    public int impactsLeft = 1;
    [Tooltip("Number of units the fireball travels before disappearing.")]
    public float range = 8;

    //AOE Fireball Stats
    public float aoeRange = 0;
    public int aoeDamage = 0;
    public float aoeKnockback = 0;
    [Tooltip("This can be undefined if aoeRange is 0.")]
    public GameObject explosionPrefab;

    public bool debugDrawGizmos = false;

    //Internal Instance Variables
    private Vector2 startingPosition;
    private List<GameObject> alreadyHit;
    private bool isActive = true;
    private float offsetY = -0.3f;
    //private float moveModifier = 1.0f;

    //Components
    private Rigidbody2D body;
    private Collider2D myCollider;
    private Animator animator;
    private SpriteRenderer spriteRenderer;


    // Start is called before the first frame update
    void Start()
    {
        //Link components
        body = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        myCollider = GetComponent<Collider2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();

        //Initialize Variables
        isActive = true;
        alreadyHit = new List<GameObject>();

        //Make note of the starting position.
        startingPosition = transform.position;
    }

    // Update is called once per frame
    void Update()
    {
        //Calculate Layer order
        //Note: The value must be between -32768 and 32767.
        spriteRenderer.sortingOrder = 30000 - (int)((spriteRenderer.bounds.min.y + offsetY) * 100);

        //Check distance vs range
        if (Vector2.Distance(transform.position, startingPosition) > range && isActive)
            Impact();
    }

    public void FixedUpdate()
    {
        //transform.right is the "forward" vector
        Vector2 moveAmount = transform.right * moveSpeed * Time.deltaTime;

        if (isActive)
            body.MovePosition((Vector2)transform.position + moveAmount);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Enemy") && !alreadyHit.Contains(other.gameObject))
        {
            //Add the enemy to the list
            alreadyHit.Add(other.gameObject);
            impactsLeft--;

            //Calculate Force and Damage
            Vector2 hitForce = new Vector2(0, 0);
            int actualDamage = damage;

            Enemy script = other.GetComponent<Enemy>();

            //Different Behaviors based on level
            if (level == 1)
            {
                //Low knockback in the same direction as fireball
                hitForce = transform.right * knockback * 10.0f;
            }
            else if (level == 2)
            {
                //Low initial knock, relative to location hit.
                //Also spawn AoE effect
                hitForce = (other.transform.position - transform.position).normalized * knockback * 10.0f;
                aoeExplosion();
            }
            else if (level == 3)
            {
                //High knockback in same direction as fireball
                //Also multihit
                hitForce = transform.right * knockback * 10.0f;
            }

            //Apply the damage to the enemy
            script.Hit(actualDamage, hitForce);

            //[Extra]: Spawn damage text
            SpawnText(actualDamage, other.ClosestPoint(transform.position), false);

            if (impactsLeft <= 0)
                Impact();


        }

        if (other.CompareTag("Doodad"))
        {
            Doodad script = other.GetComponent<Doodad>();
            if (script.isTall() == true)
                Impact();

        }
        else if (other.name.Contains("Blocking"))
        {
            //Hit a "Blocking High" collider probably
            Impact();
        }
    }

    public void Impact()
    {
        //If there is an explosion, play one
        if (aoeRange > 0.01 && impactsLeft > 0)
        {
            aoeExplosion();
        }
        //Set the animation to play the impact
        // This animation is also going to call the destroy method
        animator.SetTrigger("Impact");

        //Turn off the colliders
        myCollider.enabled = false;
        isActive = false;
    } //end Impact()

    public void aoeExplosion()
    {
        //Spawn explosion effect
        GameObject explosion = Instantiate(explosionPrefab, transform.position, Quaternion.identity);
        Destroy(explosion, 1.5f);

        //AoE Damage calculations
        //Get all the things within AoErange
        Collider2D[] thingList = Physics2D.OverlapCircleAll(transform.position, aoeRange);
        foreach (Collider2D item in thingList)
        {
            if (item.gameObject.CompareTag("Enemy"))
            {
                //Get the enemy script
                Enemy script = item.GetComponent<Enemy>();
                Vector2 hitForce = (item.transform.position - transform.position).normalized * aoeKnockback * 10.0f;

                //Apply aoeDamage to each enemy
                script.Hit(aoeDamage, hitForce);

                //[Extra] Spawn Damage Text
                SpawnText(aoeDamage, item.ClosestPoint(transform.position), false);
            }
        }

    }  //end aoeExplosion

    //Method for the Animation to Call
    public void Delete()
    {
        Destroy(this.gameObject);
    } //end Delete()

    //[Extra] Method for Spawning Popup text
    //Use anytime Hit() is called
    //Usage: SpawnText(damage, other.ClosestPoint(transform.position), false);
    public void SpawnText(int damageAmount, Vector2 location, bool isCritical)
    {
        //Set the color for this object (Orange)
        Color textColor = new Color(1.0f, 0.3f, 0.0f);

        //Modify the position of the text spawn (if desired)
        Vector2 textSpawnLocation = new Vector2(location.x, location.y);

        //Spawn the text
        DamagePopup.Create(textSpawnLocation, damageAmount, textColor, isCritical);
    }//end SpawnText

    private void OnDrawGizmos()
    {
        if (debugDrawGizmos)
        {
            if (aoeRange > 0)
            {
                //Draw AoE
                Gizmos.color = Color.red;
                Gizmos.DrawWireSphere(transform.position, aoeRange);
            }
            //Draw visual of knockback force
            Vector2 hitForce = transform.right * (knockback + aoeKnockback);
            Gizmos.color = Color.yellow;
            Gizmos.DrawRay(transform.position, hitForce);
        }
    }//end OnDrawGizmos
}
