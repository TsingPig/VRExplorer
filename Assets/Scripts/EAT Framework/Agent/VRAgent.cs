using System.Linq;
using UnityEngine;

namespace VRAgent
{
    public class VRAgent : BaseAgent
    {
        protected override void GetNextMono(out MonoBehaviour nextMono)
        {
            nextMono = EntityManager.Instance.monoState.Keys
                .Where(mono => EntityManager.Instance.monoState[mono] == false)
                .OrderBy(mono => Vector3.Distance(transform.position, mono.transform.position))
                .FirstOrDefault();
        }
    }
}