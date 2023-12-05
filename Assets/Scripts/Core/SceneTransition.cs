using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneTransition : MonoBehaviour
{
    [Tooltip("[REQUIRED] The name of scene to load without quote.")]
    public string sceneToLoad;

    [Tooltip("Exiting Point: set x and y\nExiting East&West: set x to 0, y to exit on other scene y\nExiting: North&South: set y to 0, x to exit on other scene")]
    public Vector2 playerDestinationPosition;

    [Tooltip("How far from the edge should the player spawn.")]
    public float offset = 0.1f;

    [Tooltip("What side is the player existing from.")]
    public ExitDirection exitSide;

    [Tooltip("(Don't change) Link to the Scriptable Object of Player Position.")]
    public ExitVectorValue playerLocationStorage;

    //enums are kinda like ints, but you use their name instead
    public enum ExitDirection
    {
        Point,  //0
        East,   //1
        North,  //2
        West,   //3
        South   //4
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            //Store this script's target location into the "permanment" scriptable object
            playerLocationStorage.spawningPosition = playerDestinationPosition;

            //Load the new scene
            SceneManager.LoadScene(sceneToLoad);

        }
    }

}
