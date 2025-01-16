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
        private Func<IGrabbableEntity> _grabbableEntityHandle;
        private IGrabbableEntity _grabbableEntity;

        public GrabAction(HandController handController, NavMeshAgent agent, Vector3 sceneCenter, float moveSpeed, Func<IGrabbableEntity> grabbableEntityHandle)
        {
            actionName = "GrabAction";
            _handController = handController;
            _grabbableEntityHandle = grabbableEntityHandle;
            _agent = agent;
            _sceneCenter = sceneCenter;
            _moveSpeed = moveSpeed;
        }

        public override async Task Execute()
        {
            _grabbableEntity = _grabbableEntityHandle();
            Grab();
            await Drag();
            Release();
        }

        private void Grab()
        {
            _handController.grabber.GrabGrabbable(_grabbableEntity.Grabbable);
            _grabbableEntity.OnGrabbed();
        }

        private async Task Drag()
        {
            Debug.Log($"Start dragging Objects: {_grabbableEntity.Name}");

            Vector3 randomPosition = _sceneCenter;
            int attempts = 0;
            int maxAttempts = 10;

            while(attempts < maxAttempts)
            {
                float randomOffsetX = UnityEngine.Random.Range(-1f, 1f) * 8f;
                float randomOffsetZ = UnityEngine.Random.Range(-1f, 1f) * 8f;
                randomPosition = _grabbableEntity.Grabbable.transform.position + new Vector3(randomOffsetX, 1f, randomOffsetZ);
                NavMeshPath path = new NavMeshPath();

                if(NavMesh.CalculatePath(_grabbableEntity.Grabbable.transform.position, randomPosition, NavMesh.AllAreas, path))
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
            Debug.Log($"Finish dragging Objects: {_grabbableEntity.Name}");
        }

        private void Release()
        {
            _handController.grabber.TryRelease();
        }
    }
}