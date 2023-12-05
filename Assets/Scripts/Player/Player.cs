using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.UI;

public class Player : MonoBehaviour
{

    //Health and Mana (and UI)
    public IntValue health; //health has two variables: health.currentValue and health.maxValue
    public IntValue mana;
    private Image healthBar;
    private Image manaBar;
    private bool isStunned = false;
    private float isStunnedTicker = 0.0f;
    private float hitFlashDuration = 0.0f;
    private float hitFlashTicker = 0.0f;

    //Movement
    public float moveSpeed = 5.0f;
    private float moveModifier = 1.0f;
    private Vector2 toMouse;

    //Melee Attack
    public GameObject swordAttackPrefab;
    public float meleeAttackCooldown = 0.25f;
    private float attackCooldownTicker = 0.0f;
    private bool isAttacking;

    //Range Attack
    public GameObject[] rangedProjectile;
    //public int[] rangedDamage = { 5, 15, 45 }; //Damage is on the projectile prefab...
    public int[] rangedManaCost = { 15, 30, 60 };
    public float[] rangedTimeGoal = { 0.5f, 0.5f, .75f }; //Level 1: 0.5 seconds, Level 2: 1.0 second, Level 3: 1.75 seconds
    public float rangedAttackCooldown = 0.40f;
    public float manaPerSecond = 10f;
    private float manaRegenerateTicker = 1;
    private int rangedChargeLevel;
    private float rangedChargeTime;
    private bool isCharging;
    private bool isFreeze = false ;
    //ChargeBar Stuff
    public GameObject chargeBarPrefab;
    private GameObject chargeBar;
    public Color[] chargeBarRangedColors = { Color.black, Color.red, new Color(1.0f, 0.5f, 0.0f), Color.yellow };
    [Tooltip("Set this to the Scene's Canvas, not a prefab.")]
    public GameObject uiCanvas;
    private float attackCooldownTickerMax = 1;



    //Renderering
    public bool debugDrawGizmos = false;
    [Tooltip("Positive Numbers make this object more likely to be behind things.")]
    private float offsetY = 0.0f;  //Make this public if player sprite order needs tweaked

    private Vector2 movement = new Vector2();
    private Rigidbody2D body;
    private Animator animator;
    private SpriteRenderer spriteRenderer;

    //Loading new Levels
    public ExitVectorValue startingPosition;

    //[Extra] Audio
    private SoundPlayer sounds;

    // Start is called before the first frame update
    void Start()
    {
        //Attach components to Variables
        body = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        uiCanvas = GameObject.Find("Canvas");

        //Attack Settings
        isAttacking = false;
        isCharging = false;
        isStunned = false;
        toMouse = new Vector2(0, 0);
        manaRegenerateTicker = 1.0f / manaPerSecond;
        rangedChargeLevel = 0;
        rangedChargeTime = 0;

        UpdateUI();
        SetupPlayerLocation();

        //[Extra] Attach Audio Script
        sounds = GetComponent<SoundPlayer>();

    } //end Start()

    private void UpdateUI()
    {
        //Search for health and mana bars
        if (healthBar == null)
        {
            GameObject healthBarSearch = GameObject.Find("HealthBar");
            if (healthBarSearch != null)
                healthBar = healthBarSearch.GetComponent<Image>();
        }
        if (manaBar == null)
        {
            GameObject manaBarSearch = GameObject.Find("ManaBar");
            if (manaBarSearch != null)
                manaBar = manaBarSearch.GetComponent<Image>();
        }

        //Redraw the health and mana bars if one has changed
        if (healthBar != null)
            healthBar.fillAmount = (float)health.currentValue / health.maxValue;
        if (manaBar != null)
            manaBar.fillAmount = (float)mana.currentValue / mana.maxValue;
    }

    private void SetupPlayerLocation()
    {
        Tilemap ground = GameObject.Find("Ground").GetComponent<Tilemap>();
        //Setup Starting Location on Scene Load
        //Get the edge of the world from the camera script
        float xMin = ground.localBounds.min.x;
        float xMax = ground.localBounds.max.x;
        float yMin = ground.localBounds.min.y;
        float yMax = ground.localBounds.max.y;

        //variables to make accessing the Scriptable Object's starting position cleaner
        float xStart = startingPosition.spawningPosition.x;
        float yStart = startingPosition.spawningPosition.y;

        //Determine where to spawn the player
        if (startingPosition.exit == SceneTransition.ExitDirection.Point)
            transform.position = startingPosition.spawningPosition;
        else if (startingPosition.exit == SceneTransition.ExitDirection.South)
            transform.position = new Vector2(xStart, yMax + yStart);
        else if (startingPosition.exit == SceneTransition.ExitDirection.North)
            transform.position = new Vector2(xStart, yMin + yStart);
        else if (startingPosition.exit == SceneTransition.ExitDirection.West)
            transform.position = new Vector2(xStart + xMax, yStart);
        else if (startingPosition.exit == SceneTransition.ExitDirection.East)
            transform.position = new Vector2(xStart + xMin, yStart);
    } //end SetupPlayerLocation

    // Update is called once per frame
    void Update()
    {
        /*
        //Used to test health
        if (Input.GetKeyUp("r"))
        {
            Hit(20, Random.insideUnitCircle.normalized * 20);
        }
        */

        //Calculate Layer order
        //Note: The value must be between -32768 and 32767.
        spriteRenderer.sortingOrder = 30000 - (int)((spriteRenderer.bounds.min.y) * 100);

        UpdateGetInputs();
        UpdateAnimations();
        UpdateCooldowns();
        UpdateHitFlash();
        UpdateStunnedState();


        //
        //Player Attacks
        //
        UpdateMeleeAttack();
        UpdateRangedAttack();



        //Moving Modifiers
        if (isAttacking)
            moveModifier = 0.0f; //Stop all movement when attacking
        else if (isCharging)
            moveModifier = 0.0f;
        else
            moveModifier = 1.0f;

    } //end Update()

    private void FixedUpdate()
    {
        if (!isStunned && (movement.x != 0 || movement.y != 0))
        {
            //Normalize the movement vector
            if (movement.magnitude > 1)
                movement.Normalize();

            Move(movement * moveSpeed * moveModifier);

        }
    } //end FixedUpdate()


    //This method is to be used during Update to get the keyboard and mouse input
    private void UpdateGetInputs()
    {
        //Get Inputs
        movement.x = Input.GetAxisRaw("Horizontal");
        movement.y = Input.GetAxisRaw("Vertical");
        Vector2 mouseWorldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        toMouse = mouseWorldPos - body.position;
    } //end UpdateGetInputs()

    private void Move(Vector2 move)
    {
        //Check to see if the desired move is clear of Players
        bool clear = true;
        CircleCollider2D myCollider = GetComponent<CircleCollider2D>();

        Vector2 myFutureCenter = body.position + myCollider.offset + move * Time.deltaTime;
        float myRadius = myCollider.radius * Mathf.Max(transform.localScale.x, transform.localScale.y);

        //Detect the colliders that the Enemy would be hitting if it moved there
        Collider2D[] things = Physics2D.OverlapCircleAll(myFutureCenter, myRadius);
        foreach (Collider2D item in things)
        {
            if (item == myCollider)
                continue;
            if (item.CompareTag("Enemy") || item.CompareTag("Player"))
                clear = false;

        }

        if (clear)
        {
            body.velocity += new Vector2(move.x * 0.2f, move.y * 0.2f);
            if (body.velocity.magnitude > moveSpeed)
            {
                body.velocity = body.velocity.normalized * moveSpeed;
            }

            //Uses Drag
            //body.velocity = new Vector2(move.x, move.y);
            //Simplest movement
            //body.MovePosition(body.position + move * Time.deltaTime);
        }
    }

    private void OnDrawGizmos()
    {
        if (debugDrawGizmos)
        {
            //This code executes during the editor... which means Start() doesn't happen
            spriteRenderer = GetComponent<SpriteRenderer>();

            //Calculate the two lower points
            float yValue = spriteRenderer.bounds.min.y + offsetY;
            Vector2 bottomLeft = new Vector2(spriteRenderer.bounds.min.x, yValue);
            Vector2 bottomRight = new Vector2(spriteRenderer.bounds.max.x, yValue);

            //Draw the line
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(bottomLeft, bottomRight);
        }
    } //end OnDrawGizmos()



    //Built-In method that runs after ALL updates are finished
    public virtual void LateUpdate()
    {
        checkHealth();
    }

    #region Attacks
    //This is called by the attack animation.
    public void EndAttackAnimation()
    {
        isAttacking = false;
    }

    //This is also called by the attack animation.
    // Be careful: Due to the nature blend tree, this method can be called twice, almost simulatanously
    public void SpawnMeleeAttack()
    {
        if (attackCooldownTicker <= 0)
        {
            //Calculate the angle to spawn the sword slash
            float angle = Mathf.Atan2(toMouse.y, toMouse.x) * Mathf.Rad2Deg;

            //Instantiate the Sword Slash Prefab
            GameObject tempSwordSlash = Instantiate(swordAttackPrefab, transform.position, Quaternion.Euler(0, 0, angle));
            tempSwordSlash.GetComponent<SwordSlash>().player = this.gameObject;

            //Set attack cooldown
            attackCooldownTicker = meleeAttackCooldown;

            //[Extra] Play Melee Sound Effect
            sounds.PlayOneShot(sounds.audio_meleeAttack);
        }
    }

    //Check for Melee Attack Inputs
    private void UpdateMeleeAttack()
    {
        //Check for Melee Attack
        if (Input.GetButtonDown("Fire1") && !isAttacking && !isStunned && attackCooldownTicker <= 0)
        {
            //Tell animation to play
            //This is important, because the animator will trigger two functions:
            // spawnMeleeHitbox() at the start of the second frame
            // endAttackAnimation() at the end of the second frame
            animator.SetTrigger("Attack");
            isAttacking = true;

            Destroy(chargeBar);
            isCharging = false;
            rangedChargeLevel = 0;
        }
    } //end UpdateMeleeAttack()

    //Check for Range Attack Inputs
    private void UpdateRangedAttack()
    {
        //Check for Ranged Attack   
        if (Input.GetButtonDown("Fire2") && !isAttacking && !isStunned && attackCooldownTicker <= 0)
        {
            //
            //Button is First Pressed
            //
            if (mana.currentValue >= rangedManaCost[0])
            {
                isCharging = true;
                rangedChargeTime = 0;
                rangedChargeLevel = 0;
                //Update animation
                //Comment out if you want to "appear to walk" as you charge
                animator.SetTrigger("Charging");

                //Charge Bar: Create the bar
                chargeBar = Instantiate(chargeBarPrefab);
                chargeBar.GetComponent<ChargeBar>().Setup(this.gameObject, uiCanvas.GetComponent<RectTransform>());
                chargeBar.GetComponent<ChargeBar>().ChangeColors(chargeBarRangedColors[rangedChargeLevel + 1], chargeBarRangedColors[rangedChargeLevel]);
                chargeBar.GetComponent<ChargeBar>().ChangeFill(0);

                //Give the bar to the canvas as a parent
                chargeBar.transform.SetParent(uiCanvas.GetComponent<RectTransform>(), false);

                //[Extra] Play Magic Charge Sound (Passive)
                sounds.PlayPassive(sounds.audio_magicChargePassiveLevel[rangedChargeLevel]);

            }
            else
            {
                //Play Sound Effect for not enough mana?
            }
        }
        else if (Input.GetButton("Fire2") && isCharging)
        {
            //
            //Button is Being Held Down
            //
            //Dynamically check current charge level vs the total size of the arrays
            if (rangedChargeLevel < rangedTimeGoal.Length && mana.currentValue >= rangedManaCost[rangedChargeLevel])
            {
                //If you are not max rank and have enough mana to go to the next rank
                //Add time to the charge
                rangedChargeTime += Time.deltaTime;

                //Charge Bar: Update the Fill
                chargeBar.GetComponent<ChargeBar>().ChangeFill(rangedChargeTime, rangedTimeGoal[rangedChargeLevel]);

                //Check to see if you've reach the next charge level
                if (rangedChargeTime >= rangedTimeGoal[rangedChargeLevel])
                {
                    //Increase the charge level
                    rangedChargeLevel++;
                    //Reset time to 0
                    rangedChargeTime = 0;

                    //Charge Bar: Update the Colors to Next Level
                    if (rangedChargeLevel == rangedTimeGoal.Length) //If you're max level
                        chargeBar.GetComponent<ChargeBar>().ChangeColors(chargeBarRangedColors[rangedChargeLevel], chargeBarRangedColors[rangedChargeLevel]);
                    else
                        chargeBar.GetComponent<ChargeBar>().ChangeColors(chargeBarRangedColors[rangedChargeLevel + 1], chargeBarRangedColors[rangedChargeLevel]);

                    //Reset Bar to zero:
                    chargeBar.GetComponent<ChargeBar>().ChangeFill(0);

                    //[Extra] Play Magic Charge Sound (Passive)
                    sounds.Stop();
                    sounds.PlayPassive(sounds.audio_magicChargePassiveLevel[rangedChargeLevel]);
                    sounds.PlayOneShot(sounds.audio_magicChargeLevelUp);

                }
            }
        }

        //
        //Button is Just Released
        //
        if (Input.GetButtonUp("Fire2") && !isAttacking && isCharging && !isStunned && attackCooldownTicker <= 0)
        {
            //Tell animation to play
            animator.SetTrigger("Cast");
            isAttacking = true;
            isCharging = false;

            //Calculate the angle
            float angle = Mathf.Atan2(toMouse.y, toMouse.x) * Mathf.Rad2Deg;

            //Charge Bar: Destroy the Bar
            Destroy(chargeBar);

            //Attack if you have reached a charge level 1 or higher
            if (rangedChargeLevel > 0)
            {
                //Find the offset of the player's collider:
                Vector2 colliderOffset = GetComponent<CircleCollider2D>().offset * 0.9f;
                //Spawn the fireball
                GameObject tempFireball = Instantiate(rangedProjectile[rangedChargeLevel - 1], (Vector2)transform.position + colliderOffset, Quaternion.Euler(0, 0, angle));
                attackCooldownTicker = rangedAttackCooldown;

                //Deduct mana from player
                mana.currentValue -= rangedManaCost[rangedChargeLevel - 1];
                UpdateUI();

                //Charge Bar: Spawn a Bar for Magic Cooldown
                chargeBar = Instantiate(chargeBarPrefab);
                chargeBar.GetComponent<ChargeBar>().Setup(this.gameObject, uiCanvas.GetComponent<RectTransform>());
                chargeBar.GetComponent<ChargeBar>().ChangeColors(Color.blue, Color.black);
                chargeBar.GetComponent<ChargeBar>().ChangeFill(1);
                chargeBar.transform.SetParent(uiCanvas.GetComponent<RectTransform>(), false);
                attackCooldownTickerMax = rangedAttackCooldown;
            }

            //[Extra] Play Magic Launch Sound
            sounds.Stop();
            sounds.PlayOneShot(sounds.audio_magicChargeLaunchLevel[rangedChargeLevel]);

            //Clear out state of the ranged attack
            rangedChargeTime = 0;
            rangedChargeLevel = 0;


        }
    } //end UpdateRangedAttack()

    #endregion

    #region Health
    protected void checkHealth()
    {
        if (health.currentValue <= 0)
        {
            //Delete the Player
            Destroy(this.gameObject);
        }
    }

    public void Hit(int incomingDamage, Vector2 forceDirection)
    {
        //Subtract health (we'll check for death in update())
        health.currentValue -= incomingDamage;

        //Apply the knockback force
        body.AddForce(forceDirection, ForceMode2D.Impulse);

        //Make it stunned and flashy
        isStunned = true;
        //Stun for max of 2s or 0.5s (a hit for 10 damage stuns for 0.2s second)
        isStunnedTicker = Mathf.Max((float)incomingDamage / health.maxValue * 2f, 0.5f);
        //Maybe we should calculate the duration instead of just 3.0 seconds of flashing?
        hitFlashDuration = 3.0f;

        //Tell animator to play "Hurt" animation
        if (animator != null)
            animator.SetBool("isHurt", true);

        //Disable attack
        isAttacking = false;

        //[Extra] Stop any charging sound effect
        if (isCharging)
            sounds.Stop();

        UpdateUI();
        //Extra Vidoes Stuff: Disable charging
        isCharging = false;

        if (chargeBar != null)
            Destroy(chargeBar);

        //[Extra] Play hurt sound
        sounds.PlayOneShot(sounds.audio_hurt);

    }  //end Hit()
    #endregion
    public void HitFreeze(int incomingDamage, Vector2 forceDirection)
    {
        //Subtract health (we'll check for death in update())
        health.currentValue -= incomingDamage;
        isFreeze = true;
        moveSpeed -= .25f;
        //Apply the knockback force
        body.AddForce(forceDirection, ForceMode2D.Impulse);

        //Make it stunned and flashy
        isStunned = true;
        //Stun for max of 2s or 0.5s (a hit for 10 damage stuns for 0.2s second)
        isStunnedTicker = Mathf.Max((float)incomingDamage / health.maxValue * 5f, 0.5f);
        //Maybe we should calculate the duration instead of just 3.0 seconds of flashing?
        hitFlashDuration = 3.0f;

        //Tell animator to play "Hurt" animation
        if (animator != null)
            animator.SetBool("isHurt", true);

        //Disable attack
        isAttacking = false;

        //[Extra] Stop any charging sound effect
        if (isCharging)
            sounds.Stop();

        UpdateUI();
        //Extra Vidoes Stuff: Disable charging
        isCharging = false;

        if (chargeBar != null)
            Destroy(chargeBar);

        //[Extra] Play hurt sound
        sounds.PlayOneShot(sounds.audio_hurt);

    }  //end Hit()
    #region Update Behaviors
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
                    hitFlashTicker = .05f;
                }
                else if(isFreeze == true)
                {
                    spriteRenderer.color = Color.blue;
                    hitFlashTicker = .05f;
                    isFreeze = false;
                }
                else
                {
                    //Currently white, turn red
                    spriteRenderer.color = Color.red;
                    hitFlashTicker = .05f;
                }
            }

        } //end if hitFlashDuration >= 0

    } //end UpdateHitFlash()

    protected void UpdateStunnedState()
    {
        //Countdown the stun timer
        if (isStunnedTicker >= 0)
            isStunnedTicker -= Time.deltaTime;

        //Unstun if either the stunned timer is up
        if (isStunned && isStunnedTicker < 0)
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

    private void UpdateCooldowns()
    {
        //Cooldown Updates
        if (attackCooldownTicker > 0)
        {
            attackCooldownTicker -= Time.deltaTime;

            //Charge Bar: Cooldown of Attack
            if (chargeBar != null)
                chargeBar.GetComponent<ChargeBar>().ChangeFill(attackCooldownTicker, attackCooldownTickerMax);

            if (attackCooldownTicker <= 0 && chargeBar != null)
            {
                Destroy(chargeBar);
            }

        }

        //Mana Regeneration
        if (!isAttacking && !isCharging && !isStunned && attackCooldownTicker <= 0) //Only regenerate when not attacking
        {
            manaRegenerateTicker -= Time.deltaTime;

            if (manaRegenerateTicker <= 0)
            {
                //add one mana
                if (mana.currentValue < mana.maxValue)
                    mana.currentValue += 1;
                else
                    mana.currentValue = mana.maxValue;

                //reset the timer
                manaRegenerateTicker = 1.0f / manaPerSecond;
                UpdateUI();
            }
        }
    } //end UpdateCooldowns()

    //This method is to be used during Update to send info the animator
    private void UpdateAnimations()
    {
        //Send movement Info to Animator
        animator.SetFloat("Horizontal", movement.x);
        animator.SetFloat("Vertical", movement.y);
        float speed = movement.magnitude; //Slightly faster: movement.sqrMagnitude
        animator.SetFloat("Speed", speed);
        //Face the Mouse
        //If not moving or attacking, face the mouse
        if (speed < 0.01 || isAttacking)
        {
            animator.SetFloat("Horizontal", toMouse.x);
            animator.SetFloat("Vertical", toMouse.y);
        }
    } //end UpdateAnimations
    #endregion

    //Prevent the player from pushing monsters
    private void OnCollisionEnter2D(Collision2D other)
    {
        if (!isStunned && other.gameObject.CompareTag("Enemy"))
        {
            body.velocity = Vector2.zero;
            other.gameObject.GetComponent<Rigidbody2D>().velocity = Vector2.zero;
        }
    }

    public void Restore(int healthAdded)
    {
        health.currentValue += healthAdded;
        moveSpeed = 5.0f;
        UpdateUI();
    }
} //end Class Player
