using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class PredatorBehaviourController : MonoBehaviour
{
    public delegate void predatorDelegate(Transform target);
    public static predatorDelegate attackEvent;
    private static int instances = 0;
    private static bool initialization = false;
    private static bool hasPlayer = false;
    private static List<Transform> targets = new List<Transform>();
    public float maxDetectAngle = 120.0f;
    public float maxDetectDistance = 6.0f;
    public float attackDistance = 1.5f;
    public float attackCooldown = 3.0f;
    public Material matNotFound;
    public Material matFound;
    public bool displayDetectIndicator = false;
    public bool enableOnStart = false;
    private int targetIndex = -1;
    private bool coolingdown = false;
    private bool gameFail = false;
    private bool pause = false;
    private Transform agentModel;
    private Transform detectOrigin;
    private Animator animator;
    private NavMeshAgent agent;
    private GameObject detectIndicator;
    private MeshRenderer indicatorRenderer;
    private Vector3[] deltaP;

    void Awake() 
    {
        instances++;
        animator = this.GetComponent<Animator>();
        agent = this.GetComponent<NavMeshAgent>();
        agentModel = transform.GetChild(1);
        detectOrigin = transform.GetChild(3);
        detectIndicator = transform.GetChild(4).gameObject;
        indicatorRenderer = detectIndicator.GetComponent<MeshRenderer>();
    }

    // Start is called before the first frame update
    void Start()
    {
        if(!initialization)
        {
            GameObject player = GameObject.Find("OVRPlayerController");
            if(player)
            {
                hasPlayer = true;
                targets.Add(player.transform);
            }
            

            GameObject antsCluster = GameObject.Find("AgentAnts");
            for(int i = 0; i < antsCluster.transform.childCount; i++)
            {
                targets.Add(antsCluster.transform.GetChild(i));
            }

            initialization = true;
        }
        deltaP = new Vector3[targets.Count];

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
        behaviour.gameFailEvent += onGameFailed;
    }

    void OnDisable() {
        AgentManager.pauseAllEvent -= onAgentPause;
        AgentManager.resumeAllEvent -= onAgentResume;
        behaviour.gameFailEvent -= onGameFailed;
    }

    void OnDestroy() {
        instances--;
        if(instances <= 0)
        {
            attackEvent = null;
            targets.Clear();
            initialization = false;
        }
    }

    // Update is called once per frame
    void Update()
    {
        if(!pause)
        {
            for(int i = 0; i < targets.Count; ++i)
            {
                if(targets[i].gameObject.activeInHierarchy)
                {
                    if(hasPlayer && i == 0)
                    {
                        deltaP[i] = targets[i].position - detectOrigin.position;
                    }
                    else
                    {
                        //ant position offset
                        deltaP[i] = targets[i].position + targets[i].TransformDirection(0, 0.45f, 0) - detectOrigin.position;
                        //Debug.DrawLine(detectOrigin.position, targets[i].position + targets[i].TransformDirection(0, 0.45f, 0), Color.red, Time.deltaTime);
                    }
                }
            }

            if(isTargetFound(deltaP, out targetIndex))
            {
                if(!isInAttackDistance(deltaP[targetIndex]))
                {
                    agent.SetDestination(targets[targetIndex].position);
                    animator.SetFloat("distance", (agent.destination - transform.position).sqrMagnitude);
                }
                else
                {
                    if(!coolingdown)
                    {
                        //Debug.Log("attack");
                        agent.SetDestination(transform.position);
                        animator.SetTrigger("attackDistance");
                        StartCoroutine(cooldown());
                        if(attackEvent != null)
                            attackEvent.Invoke(targets[targetIndex]);
                    }
                }
                indicatorRenderer.material = matFound;
                animator.SetBool("foundTarget", true);
            } 
            else
            {
                indicatorRenderer.material = matNotFound;
                animator.SetBool("foundTarget", false);
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
        animator.ResetTrigger("attackDistance");
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
        LayerMask mask = ~(1 << 9 | 1 << 11 | 1 << 12);
        if (Physics.Raycast(agentModel.position , transform.TransformDirection(Vector3.down), out hitInfo, 5.0f, mask))
        {
            Quaternion nextRot = Quaternion.LookRotation(Vector3.Cross(hitInfo.normal,Vector3.Cross(transform.forward,hitInfo.normal)),hitInfo.normal);
            transform.rotation = nextRot;
        }
    }

    private bool isTargetFound(Vector3[] deltaP, out int targetIndex) 
    {
        float nearestDistanceSqr = float.MaxValue;
        targetIndex = -1;

        for(int i = 0; i < deltaP.Length; i++)
        {
            if((targets[i].tag != "Player" && targets[i].gameObject.activeInHierarchy) || (targets[i].tag == "Player" && !gameFail))
            {
                if(Vector3.Angle(detectOrigin.forward, deltaP[i]) <= maxDetectAngle / 2.0f) 
                {
                    RaycastHit hitInfo;
                    LayerMask mask = ~(1 << 9 | 1 << 11 | 1 << 12);
                    if (Physics.Raycast(detectOrigin.position , deltaP[i], out hitInfo, maxDetectDistance, mask))
                    {
                        //Debug.Log(hitInfo.collider.name);
                        if (hitInfo.collider.tag == "Player" || hitInfo.collider.tag == "Agent")
                        {
                            float distanceSqr = deltaP[i].sqrMagnitude;
                            if(distanceSqr < nearestDistanceSqr)
                            {
                                targetIndex = i;
                                nearestDistanceSqr = distanceSqr;
                            }
                        }
                    }
                }
            }
        }

        return (targetIndex != -1);
    }

    private bool isInAttackDistance(Vector3 deltaP) 
    {
        if(deltaP.sqrMagnitude <= attackDistance * attackDistance)
        {
            return true;
        }

        return false;
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

    private void onGameFailed()
    {
        gameFail = true;
    }
}
