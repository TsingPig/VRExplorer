using System;
using System.Threading.Tasks;
using TsingPigSDK;
using UnityEngine;

namespace VRExplorer
{
    [Serializable]
    public class BaseAction
    {
        [SerializeField] private string _name;
        protected Action _callback = null;

        public BaseAction(Action callback = null)
        {
            _callback = callback;
        }

        public string Name { get => _name; set => _name = value; }

        public virtual async Task Execute()
        {
            Debug.Log(new RichText().Add("Action: ").Add(Name, color: new Color(0f, 0.5f, 1f)));
            await Task.CompletedTask;
            _callback?.Invoke();
        }
    }
}