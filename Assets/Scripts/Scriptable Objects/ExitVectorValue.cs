using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "My Scriptable Objects/Exit Vector Value")]
public class ExitVectorValue : ScriptableObject, ISerializationCallbackReceiver
{
    public Vector2 spawningPosition;
    public Vector2 defaultSpawningPosition;

    public SceneTransition.ExitDirection exit;
    public SceneTransition.ExitDirection defaultExit;

    public void reset()
    {
        spawningPosition = defaultSpawningPosition;
        exit = defaultExit;
    }

    //When it is unloaded from memory (even during play)
    //Triggered when play is stopped or if the new scene does not reference this script
    public void OnAfterDeserialize()
    {
        reset();
    }


    public void OnBeforeSerialize()
    {

    }
}
