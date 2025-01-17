using System.Collections.Generic;
using System.Threading.Tasks;

namespace VRAgent
{
    public class ParallelAction : BaseAction
    {
        private List<BaseAction> parallelActions;

        public ParallelAction(List<BaseAction> parallelActions)
        {
            this.parallelActions = parallelActions;
        }

        public override async Task Execute()
        {
            await base.Execute();

            foreach(var action in parallelActions)
            {
                _ = action.Execute();
            }
        }
    }
}