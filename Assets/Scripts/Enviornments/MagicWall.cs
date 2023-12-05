using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MagicWall : MonoBehaviour
{
    //Instance Variables
    [Header("Data")]
    public DataDictionary data;

    [Tooltip("What is the name of the lever system (shared with anything affected by it)?")]
    public string leverName = "Default Lever";

    [Tooltip("Does this wall work opposite of the state?")]
    public bool isInverted = false;

    //Components
    protected SpriteRenderer spriteRenderer;
    protected Collider2D myCollider;

    // Start is called before the first frame update
    void Start()
    {
        //Attach Components
        spriteRenderer = GetComponent<SpriteRenderer>();
        myCollider = GetComponent<Collider2D>();

        DataDictionary.InitializeCheck(data);
    } //end Start

    // Update is called once per frame
    void Update()
    {
        //Get the state of the wall from the Data Dictionary
        //bool state = data.dataBoolean[leverName];
        bool state = false; //Gets updated by next line's method
        bool found = data.dataBoolean.TryGetValue(leverName, out state);

        //If it's an inverted block (opposite rules), flip the state
        if (isInverted)
            state = !state;

        if (state == true) //Switch is activated, blocks should disappear
        {
            spriteRenderer.enabled = false;
            myCollider.enabled = false;
        }
        else
        {
            //switch is not activated, so the blocks should appear
            spriteRenderer.enabled = true;
            myCollider.enabled = true;
        }

    } //end Update
}
