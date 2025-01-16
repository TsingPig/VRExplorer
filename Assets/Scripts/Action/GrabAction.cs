using BNG;
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
        private Grabbable _grabbable;

        public GrabAction(HandController handController, Grabbable grabbable)
        {
            actionName = "GrabAction";
            _handController = handController;
            _grabbable = grabbable;
        }
        public void Grab()
        {
            _handController.grabber.GrabGrabbable(_grabbable);
            _grabbable.GetComponent<GrabbableEntity>().OnGrabbed();
        }

        public void Release()
        {
            _handController.grabber.TryRelease();
        }
    }

}