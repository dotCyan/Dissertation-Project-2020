using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BushRustle : MonoBehaviour {
    AudioSource source;

	// Use this for initialization
	void Start () {
        source = this.GetComponent<AudioSource>();
	}

    private void OnTriggerEnter(Collider other)
    {
        source.PlayOneShot(source.clip);
        //Debug.Log("Entered collider");
    }

    private void OnTriggerExit(Collider other)
    {
        source.PlayOneShot(source.clip);
        //Debug.Log("Exit collider");
    }


}
