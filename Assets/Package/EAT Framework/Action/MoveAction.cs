using System;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AI;

namespace HenryLab
{
    /// <summary>
    /// Player Move Action
    /// </summary>
    public class MoveAction : BaseAction
    {
        private NavMeshAgent _agent;
        private float _speed;
        private Vector3 _destination;

        public MoveAction(NavMeshAgent agent, float speed, Vector3 destination)
        {
            Name = "MoveAction";
            _agent = agent;
            _speed = speed;
            _destination = destination;
        }

        public override async Task Execute()
        {
            await base.Execute();
            NavMeshPath path = new NavMeshPath();

            _agent.SetDestination(_destination);
            _agent.speed = _speed;
            while(_agent && _agent.isActiveAndEnabled && _agent.isOnNavMesh &&
                   (_agent.pathPending || _agent.remainingDistance > _agent.stoppingDistance))
            {
                await Task.Yield();
            }

            if(!NavMesh.CalculatePath(_agent.transform.position, _destination, NavMesh.AllAreas, path) ||
                path.status != NavMeshPathStatus.PathComplete)
            {
                Debug.LogWarning($"{Str.Tags.HeuristicBugTag}Destination: {_destination} is not reachable on the NavMesh.");
            }
        }
    }
}