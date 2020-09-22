using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class AntBehaviourController : MonoBehaviour
{
    private static List<Transform> cupcakes = new List<Transform>();
    private static List<Transform> nests = new List<Transform>();
    private static int instances = 0;
    private static bool initialization = false;
    public float maxDetectAngle = 120.0f;
    public float maxDetectDistance = 6.0f;
    public float attackDistance = 1.5f;
    public float attackCooldown = 3.0f;
    public Material matNotFound;
    public Material matFoundFood;
    public Material matFoundAttachedFood;
    public bool displayDetectIndicator = false;
    public bool enableOnStart = false;
    private int targetIndex = -1;
    private int targetCost;
    private int targetNestIndex = 0;
    private bool getFood = false;
    private bool pause = true;
    private bool coolingdown = false;
    private bool foundFood = false;
    private bool foundByOwner = false;
    private Transform agentModel;
    private Transform attachPoint;
    private Transform detectOrigin;
    private ItemCupcake cupcake = null;
    private Vector3[] deltaP;
    private Animator animator;
    private NavMeshAgent agent;
    private GameObject detectIndicator;
    private MeshRenderer indicatorRenderer;

    void Awake()
    {
        instances++;
        animator = this.GetComponent<Animator>();
        agent = this.GetComponent<NavMeshAgent>();
        agentModel = transform.GetChild(1);
        detectOrigin = transform.GetChild(2);
        detectIndicator = transform.GetChild(3).gameObject;
        attachPoint = transform.GetChild(4);
        indicatorRenderer = detectIndicator.GetComponent<MeshRenderer>();
    }

    // Start is called before the first frame update
    void Start()
    {
        if(!initialization)
        {
            GameObject cupcakeCluster = GameObject.Find("Cupcakes");
            GameObject nestCluster = GameObject.Find("AgentNests");
            int cupcakeCount = cupcakeCluster.transform.childCount;
            int nestCount = nestCluster.transform.childCount;
            int maxCount = Mathf.Max(cupcakeCount, nestCount);
            for(int i = 0; i < maxCount; i++)
            {
                if(i < cupcakeCount)
                {
                    cupcakes.Add(cupcakeCluster.transform.GetChild(i));
                }

                if(i < nestCount)
                {
                    nests.Add(nestCluster.transform.GetChild(i));
                }
            }

            initialization = true;
        }
        deltaP = new Vector3[cupcakes.Count];

        generateIndicator();
        detectIndicator.SetActive(false);

        if(enableOnStart)
        {
            onAgentResume();
        }
        else
        {
            onAgentPause();
        }
    }

    void OnEnable() {
        AgentManager.pauseAllEvent += onAgentPause;
        AgentManager.resumeAllEvent += onAgentResume;
        CupcakeManager.cupcakeCollectedEvent += onFoodCollected;
        CupcakeManager.attachChangeEvent += onFoodAttachChange;
        PredatorBehaviourController.attackEvent += onPredatorAttack;
    }

    void OnDisable() {
        AgentManager.pauseAllEvent -= onAgentPause;
        AgentManager.resumeAllEvent -= onAgentResume;
        CupcakeManager.cupcakeCollectedEvent -= onFoodCollected;
        CupcakeManager.attachChangeEvent -= onFoodAttachChange;
        PredatorBehaviourController.attackEvent -= onPredatorAttack;
    }

    void OnDestroy() {
        instances--;
        if(instances <= 0)
        {
            cupcakes.Clear();
            nests.Clear();
            initialization = false;
        }
    }

    // Update is called once per frame
    void Update()
    {
        if(!pause)
        {
            for(int i = 0; i < cupcakes.Count; ++i)
            {
                if(cupcakes[i].gameObject.activeInHierarchy)
                {
                    deltaP[i] = cupcakes[i].position - detectOrigin.position;
                }
            }
        
            if(!getFood)
            {
                foundFood = isTargetFound(deltaP, out targetIndex, out targetCost);
                foundByOwner = (!foundFood) ? false : isFoundByOwner(cupcakes[targetIndex]);
                
                if(foundFood && !foundByOwner)
                {
                    if(targetCost == 1)
                    {
                        agent.SetDestination(cupcakes[targetIndex].position);
                        animator.SetFloat("distance", (agent.destination - transform.position).sqrMagnitude);
                        indicatorRenderer.material = matFoundFood;
                    }
                    else
                    {
                        if(deltaP[targetIndex].sqrMagnitude > attackDistance)
                        {
                            agent.SetDestination(cupcakes[targetIndex].position);
                            animator.SetFloat("distance", (agent.destination - transform.position).sqrMagnitude);
                        }
                        else
                        {
                            if(!coolingdown)
                            {
                                cupcakes[targetIndex].GetComponent<ItemCupcake>().attachTo(attachPoint);
                                StartCoroutine(cooldown());
                            }
                        }
                        indicatorRenderer.material = matFoundAttachedFood;
                    }
                    animator.SetBool("foodIsFound", true);
                } 
                else
                {
                    indicatorRenderer.material = matNotFound;
                    animator.SetBool("foodIsFound", false);
                }

                animator.SetBool("beingFound", foundByOwner);
                animator.SetInteger("cost",targetCost);
            }
            animator.SetFloat("moveSpeed", agent.velocity.sqrMagnitude);

            detectIndicator.SetActive(displayDetectIndicator);
        }

        modelRotation();
    }

    IEnumerator cooldown()
    {
        coolingdown = true;
        yield return new WaitForSeconds(attackCooldown);
        coolingdown = false;
    }

    private void generateIndicator()
    {
        int samples = 16;
        float angleStart = - maxDetectAngle / 360.0f * Mathf.PI;
        float angleEnd = - angleStart;
        Vector3[] newVertices = new Vector3[samples + 2]; 
        int[] newTriangles = new int[samples * 3];
        MeshFilter filter = detectIndicator.GetComponent<MeshFilter>();

        newVertices[0] = Vector3.zero;
        for(int i = 0; i <= samples; ++i)
        {
            float currentAngle = Mathf.Lerp(angleStart, angleEnd, (float)i/(float)samples) + Mathf.PI / 2.0f;
            
            newVertices[i + 1] = new Vector3(maxDetectDistance * Mathf.Cos(currentAngle), 0, maxDetectDistance * Mathf.Sin(currentAngle));
            if(i >= 1) {
                newTriangles[(i - 1) * 3] = i + 1;
                newTriangles[(i - 1) * 3 + 1] = i;
                newTriangles[(i - 1) * 3 + 2] = 0;
            }
        }
        Mesh mesh = new Mesh();
        mesh.name = "Indicator";
        mesh.vertices = newVertices;
        mesh.triangles = newTriangles;
        filter.mesh = mesh;
    }

    private void modelRotation()
    {
        RaycastHit hitInfo;
        LayerMask mask = ~(1 << 8 | 1 << 9 | 1 << 12);
        if (Physics.Raycast(agentModel.position , transform.TransformDirection(Vector3.down), out hitInfo, 5.0f, mask))
        {
            Quaternion nextRot = Quaternion.LookRotation(Vector3.Cross(hitInfo.normal,Vector3.Cross(transform.forward,hitInfo.normal)),hitInfo.normal);
            transform.rotation = nextRot;
        }
    }

    private bool isTargetFound(Vector3[] deltaP, out int index, out int cost) 
    {
        float nearest = float.MaxValue;
        index = -1;
        cost = 99;

        for(int i = 0; i < deltaP.Length; ++i)
        {
            if(cupcakes[i].gameObject.activeInHierarchy)
            {
                if(Vector3.Angle(detectOrigin.forward, deltaP[i]) <= maxDetectAngle / 2.0f) 
                {
                    RaycastHit hitInfo;
                    LayerMask mask = ~(1 << 8 | 1 << 9| 1 << 10| 1 << 11);
                    if (Physics.Raycast(detectOrigin.position , deltaP[i], out hitInfo, maxDetectDistance, mask))
                    {
                        if (hitInfo.collider.tag == "Food")
                        {
                            float sqrDistance = deltaP[i].sqrMagnitude;
                            int expectedCost = (cupcakes[i].GetComponent<ItemCupcake>().isAttached) ? 2 : 1;
                            if(expectedCost < cost || (expectedCost == cost && sqrDistance < nearest))
                            {
                                index = i;
                                nearest = sqrDistance;
                                cost = expectedCost;
                            }
                        }
                    }
                }
            }

        }

        return (index != -1);
    }

    private bool isFoundByOwner(Transform cupcake)
    {
        if(cupcake.GetComponent<ItemCupcake>().isAttached)
        {
            Transform owner = cupcake.parent.parent;
            float angle = Vector3.Angle(transform.forward, owner.forward);
            if(Mathf.Abs(angle) > 90.0f)
            {
                return true;
            }
        }
        return false;
    }

    private int nearestNest()
    {
        int nearestIndex = 0;
        float nearestSqr = Vector3.SqrMagnitude(nests[0].transform.position - transform.position);

        for(int i = 1; i < nests.Count; i++)
        {
            float distanceSqr = Vector3.SqrMagnitude(nests[i].transform.position - transform.position);
            if(distanceSqr < nearestSqr)
            {
                nearestIndex = i;
                nearestSqr = distanceSqr;
            }
        }
        return nearestIndex;
    }

    private void onFoodAttachChange(ItemCupcake sender, Transform currentAttach)
    {
        if(currentAttach.Equals(attachPoint))
        {
            cupcake = sender;
            targetNestIndex = nearestNest();
            agent.SetDestination(nests[targetNestIndex].position);
            animator.SetFloat("distance", (agent.destination - transform.position).sqrMagnitude);
            animator.SetBool("getFood", true);
            getFood = true;
        }
        else if(sender.Equals(cupcake))
        {
            cupcake = null;
            agent.SetDestination(transform.position);
            animator.SetBool("getFood", false);
            getFood = false;
        }
    }

    private void onFoodCollected(NestTrigger sender, ItemCupcake item)
    {
        if(getFood && sender.transform.Equals(nests[targetNestIndex]) && item.Equals(cupcake))
        {
            cupcake = null;
            agent.SetDestination(transform.position);
            animator.SetBool("getFood", false);
            getFood = false;
        }
        else if(!getFood)
        {
            if(targetIndex != -1 && item.Equals(cupcakes[targetIndex].GetComponent<ItemCupcake>()))
            {
                agent.SetDestination(transform.position);
            }
        }
    }

    private void onPredatorAttack(Transform target)
    {
        if(target.Equals(transform))
        {
            if(getFood)
            {
                cupcake.detach();
                cupcake = null;
                getFood = false;
            }
            pause = true;
            animator.enabled = false;
            gameObject.SetActive(false);
        }
    }

    private void onAgentPause()
    {
        pause = true;
        animator.enabled = false;
        agent.isStopped = true;
    }

    private void onAgentResume()
    {
        pause = false;
        animator.enabled = true;
        agent.isStopped = false;
    }
}
