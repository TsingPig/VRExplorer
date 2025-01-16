using BNG;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.AI;
namespace VRAgent
{
    public class VRAgent : BaseAgent
    {

        /// <summary>
        /// 获取最近的可抓取物体
        /// </summary>
        protected override void GetNextGrabbableEntity(out IGrabbableEntity nextGrabbableEntity)
        {
            nextGrabbableEntity = _grabbableEntities
                .Where(e => _grabbablesStates[e] == false)
                .OrderBy(e => Vector3.Distance(transform.position, e.transform.position))
                .FirstOrDefault();
        }

        protected override void GetNextTriggerableEntity(out ITriggerableEntity nextTriggerableEntity)
        {
            nextTriggerableEntity = _triggerableEntity
                .Where(e => _triggerablesStates[e] == false)
                .OrderBy(e => Vector3.Distance(transform.position, e.transform.position))
                .FirstOrDefault();
        }
    }
}

