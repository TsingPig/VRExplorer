using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AI;

namespace VRAgent
{

    public class BaseAction
    {
        public string actionName;
        public virtual Task Execute() => Task.CompletedTask; 

    }



}