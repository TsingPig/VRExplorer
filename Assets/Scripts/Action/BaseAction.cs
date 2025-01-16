using System.Threading.Tasks;

namespace VRAgent
{
    public class BaseAction
    {
        public string actionName;

        public virtual Task Execute() => Task.CompletedTask;
    }
}