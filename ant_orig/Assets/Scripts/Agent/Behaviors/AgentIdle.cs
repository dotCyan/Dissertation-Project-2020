using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class AgentIdle : StateMachineBehaviour
{
    public float maxDistance = 10.0f;
    public float minDistance = 5.0f;
    public float maxWaitTime = 5.0f;
    public float minWaitTime = 3.0f;
    public float distanceOffset = 0.01f;
    private float currentTime;
    private float distance;
    private float angle;
    private bool triggered;
    private NavMeshAgent agent;

    public override void OnStateEnter(Animator animator, AnimatorStateInfo animatorStateInfo, int layerIndex) 
    {
        agent = animator.gameObject.GetComponent<NavMeshAgent>();
        rollTime();
    }

    public override void OnStateUpdate(Animator animator, AnimatorStateInfo animatorStateInfo, int layerIndex) 
    {
        currentTime -= Time.deltaTime;
        if(currentTime <= 0 && !triggered)
        {
            rollDestination();
            Vector3 rawDestination = animator.transform.position + Quaternion.Euler(0, angle, 0) * animator.transform.forward * distance;
            agent.SetDestination(adjustedPosition(rawDestination));
            //Debug.Log(animator.gameObject.name + " destination: " + agent.destination);
            float distanceSqr = (agent.destination - animator.transform.position).sqrMagnitude;
            if(distanceSqr > distanceOffset * distanceOffset)
            {
                animator.SetFloat("distance", distanceSqr);
                triggered = true;
            }
        }
        //Debug.Log("State update");
    }

    public override void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex) 
    {

    }

    private Vector3 adjustedPosition(Vector3 position)
    {
        RaycastHit hit;
        LayerMask mask = ~(1 << 9 | 1 << 12);
        if (Physics.Raycast(new Vector3(position.x, 1000.0f, position.z), Vector3.down, out hit, Mathf.Infinity, mask))
        {
            return hit.point + hit.normal * Mathf.Sqrt(Mathf.Abs(agent.baseOffset));
        }
        return position;
    }

    private void rollTime()
    {
        currentTime = Mathf.Max(minWaitTime, Random.value * maxWaitTime);
        triggered = false;
    }

    private void rollDestination() 
    {
        distance = Mathf.Max(minDistance, Random.value * maxDistance);
        angle = Random.value * 360.0f;

    }
}
