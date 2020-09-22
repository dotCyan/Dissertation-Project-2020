using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class AgentManager : MonoBehaviour
{
    public delegate void AgentManagerDelegate();
    public static event AgentManagerDelegate pauseAllEvent;
    public static event AgentManagerDelegate resumeAllEvent;
    private static int instances = 0;
    public GameObject prefabAnt;
    public GameObject prefabPredator;
    public float radius = 80.0f;
    public float noAgentRadius = 40.0f;
    public Transform randomSpawnOrigin;
    public Transform antCluster;
    public Transform predatorCluster;
    public List<AntBehaviourController> ants = new List<AntBehaviourController>();
    public List<PredatorBehaviourController> predators = new List<PredatorBehaviourController>();
    public bool randomSpawnMode = true;
    private List<Animator> animators = new List<Animator>();
    private List<NavMeshAgent> navMeshAgents = new List<NavMeshAgent>();

    void Awake()
    {
        instances++;
    }

    void OnEnable() 
    {
        behaviour.gameStartEvent += onGameResume;
        behaviour.gamePauseEvent += onGamePause;
        behaviour.gameResumeEvent += onGameResume;
        behaviour.gameSucceedEvent += onGamePause;
    }

    void OnDisable() 
    {
        behaviour.gameStartEvent -= onGameResume;
        behaviour.gamePauseEvent -= onGamePause;
        behaviour.gameResumeEvent -= onGameResume;
        behaviour.gameSucceedEvent -= onGamePause;    
    }

    void OnDestroy() 
    {
        instances--;
        if(instances <= 0)
        {
            pauseAllEvent = null;
            resumeAllEvent = null;
        }
    }

    void onGamePause()
    {
        if(pauseAllEvent != null)
            pauseAllEvent.Invoke();
    }

    void onGameResume()
    {
        if(resumeAllEvent != null)
            resumeAllEvent.Invoke();
    }

    // Start is called before the first frame update
    void Start()
    {
        int maxAmount = Mathf.Max(GameProgressionHelper.agentAntLevels[GameProgressionHelper.currentLevel], GameProgressionHelper.agentPredatorLevels[GameProgressionHelper.currentLevel]);
        for(int i = 0; i < maxAmount; i++)
        {
            if(i < GameProgressionHelper.agentAntLevels[GameProgressionHelper.currentLevel])
            {
                if(i >= ants.Count)
                {
                    GameObject newAnt = Instantiate(prefabAnt,Vector3.zero,Quaternion.identity);
                    newAnt.transform.parent = antCluster;
                    ants.Add(newAnt.GetComponent<AntBehaviourController>());
                }
                Vector3 warpPosition = (randomSpawnMode) ? randomPosition(false) : Vector3.zero;
                ants[i].transform.position = warpPosition;
                ants[i].GetComponent<NavMeshAgent>().Warp(warpPosition);
                ants[i].gameObject.SetActive(true);
            }
            if(i < GameProgressionHelper.agentPredatorLevels[GameProgressionHelper.currentLevel])
            {
                if(i >= predators.Count)
                {
                    GameObject newPredator = Instantiate(prefabPredator,Vector3.zero,Quaternion.identity);
                    newPredator.transform.parent = predatorCluster;
                    predators.Add(newPredator.GetComponent<PredatorBehaviourController>());
                }
                Vector3 warpPosition = (randomSpawnMode) ? randomPosition(true) : Vector3.zero;
                predators[i].transform.position = warpPosition;
                predators[i].GetComponent<NavMeshAgent>().Warp(warpPosition);
                predators[i].gameObject.SetActive(true);
            }
        }
    }

    private Vector3 randomPosition(bool isPredator)
    {
        float angle = Random.value * 360.0f;
        float distance = Mathf.Max(Random.value * radius, noAgentRadius);

        Vector3 randomVec = randomSpawnOrigin.position + Quaternion.Euler(0, angle, 0) * randomSpawnOrigin.forward * distance; 
        RaycastHit hit;
        LayerMask mask = ~(1 << 8 | 1 << 9);
        if (Physics.Raycast(new Vector3(randomVec.x, 100.0f, randomVec.z), Vector3.down, out hit, Mathf.Infinity, mask))
        {
            return hit.point + new Vector3(0, (!isPredator) ? 0.4f : 1.0f, 0);
        }
        return Vector3.zero;
    }
}
