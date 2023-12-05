using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class GameController : MonoBehaviour
{

    //Instance variables
    //Scriptable Objects
    //By keeping them here, they won't reset when you go to a map that doesn't have them
    public DataDictionary data;
    public IntValue playerHealth;
    public IntValue playerMana;
    public ExitVectorValue playerLocation;

    //Game Management Stuff
    //Prefabs
    public Text gameOverText;

    //Variables
    public bool gameOver = false;

    // Update is called once per frame
    void Update()
    {
        //Check to see if the player is dead
        if (playerHealth.currentValue <= 0 && gameOver == false)
        {
            //Spawn the game over text and make the  Canvas its parent
            Text textTemp = Instantiate(gameOverText);
            textTemp.transform.SetParent(GameObject.Find("Canvas").GetComponent<RectTransform>(), false);

            //So we know the state of game
            gameOver = true;
        }

        //If the player is dead, then restart if they press Enter
        if (gameOver == true && Input.GetKeyUp("return"))
        {
            //Reset Player Health & Mana
            playerHealth.reset();
            playerMana.reset();

            //Reset Player Location
            playerLocation.reset();

            //Reset the Data Dictionary
            data.reset();

            //Reload the first map
            SceneManager.LoadScene(0);
        }

        //Allow the player to quit the game
        if (Input.GetKeyUp("escape"))
        {
            Application.Quit();
        }
    } //end Update()
}
