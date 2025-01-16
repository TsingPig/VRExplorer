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
        private Vector3 _destination;
        private float _speed;

        public MoveAction(NavMeshAgent agent, Vector3 destination, float speed)
        {
            actionName = "MoveAction";
            _agent = agent;
            _destination = destination;
            _speed = speed;
        }

        public async override Task Execute()
        {
            _agent.SetDestination(_destination);
            _agent.speed = _speed;
            while(_agent && _agent.isActiveAndEnabled && _agent.isOnNavMesh && (_agent.pathPending || _agent.remainingDistance > 0.5f))
            {
                await Task.Yield();
            }
        }
    }


}