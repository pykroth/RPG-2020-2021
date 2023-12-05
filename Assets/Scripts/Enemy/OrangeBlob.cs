using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OrangeBlob : Enemy
{


    // Update is called once per frame
    public override void Update()
    {
        //Call the Enemy's Update() method for all the generic monster stuff
        //base.Update() is like calling super.Update()
        base.Update();

        //Code here runs in Update() unique to the Orange Blob

        //Attacking Logic
        //Melee
        if (target != null && !isStunned && !isAttacking && attackCooldownTicker <= 0)
        {

            //Check the distance to the target
            Vector3 targetOffset = target.GetComponent<Collider2D>().offset;
            Vector3 myOffset = GetComponent<Collider2D>().offset;
            float distance = Vector2.Distance(target.position + targetOffset, transform.position + myOffset);

            //If the target is in range, start a melee attack
            if (distance <= meleeAttackRadius)
            {
                //The animation will call the Attack() method for the blob
                //The animation will call the EndAttack() method to exit isAttacking
                if (animator != null)
                    animator.SetTrigger("Attack");

                isAttacking = true;

            }
        }//end melee check

        //Ranged
        //None

        //Movement Stuff unique to the blob
        if (isAttacking)
            moveModifier *= 0.5f;

    } //end Update()

    private void FixedUpdate()
    {
        if (!isStunned)
        {
            Move();
        }
    }  //end FixedUpdate()

    //This is triggered by the animation
    public override void Attack()
    {
        PBAoEAttack();
    }
}
