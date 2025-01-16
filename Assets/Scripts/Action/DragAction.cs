using System;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AI;

namespace VRAgent
{
    [Obsolete("拖拽动作已经被集成到抓取动作中")]
    public class DragAction : BaseAction
    {
        private NavMeshAgent _agent;
        private Transform _grabbable;
        private Vector3 _sceneCenter;
        private float _moveSpeed;

        public DragAction(NavMeshAgent agent, Transform grabbable, Vector3 sceneCenter, float moveSpeed)
        {
            actionName = "DragAction";
            _agent = agent;
            _grabbable = grabbable;
            _sceneCenter = sceneCenter;
            _moveSpeed = moveSpeed;
        }

        public override async Task Execute()
        {
            Debug.Log($"Start dragging Objects: {_grabbable.name}");

            Vector3 randomPosition = _sceneCenter;
            int attempts = 0;
            int maxAttempts = 10;

            while(attempts < maxAttempts)
            {
                float randomOffsetX = UnityEngine.Random.Range(-1f, 1f) * 8f;
                float randomOffsetZ = UnityEngine.Random.Range(-1f, 1f) * 8f;
                randomPosition = _grabbable.position + new Vector3(randomOffsetX, 0, randomOffsetZ);
                NavMeshPath path = new NavMeshPath();

                if(NavMesh.CalculatePath(_grabbable.position, randomPosition, NavMesh.AllAreas, path))
                {
                    if(path.status == NavMeshPathStatus.PathComplete)
                    {
                        break;
                    }
                }
                attempts++;
            }

            _agent.SetDestination(randomPosition);
            _agent.speed = _moveSpeed;

            while(_agent && _agent.isActiveAndEnabled && _agent.isOnNavMesh && (_agent.pathPending || _agent.remainingDistance > 0.5f))
            {
                await Task.Yield();
            }
            Debug.Log($"Finish dragging Objects: {_grabbable.name}");
        }
    }
}