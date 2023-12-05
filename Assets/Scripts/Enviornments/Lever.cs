using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Lever : MonoBehaviour, Interactable
{
    //Instance Variables
    [Header("Data")]
    public DataDictionary data;

    [Tooltip("What is the name of the lever system (shared with anything affected by it)?")]
    public string leverName = "Default Lever";

    [Tooltip("What is the initial state of the switch (Must match for the system)?")]
    public bool isActivated = false;

    [Header("Sprites")]
    [Tooltip("The isActivated == false image")]
    public Sprite leverLeftImage; //The isActivated = false image 
    [Tooltip("The isActivated == true image")]
    public Sprite leverRightImage; //The isActivated = true image

    //Components and Stuff
    protected SpriteRenderer spriteRenderer;
    protected float offsetY = -0.3f;
    //[Extra] Audio
    protected SoundPlayer sounds;


    // Start is called before the first frame update
    void Start()
    {
        //Attach Componenets
        spriteRenderer = GetComponent<SpriteRenderer>();
        //[Extra] Audio
        sounds = GetComponent<SoundPlayer>();

        //Note: The value must be between -32768 and 32767.
        //Calculate Layer order
        //Using these values our max level size is 300x300 tiles
        spriteRenderer.sortingOrder = 30000 - (int)((spriteRenderer.bounds.min.y + offsetY) * 100);

        //Figure out the initial state
        DataDictionary.InitializeCheck(data);

        //Check to see if the Data Dictionary has an entry for this switch system yet
        if (data.dataBoolean.ContainsKey(leverName))
        {
            //The switch name is found
            // Then load the current state from memory
            isActivated = data.dataBoolean[leverName];
        }
        else
        {
            //The switch name is NOT found
            // set the memory of the switch's state to the current setting
            data.dataBoolean[leverName] = isActivated;
        }

        UpdateSprite();

    }

    // Update is called once per frame
    void Update()
    {
        //Check to see if the current state of the switch matches what's in memory
        if (isActivated != data.dataBoolean[leverName])
        {
            //if it doesn't match, set this switch to what's seen in memory
            // (this is useful for if you have two linked switches on the same map)
            isActivated = data.dataBoolean[leverName];
            UpdateSprite();
        }
    }

    protected void UpdateSprite()
    {
        if (isActivated == false)
            spriteRenderer.sprite = leverLeftImage;
        else if (isActivated == true)
            spriteRenderer.sprite = leverRightImage;
    }

    public bool IsActive()
    {
        return isActivated;
    } //end isActive()

    public void Trigger()
    {
        //Switch the state of isActivated
        // false -> true  OR  true -> false
        isActivated = !isActivated;

        //Update the Data Dictionary
        data.dataBoolean[leverName] = isActivated;

        //Update the graphic
        UpdateSprite();

        //[Extra] Audio
       // sounds.PlayOneShot(sounds.audio_activate);

    } //end Trigger()
} //end class
