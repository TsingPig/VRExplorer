using System.Linq;
using UnityEngine;

namespace VRExplorer
{
    public class VRExplorer : BaseExplorer
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