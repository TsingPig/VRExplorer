using System;
using System.Threading.Tasks;
using TsingPigSDK;
using UnityEngine;

namespace VRAgent
{
    /// <summary>
    /// BaseAction
    /// Definition: BaseAction is an indivisible and fundamental action, particularly representing an asynchronous task that the VRAgent can perform.
    /// For instance, the GrabAction comprises three distinct steps: first, grabbing the GrabbableEntity; second, dragging it around;
    /// and finally, releasing it. These three substeps together form a complete process.
    /// Thus, a BaseAction can be viewed as a single instruction in a computer, and the combination of multiple BaseActions forms a task.
    /// </summary>
    [Serializable]
    public class BaseAction
    {
        public string Name { get; protected set; }

        public virtual async Task Execute()
        {
            Debug.Log(new RichText().Add("Action: ").Add(Name, color: new Color(0f, 0.5f, 1f)));
            await Task.CompletedTask;
        }
    }
}