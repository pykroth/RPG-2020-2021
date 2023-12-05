using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public abstract class Enemy : MonoBehaviour
{
    //Instance Variables
    //Generic
    [Header("Basic")]
    [Tooltip("Name of the enemy type.")]
    public string enemyName;
    [Tooltip("Data Dictionary - Data")]
    public DataDictionary data;
    [SerializeField] protected bool debugDrawGizmos = false;
    [SerializeField] protected float offsetY = 0.0f;

    //Components
    protected SpriteRenderer spriteRenderer;
    protected Rigidbody2D body;
    protected Animator animator;

    //Movement
    [Header("Movement")]
    public float moveSpeed = 3.0f;
    protected Vector2 homePosition;
    protected Transform target; //The thing that the enemy is chasing
    protected Vector2 targetLocation; //Where the enemy wants to go
    protected Vector2 targetDirectionVector; //The vector to the target
    protected float targetLocationDistance;
    protected float moveModifier = 1.0f;
    [Tooltip("Multiplier on enemy movement while in the Patrolling State [Default = 1.0].")]
    public float patrolSpeedModifier = 0.5f;
    [Tooltip("Add Empty GameObjects to this.  The enemy will go through the list in order, and will start at the closest waypoint.")]
    public Transform[] waypointList;
    protected int currentWaypoint = 0;

    //Health
    [Header("Health")]
    [Tooltip("Starting (and max) health of the enemy.  Basic sword deals 10 damage.")]
    public int healthMax = 30;
    [Tooltip("Does this creature respawn when the map reloads?")]
    public bool allowRespawn = true;
    [Tooltip("The default death effect to play when the enemy is defeated and disappears.")]
    public GameObject deathEffect;
    protected int health;
    protected bool isStunned = false;
    protected float isStunnedTicker = 0.0f;
    protected float hitFlashTicker = -0.1f;
    protected float hitFlashDuration = -0.1f;

    //Attacking
    [Header("Combat")]
    [Tooltip("Does this monster want to stop moving towards the player once they are in range of their ranged attack?")]
    public bool preferRanged = false;
    [Tooltip("The radius the enemy begins its melee attack at (and effect of effect for PBAoE melee). This is from the center, so consider the blob's collider side!")]
    public float meleeAttackRadius = 0.9f;
    [Tooltip("The damage of the melee attack")]
    public int meleeAttackDamage = 10;
    [Tooltip("The knockback strength of the melee attack")]
    public float meleeAttackKnockback = 4f;
    [Tooltip("The wait time after the enemy attacks before it can attack again.")]
    public float meleeAttackCooldown = 0.5f;
    [Tooltip("Attack range for the enemy to start a ranged attack")]
    public float rangedAttackRange = 0f;
    [Tooltip("OPTIONAL: Prefab the enemy spawns when doing a ranged attack")]
    public GameObject rangedProjectilePrefab;
    [Tooltip("The wait time after the enemy makes a ranged attack before it can attack again.")]
    public float rangedAttackCooldown = 1.2f;
    protected float attackCooldownTicker = 0f;
    protected bool isAttacking = false;

    //AI Stuff
    [Header("AI")]
    [Tooltip("What AI Behavior to start in: Recommended idle, patrol, or superaggro")]
    public AIstate defaultState = AIstate.idle;
    [Tooltip("The distance at which the monster switches to chasing the player.")]
    public float aggroRadius = 5.0f;
    protected AIstate state; //Current state

    public enum AIstate
    {
        idle, //0 - Wait around at its start location
        patrol, //1 - Moving between waypoints (like idle, but moves)
        chase, //2 - When the player enters aggroRadius, follow the player
        superaggro // 3 - When the player hits the monster, and the monster will find the player at any distance

    } //end enum AIstate

    //[Extra] Audio Variables
    protected SoundPlayer sounds;

    // Start is called before the first frame update
    void Start()
    {
        //Link Components
        body = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        animator = GetComponent<Animator>();

        //Initialize Important Variables
        health = healthMax;
        homePosition = transform.position;
        targetLocation = homePosition;
        state = defaultState;
        attackCooldownTicker = Mathf.Max(meleeAttackCooldown, rangedAttackCooldown);

        //AI State Setups
        if (defaultState == AIstate.patrol && waypointList != null && waypointList.Length > 0)
        {
            //Use a find minium distance of all the waypoints algorithm
            float minDistance = Vector2.Distance(waypointList[0].position, transform.position);
            int minLocation = 0;

            for (int i = 0; i < waypointList.Length; i++)
            {
                float dist = Vector2.Distance(waypointList[i].position, transform.position);
                if (dist < minDistance)
                {
                    minDistance = dist;
                    minLocation = i;
                }
            }

            //Set the current waypoint to nearest waypoint.
            currentWaypoint = minLocation;
        }

        //[Extra] Audio Component Script
        sounds = GetComponent<SoundPlayer>();

        //
        //Check to see if the creature should or should not respawn

        //Make sure the dataBoolean Dictionary exists
        DataDictionary.InitializeCheck(data);

        //The result of the Dictionary check
        // (if it's not found in the dictionary, this will be set to the default value of boolean (false)
        bool isDead = false;

        //If the monster is:
        //   1) not supposed to respawn
        //   2) we found its name in the dictionary
        //   3) it is dead
        // then delete it on level load

        // Optionally you could do:
        // if (!allowRespawn && data.dataBoolean[ getUniqueName() ] )
        //  but that would crash if getUniqueName() isn't in the list
        //  (which in this case, there's a high chance it's not in the list yet)
        if (!allowRespawn && data.dataBoolean.TryGetValue(getUniqueName(), out isDead) && isDead)
        {
            Destroy(this.gameObject);
        }



    } //end Start()

    // Update is called once per frame
    //virtual: keyword that allows this method to be overriden in a child class
    public virtual void Update()
    {
        //Calculate Layer order
        //Note: The value must be between -32768 and 32767.
        spriteRenderer.sortingOrder = 30000 - (int)((spriteRenderer.bounds.min.y + offsetY) * 100);

        //Update the main things
        UpdateHitFlash();
        UpdateStunnedState();
        UpdateAttackCooldowns();

        if (!isStunned)
            UpdateAI();

    } //end Update()

    //Built-In method that runs after ALL updates are finished
    public virtual void LateUpdate()
    {
        CheckHealth();
    }



    protected void Move()
    {
        //Calculate the Vector to the targetLocation.
        targetDirectionVector = targetLocation - (Vector2)transform.position;
        //Normalize this to a unit vector (magnitude of 1)
        targetDirectionVector.Normalize();

        //Calculate Distance to the targetLocation, to prevent overshooting it
        float distance = Vector2.Distance(targetLocation, transform.position);
        Vector2 targetMove = targetDirectionVector * moveSpeed * moveModifier;

        if (distance < targetMove.magnitude)
            targetMove = targetLocation - (Vector2)transform.position;

        //Check to see if the desired move is clear of Players
        bool clear = true;
        bool blockingAlly = false;
        CircleCollider2D myCollider = GetComponent<CircleCollider2D>();

        Vector2 myFutureCenter = body.position + myCollider.offset + targetMove * Time.deltaTime;
        float myRadius = myCollider.radius * Mathf.Max(transform.localScale.x, transform.localScale.y);

        //Detect the colliders that the Enemy would be hitting if it moved there
        Collider2D[] things = Physics2D.OverlapCircleAll(myFutureCenter, myRadius);
        foreach (Collider2D item in things)
        {
            if (item == myCollider)
                continue;
            if (item.CompareTag("Enemy") || item.CompareTag("Player"))
                clear = false;
            if (item.CompareTag("Enemy"))
                blockingAlly = true;
        }

        if (clear == true)
        {
            //Move the monster
            body.velocity += new Vector2(targetMove.x * 0.2f, targetMove.y * 0.2f);
            //Check to see if the monster is moving too fast
            if (body.velocity.magnitude > moveSpeed * moveModifier)
                body.velocity = body.velocity.normalized * moveSpeed * moveModifier;

            //Old Way
            //body.MovePosition(body.position + targetMove);
        }
        else if (clear == false && blockingAlly == true)
        {
            body.velocity += new Vector2(targetMove.x * -0.05f, targetMove.y * -0.05f);
        }

    } //end Move()

    #region Attacking
    public abstract void Attack();

    //Point-Blank Area of Effect Attack
    // Basically a melee attack that hits a radius around the monster
    protected void PBAoEAttack()
    {
        if (!isStunned)
        {
            //Search for players within the meleeAttackRadius
            Collider2D[] things = Physics2D.OverlapCircleAll(transform.position, meleeAttackRadius);
            foreach (Collider2D item in things)
            {
                if (item.gameObject.CompareTag("Player"))
                {
                    //If it's a player, deal melee damage to it
                    Player script = item.gameObject.GetComponent<Player>();

                    //Calculate the direction of force
                    Vector2 hitForce = (item.transform.position - transform.position).normalized * meleeAttackKnockback * 10.0f;

                    //Apply Knockback and damage to player
                    script.Hit(meleeAttackDamage, hitForce);

                    //[Extra] Spawn Damage Text
                    SpawnText(meleeAttackDamage, item.ClosestPoint(transform.position), false);
                }
            } //end of searching for Players

            isAttacking = true;
            attackCooldownTicker = meleeAttackCooldown;
        }//end if not stunned

    } //end PBAoEAttack

    //Spawn a projectile, and then return a reference to the projectile we just spawned
    protected GameObject RangedAttack()
    {
        GameObject projectile = null;

        if (rangedProjectilePrefab != null && target != null && !isStunned)
        {
            //Calculate angle to target
            float deltaY = target.position.y - transform.position.y;
            float deltaX = target.position.x - transform.position.x;
            float angle = Mathf.Atan2(deltaY, deltaX) * Mathf.Rad2Deg;

            //Spawn projectile rotated towards the player
            projectile = Instantiate(rangedProjectilePrefab, transform.position, Quaternion.Euler(0, 0, angle));

            isAttacking = true;
            attackCooldownTicker = rangedAttackCooldown;
        }

        return projectile;

    } //end RangedAttack

    public void EndAttack()
    {
        isAttacking = false;
    }
    #endregion

    #region Health and Damage

    protected virtual void CheckHealth()
    {
        if (health <= 0 && allowRespawn == false)
        {
            data.dataBoolean[getUniqueName()] = true; //Mark that this monster is dead
        }

        if (health <= 0)
        {
            //[Extra] Play Death sound
            sounds.PlayOneShot(sounds.audio_death);

            //Create the deathEffect
            Instantiate(deathEffect, transform.position, Quaternion.identity);

            //[Extra] Change the destroy before based on the death sound
            if (sounds.audio_death == null)
            {
                //Delete the Enemy
                Destroy(this.gameObject);
            }
            else
            {
                GetComponent<Enemy>().enabled = false;
                GetComponent<Collider2D>().enabled = false;
                body.simulated = false;
                spriteRenderer.enabled = false;

                Destroy(this.gameObject, sounds.audio_death.length);

            }

        }
    }

    public void Hit(int incomingDamage, Vector2 forceDirection)
    {
        //Subtract health (we'll check for death in update())
        health -= incomingDamage;

        //Apply the knockback force
        body.AddForce(forceDirection, ForceMode2D.Impulse);

        //Make it stunned and flashy
        isStunned = true;
        isStunnedTicker = forceDirection.magnitude / 20.0f; //About 1/2 of the knockback value in seconds
        isAttacking = false;
        //Maybe we should calculate the duration instead of just 3.0 seconds of flashing?
        hitFlashDuration = 3.0f;

        //Tell animator to play "Hurt" animation
        if (animator != null)
            animator.SetBool("isHurt", true);

        //[Extra] Play hurt sound effect
        sounds.PlayOneShot(sounds.audio_hurt);

        //If hit, enter the superaggro state
        //Search for Players
        Collider2D[] things = Physics2D.OverlapCircleAll(transform.position, aggroRadius * 10f);
        foreach (Collider2D item in things)
        {
            //If there is a player, set target to that player and go to CHASE state
            if (item.gameObject.CompareTag("Player"))
            {
                target = item.gameObject.transform;
                state = AIstate.superaggro;
            }
        }


    }  //end Hit()
    #endregion

    #region UpdateBehaviors
    protected string getUniqueName()
    {
        //Generates a unique name like "Overworld:Orange Blob (3)"
        //                          or "Cave 1:Skeleton (2)"
        return SceneManager.GetActiveScene().name + ":" + gameObject.name;
    } //end getUniqueName()

    protected void UpdateAI()
    {
        //Reset the moveModifier
        moveModifier = 1;

        if (state == AIstate.patrol)
        {
            //State: Idle
            //Possible Default State, Exits to Superaggro and Chase
            //Purpose: Stay at startlocation, until player enters aggroradius, or return to homeposition

            //Update targetLocation
            targetLocation = homePosition;
            moveModifier = 1.2f;

            //Search for Players
            Collider2D[] things = Physics2D.OverlapCircleAll(transform.position, aggroRadius);
            foreach (Collider2D item in things)
            {
                //If there is a player, set target to that player and go to CHASE state
                if (item.gameObject.CompareTag("Player"))
                {
                    target = item.gameObject.transform;
                    state = AIstate.chase;
                }
            }
        }//endif AIstate.idle
        else if (state == AIstate.patrol)
        {
            //State: Patrol
            //Possible Default State, Exits to Superaggro and Chase
            //Purpose: Move between waypointlist, until the player enters aggroradius

            //if there are waypoints try to update targetLocation
            if (waypointList != null && waypointList.Length > 0)
            {
                //Update targetLocation to be the waypoint
                targetLocation = waypointList[currentWaypoint].position;
                moveModifier = patrolSpeedModifier;

                //calculate distance to waypoint
                float distance = Vector2.Distance(targetLocation, transform.position);

                //If we're at the next waypoint, advance the list
                if (distance < 0.1f)
                {
                    //Add 1, but use modulus to keep from going out of bounds
                    currentWaypoint = (currentWaypoint + 1) % waypointList.Length;

                    body.velocity = Vector2.zero;
                }
            }
            else
                state = AIstate.idle;


            //Search for Players
            Collider2D[] things = Physics2D.OverlapCircleAll(transform.position, aggroRadius);
            foreach (Collider2D item in things)
            {
                //If there is a player, set target to that player and go to CHASE state
                if (item.gameObject.CompareTag("Player"))
                {
                    target = item.gameObject.transform;
                    state = AIstate.chase;
                }
            }

        } //endif AIstate.patrol
        else if (state == AIstate.chase && target != null)
        {
            //State: Chase
            //Purpose: Advance towards the player, but if the player leaves aggro range, go back to default state
            //Can exit back to idle or patrol

            moveModifier = 1;
            //Calculate distance to the target
            float distance = Vector2.Distance(target.position, transform.position);

            if (distance > aggroRadius * 1.20f)
            {
                //Conditon: Target is outside of aggrorange +20%.
                //Result: Go back to idle or patrol
                state = defaultState;
                target = null;
            }
            else
            {
                //Condition: Target is within aggrorange +20%
                //Result: Move towards the player (or stay at ranged distance)
                if (preferRanged)
                {
                    //Ranged Perferred
                    if (distance >= rangedAttackRange)
                        targetLocation = target.position; //Out of range: chase player more
                    else
                        targetLocation = transform.position; //In range: stay still
                }
                else
                {
                    //Melee Perferred
                    targetLocation = target.position; //Chase the player!
                }
            }

        } //endif AIstate.chase
        else if (state == AIstate.superaggro && target != null)
        {
            //State: Superaggro
            //Purpose: Chase the player regardless of ranged
            //Provoked by ranged attack or other effects

            moveModifier = 1;
            //Update target Location
            targetLocation = target.position;

            //Check distance, if it's within aggro, just go to Chase AI state
            float distance = Vector2.Distance(targetLocation, transform.position);

            if (distance < aggroRadius)
            {
                //Condition: target is within aggro range
                //Result: Switch to chase mode
                state = AIstate.chase;
            }


        } //endif AIstate.superaggro
        else if (state == AIstate.superaggro && target == null)
        {
            //State: Superaggro WITHOUT a target (usually from being a default state)
            //Purpose: Look for a player in the entire map, otherwise go back to idle.

            //Search for Players
            Collider2D[] things = Physics2D.OverlapCircleAll(transform.position, aggroRadius * 100f);
            foreach (Collider2D item in things)
            {
                //If there is a player, set target to that player and go to CHASE state
                if (item.gameObject.CompareTag("Player"))
                {
                    target = item.gameObject.transform;
                }
            }

            if (target == null)
            {
                state = AIstate.idle;
            }
        }
        else
            state = AIstate.idle;


    } //end UpdateAI()

    protected void UpdateHitFlash()
    {
        if (hitFlashDuration >= 0)
        {
            //Decrease time from the overall duration of flashing
            hitFlashDuration -= Time.deltaTime;
            //Decrease time from the interval until we change from red to white or vice versa
            hitFlashTicker -= Time.deltaTime;

            //Done Flashing
            if (hitFlashDuration < 0)
            {
                spriteRenderer.color = Color.white;
                hitFlashTicker = -0.1f;
                return; //end the method immediately
            }

            //Change Color
            if (hitFlashTicker < 0)
            {
                if (spriteRenderer.color.Equals(Color.red))
                {
                    //Currently red, turn white
                    spriteRenderer.color = Color.white;
                    hitFlashTicker = .15f;
                }
                else
                {
                    //Currently white, turn red
                    spriteRenderer.color = Color.red;
                    hitFlashTicker = .10f;
                }
            }

        } //end if hitFlashDuration >= 0

    } //end UpdateHitFlash()

    protected void UpdateStunnedState()
    {
        //Countdown the stun timer
        if (isStunnedTicker >= 0)
            isStunnedTicker -= Time.deltaTime;

        //Unstun if either the velocity reaches 0 OR stunned timer is up
        if (isStunned && (body.velocity.magnitude < 0.1 || isStunnedTicker < 0))
        {
            //Disable stunned state and stop any movement from the knockback
            isStunned = false;
            body.velocity = Vector2.zero;

            //Stop the flashing
            hitFlashDuration = 0;

            //Exit hurt animation
            if (animator != null)
                animator.SetBool("isHurt", false);
        }
    } //end UpdateStunnedState()

    protected void UpdateAttackCooldowns()
    {
        if (attackCooldownTicker > 0)
        {
            attackCooldownTicker -= Time.deltaTime;
            if (attackCooldownTicker <= 0)
                isAttacking = false;
        }
    }
    #endregion

    private void OnDrawGizmos()
    {
        if (debugDrawGizmos)
        {
            //Draw the Aggro Radius
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(transform.position, aggroRadius);
            //Draw the Ranged Attack Radius
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, rangedAttackRange);
            //Draw the Melee attack Radius
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, meleeAttackRadius);

            //Draw a line to the targetLocation
            Gizmos.color = Color.blue;
            Gizmos.DrawLine(transform.position, targetLocation);

            //Sprite "bottom" for stacking purposes
            //Start() isn't run during the Unity Editor (not play-mode)
            spriteRenderer = GetComponent<SpriteRenderer>();

            //Calculate the lower two points of the sprite's image
            float yValue = spriteRenderer.bounds.min.y + offsetY;
            Vector2 bottomLeft = new Vector2(spriteRenderer.bounds.min.x, yValue);
            Vector2 bottomRight = new Vector2(spriteRenderer.bounds.max.x, yValue);

            //Draw the Sprite Stacking Line
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(bottomLeft, bottomRight);

        }
    }

    //[Extra] Method for Spawning Popup text
    //Use anytime Hit() is called
    //Usage: SpawnText(damage, other.ClosestPoint(transform.position), false);
    public void SpawnText(int damageAmount, Vector2 location, bool isCritical)
    {
        //Set the color for this object (Red)
        Color textColor = new Color(1.0f, 0.0f, 0.0f);

        //Modify the position of the text spawn (if desired)
        Vector2 textSpawnLocation = new Vector2(location.x, location.y);

        //Spawn the text
        DamagePopup.Create(textSpawnLocation, damageAmount, textColor, isCritical);
    }//end SpawnText

}
