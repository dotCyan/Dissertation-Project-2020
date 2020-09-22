using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class AgentWander : StateMachineBehaviour
{
    public float maxDistance = 5.0f;
    public float minDistance = 3.0f;
    public float changeDestRate = 0.4f;
    public float changeDestAngle = 120.0f;
    public float distanceOffset = 0.2f;
    private NavMeshAgent agent;
    private float distance;
    private float actualDistance;
    private float angle;
    private float changeDistancePercentage;
    private bool changeDestination;
    private bool isWandering;
    private Vector3 rawDestination;
    private Vector3 finalDestination;
    private Vector3 currentPosition;

    public override void OnStateEnter(Animator animator, AnimatorStateInfo animatorStateInfo, int layerIndex) 
    {
        agent = animator.gameObject.GetComponent<NavMeshAgent>();
        isWandering = true;
        actualDistance = animator.GetFloat("distance");
        roll();
    }

    public override void OnStateUpdate(Animator animator, AnimatorStateInfo animatorStateInfo, int layerIndex) 
    {
        currentPosition = animator.transform.position;
        if(!isWandering)
        {
            rawDestination = currentPosition + Quaternion.Euler(0, angle, 0) * animator.transform.forward * distance;
            finalDestination = adjustedPosition(rawDestination);
            agent.SetDestination(finalDestination);
            //Debug.Log("cal: " + finalDestination + ", agent: " + agent.destination);
            actualDistance = (agent.destination- currentPosition).sqrMagnitude;
            animator.SetFloat("distance", actualDistance);
            isWandering = true;
        }
        else
        {
            float remainingDistance = (agent.destination - currentPosition).sqrMagnitude;
            //Debug.Log(remainingDistance);

            if(changeDestination)
            {
                if(remainingDistance / actualDistance < changeDistancePercentage)
                {
                    //Debug.Log(animator.gameObject.name + "changed destination");
                    isWandering = false;
                    roll();
                }
            }
            else
            {
                if(remainingDistance <= distanceOffset * distanceOffset)
                {
                    //Debug.Log("final dest:" + finalDestination + ", current pos: " + currentPosition);
                    agent.SetDestination(currentPosition);
                }
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

    private void roll() 
    {
        if(Random.value <= changeDestRate)
        {
            changeDestination = true;
            distance = Mathf.Max(minDistance, Random.value * maxDistance);
            angle = Random.value * changeDestAngle - changeDestAngle / 2.0f;
            changeDistancePercentage = Random.value;
        }
        else 
        {
            changeDestination = false;
        }
    }
}
