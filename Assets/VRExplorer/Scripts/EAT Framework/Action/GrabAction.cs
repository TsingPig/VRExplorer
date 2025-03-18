using System.Collections.Generic;
using System.Threading.Tasks;

namespace VRExplorer
{
    using HandController = BNG.HandController;

    /// <summary>
    /// ÍÏ×§¶¯×÷
    /// </summary>
    public class GrabAction : BaseAction
    {
        private HandController _handController;
        private IGrabbableEntity _grabbableEntity;
        private List<BaseAction> _sequenceActions;

        public GrabAction(HandController handController, IGrabbableEntity grabableEntity, List<BaseAction> sequenceActions)
        {
            Name = "GrabAction";
            _handController = handController;
            _grabbableEntity = grabableEntity;
            _sequenceActions = sequenceActions;
        }

        public override async Task Execute()
        {
            await base.Execute();
            Grab();
            foreach(var subAction in _sequenceActions)
            {
                await subAction.Execute();
            }
            Release();
        }

        private void Grab()
        {
            EntityManager.Instance.TriggerState(_grabbableEntity, IGrabbableEntity.GrabbableState.Grabbed);
            _handController.grabber.GrabGrabbable(_grabbableEntity.Grabbable);
            _grabbableEntity.OnGrabbed();
        }

        private void Release()
        {
            _handController.grabber.TryRelease();
        }
    }
}