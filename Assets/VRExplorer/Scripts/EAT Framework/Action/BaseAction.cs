using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TsingPigSDK;
using UnityEngine;
using UnityEngine.Events;

namespace VRExplorer
{
    [Serializable]
    public class BaseAction
    {
        [SerializeField] private string _name = "BaseAction";
        protected Action _callback = null;

        public BaseAction(Action callback = null)
        {
            _callback = callback;
        }

        public string Name { get => _name; set => _name = value; }

        public virtual async Task Execute()
        {
            Debug.Log(new RichText().Add("Action: ").Add(Name, color: new Color(0f, 0.5f, 1f)));
            await Task.Yield();
            _callback?.Invoke();
        }

        protected void SafeInvokeEvents(IEnumerable<UnityEvent> events)
        {
            foreach(var unityEvent in events)
            {
                try
                {
                    unityEvent?.Invoke();
                }
                catch(Exception ex)
                {
                    Debug.LogError(ex);
                }
            }
        }
    }
}