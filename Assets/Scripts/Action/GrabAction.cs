using BNG;
using System;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AI;

namespace VRAgent
{
    /// <summary>
    /// ÍÏ×§¶¯×÷
    /// </summary>
    public class GrabAction : BaseAction
    {
        private HandController _handController;
        private NavMeshAgent _agent;
        private Vector3 _sceneCenter;
        private float _moveSpeed;
        private Func<Grabbable> _grabbableHandle;

        private Grabbable _grabbable;

        public GrabAction(HandController handController, NavMeshAgent agent, Vector3 sceneCenter, float moveSpeed, Func<Grabbable> grabbableHandle)
        {
            actionName = "GrabAction";
            _handController = handController;
            _grabbableHandle = grabbableHandle;
            _agent = agent;
            _sceneCenter = sceneCenter;
            _moveSpeed = moveSpeed;
        }

        public override async Task Execute()
        {
            _grabbable = _grabbableHandle();
            Grab();
            await Drag();
            Release();
        }

        private void Grab()
        {
            _handController.grabber.GrabGrabbable(_grabbable);
            _grabbable.GetComponent<GrabbableEntity>().OnGrabbed();
        }

        private async Task Drag()
        {
            Debug.Log($"Start dragging Objects: {_grabbable.name}");

            Vector3 randomPosition = _sceneCenter;
            int attempts = 0;
            int maxAttempts = 10;

            while(attempts < maxAttempts)
            {
                float randomOffsetX = UnityEngine.Random.Range(-1f, 1f) * 8f;
                float randomOffsetZ = UnityEngine.Random.Range(-1f, 1f) * 8f;
                randomPosition = _grabbable.transform.position + new Vector3(randomOffsetX, 1f, randomOffsetZ);
                NavMeshPath path = new NavMeshPath();

                if(NavMesh.CalculatePath(_grabbable.transform.position, randomPosition, NavMesh.AllAreas, path))
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

        private void Release()
        {
            _handController.grabber.TryRelease();
        }
    }
}