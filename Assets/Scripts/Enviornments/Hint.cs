﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
public class Hint : MonoBehaviour
{
    public Text Hints;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
       if(Input.GetButtonUp("Use Button"))
        {
            Text textTemp = Instantiate(Hints);
            textTemp.transform.SetParent(GameObject.Find("Canvas").GetComponent<RectTransform>(), false);

        }
    }
}