using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "My Scriptable Objects/Data Dictionary")]
public class DataDictionary : ScriptableObject, ISerializationCallbackReceiver
{
    //Create a dictionary that links a string to a boolean
    // "lever1" -> true
    // "Overworld:Orange Blob (1)" -> true //true means "defeated"
    // "hasUpgradedFireball" -> true
    public Dictionary<string, bool> dataBoolean;

    public void reset()
    {
        dataBoolean = new Dictionary<string, bool>();
    }

    //Do this after unloaded from memory
    public void OnAfterDeserialize()
    {
        reset();
    }

    public void OnBeforeSerialize()
    {

    }

    public static bool InitializeCheck(DataDictionary data)
    {
        //Does the Entire dictionary object even exist (if not, we're definitely in trouble)
        if (data == null)
            return false;

        //Check for the boolean dictionary, if it doesn't exist create a new one
        if (data.dataBoolean == null)
            data.dataBoolean = new Dictionary<string, bool>();


        return true; //It was already existing
    }
}
