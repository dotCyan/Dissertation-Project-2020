using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ItemCupcake : MonoBehaviour
{
    public delegate void itemCupcakeDelegate(ItemCupcake sender, Transform trigger);
    public event itemCupcakeDelegate attachChangeEvent;
    public event itemCupcakeDelegate fullCapacityEvent;
    public event itemCupcakeDelegate itemDropEvent;
    public Vector3 spawnOffset = Vector3.zero;
    public bool isAttached 
    {
        get
        {
            return isattached;
        }
    }
    private float speed = 40.0f;
    private bool isattached = false;
    private Transform attach = null;
    private static Transform cupcakesCluster;
    private Quaternion origRotation;

    // Start is called before the first frame update
    void Start()
    {
        if(!cupcakesCluster)
        {
            cupcakesCluster = GameObject.Find("Cupcakes").transform;
        }   
    }

    // Update is called once per frame
    void Update()
    {
        if(!isattached) 
        {
            transform.Rotate(new Vector3(0,speed * Time.deltaTime, 0),Space.Self);
        }
    }

    void OnTriggerEnter(Collider other) 
    {
        if(!isattached)
        {
            Transform t = other.transform.Find("AttachPoint");
            if(t)
            {
                bool isFull = (t.childCount >= GameProgressionHelper.maxFoodCapacity);
                //Debug.Log("isFull: " + isFull);

                if(!isFull)
                {
                    this.attachTo(t);
                }
                else
                {
                    if(fullCapacityEvent != null) 
                        fullCapacityEvent.Invoke(this, t);
                }
            }
            //Debug.Log(other.name);
        }    
    }

    public void attachTo(Transform t)
    {
        if(!isattached)
        {
            origRotation = transform.rotation;
        }
        this.attach = t;
        transform.parent = t;
        transform.localPosition = Vector3.zero;
        transform.localRotation = Quaternion.identity;
        isattached = true;
        if(attachChangeEvent != null) 
            attachChangeEvent.Invoke(this, this.attach);
    }

    public Transform getAttachTo()
    {
        return attach;
    }

    public void detach()
    {
        if(isattached)
        {
            Transform prevAttach = this.attach;
            this.attach = null;
            isattached = false;
            transform.parent = cupcakesCluster;
            warp(transform.position - (prevAttach.parent.forward * 2.0f));
            transform.localRotation = origRotation;
            if(itemDropEvent != null) itemDropEvent.Invoke(this, prevAttach);
        }
    }

    public void warp(Vector3 position)
    {
        if(!isattached)
        {
            RaycastHit hit;
            LayerMask mask = ~(1 << 8 | 1 << 9 | 1 << 11 | 1 << 12);
            if (Physics.Raycast(new Vector3(position.x, 1000.0f, position.z), Vector3.down, out hit, Mathf.Infinity, mask))
            {
                transform.position = hit.point + spawnOffset;
            }
        }
    }
}
