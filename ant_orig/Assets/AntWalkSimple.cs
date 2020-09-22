using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AntWalkSimple : MonoBehaviour {

    //[SerializeField] public static GameObject startPos;    

    //[SerializeField] public static GameObject endPos;

    //[SerializeField] public static float speed;

    public float Xpos;
    public float Zpos;
    public GameObject antDest;


    void Start () {
        Xpos = Random.Range(6, 12);
        Zpos = Random.Range(-9, 0);
        antDest.transform.position = new Vector3(Xpos, 0, Zpos);
        Debug.Log("Initial Log");
        Debug.Log("Xpos:  " + Xpos + "ZPos:   " + Zpos);
        StartCoroutine(RunRandomWalk());
	}

	void Update () {
        transform.LookAt(antDest.transform);
        transform.position = Vector3.MoveTowards(transform.position, antDest.transform.position, 0.02f);
	}

    IEnumerator RunRandomWalk()
    {
        yield return new WaitForSeconds(5);
        Debug.Log("Xpos:  " + Xpos + "ZPos:   " + Zpos);
        Xpos = Random.Range(6, 12);
        Zpos = Random.Range(-9, 0);
        antDest.transform.position = new Vector3(Xpos, 0, Zpos);
        StartCoroutine(RunRandomWalk());

    }
}
