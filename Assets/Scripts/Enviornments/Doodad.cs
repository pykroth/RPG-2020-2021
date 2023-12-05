using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Doodad : MonoBehaviour
{

    public bool debugDrawGizmos = false;
    [Tooltip("Positive Numbers make this object more likely to be behind things.")]
    public float offsetY;
    [Tooltip("Does this doodad block projectiles and flying things?")]
    public bool blockProjectiles;

    //Internal Variables
    private SpriteRenderer spriteRenderer;

    // Start is called before the first frame update
    void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    // Update is called once per frame
    void Update()
    {
        //Note: The value must be between -32768 and 32767.
        //Calculate Layer order
        //Using these values our max level size is 300x300 tiles
        spriteRenderer.sortingOrder = 30000 - (int)((spriteRenderer.bounds.min.y + offsetY) * 100);
    } //end Update()

    public bool isTall()
    {
        return blockProjectiles;
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
}
