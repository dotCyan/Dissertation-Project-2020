using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CupcakeManager : MonoBehaviour
{
    public delegate void cupcakeStateDelegate();
    public static event cupcakeStateDelegate notEnoughCupcakeEvent;
    public delegate void cupcakeEventDelegate(ItemCupcake sender, Transform attach);
    public static event cupcakeEventDelegate attachChangeEvent;
    public static event cupcakeEventDelegate fullCapacityEvent;
    public static event cupcakeEventDelegate itemDropEvent;
    public delegate void nestEventDelegate(NestTrigger sender, ItemCupcake item);
    public static event nestEventDelegate cupcakeCollectedEvent;
    public static event nestEventDelegate inappropriateTypeEvent;
    private static int instances = 0;
    public GameObject prefabCupcake;
    public float radius = 60.0f;
    public float noFoodRadius = 40.0f;
    public Transform randomSpawnOrigin;
    public Transform instanceCluster;
    public Transform spawnCluster;
    public List<ItemCupcake> cupcakeInstances = new List<ItemCupcake>();
    public List<Transform> spawns = new List<Transform>();
    public NestTrigger[] nests;
    public bool randomSpawnMode = true;
    public bool respawnMode = true;
    private int remaining;
    private List<bool> isAllocated = new List<bool>();
    private List<int> allocatedIndex = new List<int>();

    void Awake() {
        instances++;

        for(int i = 0; i < GameProgressionHelper.foodCountLevels[GameProgressionHelper.currentLevel]; ++i)
        {
            if(i >= cupcakeInstances.Count)
            {
                GameObject newFood = Instantiate(prefabCupcake,Vector3.zero,Quaternion.identity);
                newFood.transform.parent = instanceCluster;
                cupcakeInstances.Add(newFood.GetComponent<ItemCupcake>());
            }
            if(i >= spawns.Count)
            {
                GameObject newSpawn = new GameObject("Spawn");
                newSpawn.transform.parent = spawnCluster;
                spawns.Add(newSpawn.transform);
            }
            if(randomSpawnMode)
                spawns[i].transform.position = randomPosition();
            cupcakeInstances[i].warp(spawns[i].position);
            isAllocated.Add(true);
            allocatedIndex.Add(i);
            cupcakeInstances[i].gameObject.SetActive(true);
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        remaining = GameProgressionHelper.foodCountLevels[GameProgressionHelper.currentLevel];
        if(remaining < GameProgressionHelper.requiredFoodLevels[GameProgressionHelper.currentLevel])
        {
            if(notEnoughCupcakeEvent != null) notEnoughCupcakeEvent.Invoke();
        }
    }

    void OnEnable() 
    {
        int maxCount = Mathf.Max(cupcakeInstances.Count, nests.Length);
        for(int i = 0; i < maxCount; i++)
        {
            if(i < cupcakeInstances.Count)
            {
                cupcakeInstances[i].attachChangeEvent += onCupcakePickedUp;
                cupcakeInstances[i].fullCapacityEvent += onFullCapacity;
                cupcakeInstances[i].itemDropEvent += onCupcakeDropped;
            }

            if(i < nests.Length)
            {
                nests[i].cupcakeCollectedEvent += onCupcakeCollected;
                nests[i].inappropriateTypeEvent += onGetToWrongNest;
            }
        }
    }

    void OnDisable() 
    {
        for(int i = 0; i < nests.Length; i++)
        {
            if(i < cupcakeInstances.Count)
            {
                cupcakeInstances[i].attachChangeEvent -= onCupcakePickedUp;
                cupcakeInstances[i].fullCapacityEvent -= onFullCapacity;
                cupcakeInstances[i].itemDropEvent -= onCupcakeDropped;
            }

            if(i < nests.Length)
            {
                nests[i].cupcakeCollectedEvent -= onCupcakeCollected;
                nests[i].inappropriateTypeEvent -= onGetToWrongNest;
            }
        }
    }

    void OnDestroy() 
    {
        instances--;
        if(instances <= 0)
        {
            notEnoughCupcakeEvent = null;
            attachChangeEvent = null;
            fullCapacityEvent = null;
            itemDropEvent = null;
            cupcakeCollectedEvent = null;
            inappropriateTypeEvent = null;
        }
    }

    private void onCupcakePickedUp(ItemCupcake sender, Transform trigger)
    {
        if(attachChangeEvent != null)
            attachChangeEvent.Invoke(sender, trigger);
        
        for(int i = 0; i < cupcakeInstances.Count; ++i)
        {
            if(sender.Equals(cupcakeInstances[i]) && allocatedIndex[i] != -1)
            {
                isAllocated[allocatedIndex[i]] = false;
                allocatedIndex[i] = -1;
                return;
            }
        }
        //Debug.Log("Cupcake picked up");
    }

    private void onFullCapacity(ItemCupcake sender, Transform trigger)
    {
        if(fullCapacityEvent != null)
            fullCapacityEvent.Invoke(sender, trigger);
    }

    private void onCupcakeDropped(ItemCupcake sender, Transform trigger)
    {
        if(itemDropEvent != null)
            itemDropEvent.Invoke(sender, trigger);
    }

    private void onCupcakeCollected(NestTrigger sender, ItemCupcake item) 
    {
        if(cupcakeCollectedEvent != null)
            cupcakeCollectedEvent.Invoke(sender, item);

        item.detach();
        if(respawnMode)
        {
            allocateToRespawn(item);
        }
        else 
        {
            item.gameObject.SetActive(false);
            if(sender.GetComponent<NestTrigger>().type == NestTrigger.NestType.agent)
                remaining--;
            if(remaining < GameProgressionHelper.requiredFoodLevels[GameProgressionHelper.currentLevel])
            {
                if(notEnoughCupcakeEvent != null) notEnoughCupcakeEvent.Invoke();
            }
        }
        //Debug.Log("Cupcake collected");
    }

    private void onGetToWrongNest(NestTrigger sender, ItemCupcake item) 
    {
        if(inappropriateTypeEvent != null)
            inappropriateTypeEvent.Invoke(sender, item);
    }

    private Vector3 randomPosition()
    {
        float angle = Random.value * 360.0f;
        float distance = Mathf.Max(Random.value * radius, noFoodRadius);

        Vector3 randomVec = randomSpawnOrigin.position + Quaternion.Euler(0, angle, 0) * randomSpawnOrigin.forward * distance; 
        RaycastHit hit;
        LayerMask mask = ~(1 << 9);
        if (Physics.Raycast(new Vector3(randomVec.x, 100.0f, randomVec.z), Vector3.down, out hit, Mathf.Infinity, mask))
        {
            return hit.point + new Vector3(0, 0.1f, 0);
        }
        return Vector3.zero;
    }

    private void allocateToRespawn(ItemCupcake item)
    {
        for(int i = 0; i < cupcakeInstances.Count; ++i)
        {
            if(item.Equals(cupcakeInstances[i]))
            {
                for(int j = 0; j < spawns.Count; ++j)
                {
                    if(!isAllocated[j])
                    {
                        item.gameObject.SetActive(true);
                        item.warp(spawns[j].transform.position);
                        allocatedIndex[i] = j;
                        isAllocated[j] = true;
                        return;
                    }
                }
            }
        }
    }
}
