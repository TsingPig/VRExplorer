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
        protected override void GetNextGrabbableEntity(out GrabbableEntity nextGrabbableEntity)
        {
            nextGrabbableEntity = _grabbableEntities
                .Where(grabbableEntity => _grabbablesStates[grabbableEntity] == false)
                .OrderBy(grabbableEntity => Vector3.Distance(transform.position, grabbableEntity.Grabbable.transform.position))
                .FirstOrDefault();
        }

    }
}

