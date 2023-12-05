using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class SoundPlayer : MonoBehaviour
{

    //Sound Effect Clips
    public AudioClip audio_hurt, audio_death;
    public AudioClip audio_meleeAttack;
    public AudioClip[] audio_magicChargePassiveLevel, audio_magicChargeLaunchLevel;
    public AudioClip audio_magicChargeLevelUp, audio_magicExplode, audio_magicImpact, audio_magicPassThrough;
    public AudioClip audio_rangedLaunch, audio_rangedImpact;

    //Component
    private AudioSource sound;

    // Start is called before the first frame update
    void Start()
    {
        //Connect the audio source
        sound = GetComponent<AudioSource>();
    }


    //A OneShot audio is a clip that doesn't stop or interrupt other clips
    public bool PlayOneShot(AudioClip clip)
    {
        if (clip != null)
        {
            sound.PlayOneShot(clip);
            return true;
        }

        return false;
    }

    //Plays a audioclip (only one a time) only if audio is not currentlying playing something
    public bool PlayPassive(AudioClip clip)
    {
        if (clip != null && !sound.isPlaying)
        {
            sound.clip = clip;
            sound.Play();
            return true;
        }

        return false;
    }

    //Plays an audioclip (only one a time)
    public bool Play(AudioClip clip)
    {
        if (clip != null)
        {
            sound.clip = clip;
            sound.Play();
            return true;
        }

        return false;
    }

    public void Stop()
    {
        sound.Stop();
        sound.clip = null;
    }
}
