using System.Collections.Generic;
using System.Threading.Tasks;

namespace VRExplorer
{
    public class ParallelAction : BaseAction
    {
        private List<BaseAction> parallelActions;

        public ParallelAction(List<BaseAction> parallelActions)
        {
            this.parallelActions = parallelActions;
            Name = "ParallelAction";
        }

        public override async Task Execute()
        {
            await base.Execute();
            List<Task> tasks = new List<Task>();
            foreach(var action in parallelActions)
            {
                tasks.Add(action.Execute());
            }

            await Task.WhenAll(tasks);
        }
    }
}