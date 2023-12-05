using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class CameraFollow : MonoBehaviour
{
    [Tooltip("Optional: Attach Object to follow, usually the Player.")]
    public Transform followTransform;

    [Tooltip("REQUIRED: Attach the Ground Tilemap here. (Used to get the bounds)")]
    public Tilemap map;

    [Tooltip("[0.0 - 1.0]: How quickly to move to the player.  Higher numbers are faster movement.")]
    public float smoothSpeed = 0.5f;

    //Internal Variables
    public float xMin, xMax, yMin, yMax; //Will be edges of the world
    private float camX, camY;
    private float camOrthsize; //Zoom level (used for y-height of view)
    private float cameraRatio; //Aspect ratio (used for x-width of view)
    private Camera mainCam; //Reference to this
    private Vector3 smoothPos;


    // Start is called before the first frame update
    void Start()
    {
        if (followTransform != null)
        {
            //Immediately jump to the thing we are following                                        //Keep the camera's z
            transform.position = new Vector3(followTransform.position.x, followTransform.position.y, transform.position.z);
        }
        //Backup attempt to find the map
        if (map == null)
            map = GameObject.Find("Ground").GetComponent<Tilemap>();

        //Determine the edges of the world (using the map)
        xMin = map.localBounds.min.x;
        xMax = map.localBounds.max.x;
        yMin = map.localBounds.min.y;
        yMax = map.localBounds.max.y;
        Debug.Log("x: " + xMin + " to " + xMax + " y: " + yMin + " to " + yMax);

        //Define the rest of the variables
        mainCam = GetComponent<Camera>();
        camOrthsize = mainCam.orthographicSize;
        cameraRatio = mainCam.aspect * camOrthsize;


    } //end Start()

    private void FixedUpdate()
    {
        if (map != null && followTransform != null)
        {
            //Calculate the goal position of the camera but restrict it from going to the edge
            camX = Mathf.Clamp(followTransform.position.x, xMin + cameraRatio, xMax - cameraRatio);
            camY = Mathf.Clamp(followTransform.position.y, yMin + camOrthsize, yMax - camOrthsize);

            //Create the goal as a Vector3
            Vector3 cameraGoalPosition = new Vector3(camX, camY, transform.position.z);

            //Use Lerp to calculate a position between the starting position and the goal position.
            smoothPos = Vector3.Lerp(transform.position, cameraGoalPosition, smoothSpeed);

            //Set the Camera's Position:
            transform.position = smoothPos;
        }
    } //end FixedUpdate()
}
