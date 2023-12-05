using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ChargeBar : MonoBehaviour
{
    public RectTransform barBack; //We need RectTransform because we will be moving this
    public RectTransform barFront;
    [Tooltip("Set the y-value to be above the objectToFollow's head.")]
    public Vector2 positionCorrection = new Vector2(0, 0.75f); //Placing this 0.75 units above the player

    private RectTransform targetCanvas;
    private GameObject objectToFollow;  //Usually the player


    // Start is called before the first frame update
    void Start()
    {
        //objectToFollow = GameObject.FindGameObjectWithTag("Player");
    }

    // Update is called once per frame
    void Update()
    {
        RepositionBar();
    }

    //Initializes the Bar to follow the target
    public void Setup(GameObject target, RectTransform canvas)
    {
        objectToFollow = target;
        targetCanvas = canvas;
    }

    public void ChangeColors(Color foreground, Color background)
    {
        barBack.GetComponent<Image>().color = background;
        barFront.GetComponent<Image>().color = foreground;
    }

    public void ChangeFill(float ratio)
    {
        barFront.GetComponent<Image>().fillAmount = ratio;
    }

    public void ChangeFill(float current, float max)
    {
        barFront.GetComponent<Image>().fillAmount = current / max;
    }

    //Follow the target around
    private void RepositionBar()
    {
        //Calculate the desired location to be
        Vector2 ViewportPosition = Camera.main.WorldToViewportPoint(objectToFollow.transform.position + (Vector3)positionCorrection);

        //Convert into a UI Canvas position
        Vector2 WorldObject_ScreenPosition = new Vector2(
                                                (ViewportPosition.x * targetCanvas.sizeDelta.x) - (targetCanvas.sizeDelta.x * 0.5f),
                                                (ViewportPosition.y * targetCanvas.sizeDelta.y) - (targetCanvas.sizeDelta.y * 0.5f));

        //Actually set the position
        barBack.anchoredPosition = WorldObject_ScreenPosition;
    }
}
