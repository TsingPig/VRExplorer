using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AI;

namespace VRExplorer
{
    /// <summary>
    /// ÒÆ¶¯¶¯×÷
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

            _agent.SetDestination(_destination);
            _agent.speed = _speed;
            while(_agent && _agent.isActiveAndEnabled && _agent.isOnNavMesh &&
                   (_agent.pathPending || _agent.remainingDistance > _agent.stoppingDistance))
            {
                await Task.Yield();
            }
        }
    }
}