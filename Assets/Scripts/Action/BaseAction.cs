using UnityEngine;
using System.Threading.Tasks;
using System;

namespace VRAgent
{
    [Serializable]
    public class BaseAction
    {
        [SerializeField] private string _name;
        public string Name { get => _name; set => _name = value; }

        public virtual async Task Execute()
        {
            Debug.Log($"Action: {_name}");
            await Task.CompletedTask;
        }
    }
}
