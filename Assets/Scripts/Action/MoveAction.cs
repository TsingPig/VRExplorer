using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AI;

namespace VRAgent
{
    /// <summary>
    /// ÒÆ¶¯¶¯×÷
    /// </summary>
    public class MoveAction : BaseAction
    {
        private NavMeshAgent _agent;
        private float _speed;

        public Vector3 destination;

        public MoveAction(NavMeshAgent agent, float speed, Vector3 destination = default)
        {
            actionName = "MoveAction";
            _agent = agent;
            _speed = speed;
            this.destination = destination;
        }

        public async Task Execute(Vector3 destination)
        {
            if(destination == null)
            {
                Debug.LogError($"None Destination For {actionName}, Please check the Destination Vector3");
                return;
            }
            this.destination = destination;
            await Execute();
        }

        public async override Task Execute()
        {
            _agent.SetDestination(destination);
            _agent.speed = _speed;
            while(_agent && _agent.isActiveAndEnabled && _agent.isOnNavMesh && (_agent.pathPending || _agent.remainingDistance > 0.5f))
            {
                await Task.Yield();
            }
        }
    }
}