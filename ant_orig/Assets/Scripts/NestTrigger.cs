using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NestTrigger : MonoBehaviour
{
    public enum NestType
    {
        player,
        agent,
        both
    }
    public NestType type = NestType.both;
    public delegate void cupcakeCollectedDelegate(NestTrigger sender, ItemCupcake item);
    public event cupcakeCollectedDelegate cupcakeCollectedEvent;
    public event cupcakeCollectedDelegate inappropriateTypeEvent;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    void OnTriggerEnter(Collider other) 
    {
        ItemCupcake item = other.GetComponent<ItemCupcake>();

        if(item) 
        {
            //Debug.Log("Name: " + gameObject.name + ", Type: " + type + ", AttachType: " + item.getAttachTo().tag);
            if(type == NestType.player && item.getAttachTo().tag != "Player")
            {
                if(inappropriateTypeEvent != null) inappropriateTypeEvent.Invoke(this, item);
                return;
            }
            else if(type == NestType.agent && item.getAttachTo().tag != "Agent")
            {
                if(inappropriateTypeEvent != null) inappropriateTypeEvent.Invoke(this, item);
                return;
            }

            if(cupcakeCollectedEvent != null) cupcakeCollectedEvent.Invoke(this, item);
        }
        //Debug.Log(other.name);
    }
}
