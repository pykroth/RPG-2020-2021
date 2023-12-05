using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TreeTransition : MonoBehaviour
{
    protected float hitFlashTicker = -0.1f;

    protected SpriteRenderer spriteRenderer;
    protected Rigidbody2D body;
    protected Animator animator;
    public int healthMax = 30;
    protected int health;
    protected float hitFlashDuration = -0.1f;
    // Start is called before the first frame update
    void Start()
    {
        //Link Components
        body = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        animator = GetComponent<Animator>();

        //Initialize Important Variables
        health = healthMax;
    }

    // Update is called once per frame
    void Update()
    {
        UpdateHitFlash();
        CheckHealth();
      
        //Debug.Log(health);
    }


    public void Hit(int incomingDamage)
    {
        //Subtract health (we'll check for death in update())
        health -= incomingDamage;

       
        hitFlashDuration = 3.0f;

    


    }  //end Hit()
    protected virtual void CheckHealth()
    {
        if (health <= 20 && health > 0 )
        {

            animator.SetBool("IsCut", true);




        }

        if (health<=0)
        {
            Destroy(this.gameObject);
        }
    }
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
}
