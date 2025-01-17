using System.Linq;
using UnityEngine;

namespace VRAgent
{
    public class VRAgent : BaseAgent
    {
        protected override void GetNextEntity(out IBaseEntity nextEntity)
        {
            nextEntity = _entities.Keys
                .Where(e => _entities[e] == false)
                .OrderBy(e => Vector3.Distance(transform.position, e.transform.position))
                .FirstOrDefault();
        }
    }
}