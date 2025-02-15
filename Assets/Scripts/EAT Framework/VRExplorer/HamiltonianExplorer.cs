using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.AI;

namespace VRExplorer
{
    public class HamiltonianExplorer : BaseExplorer
    {
        private float[,] distanceMatrix; // 距离矩阵
        private List<int> hamiltonianPath; // 哈密顿路径结果
        private int curGrabbableIndex = 0;

        /// <summary>
        /// 计算距离矩阵
        /// </summary>
        private void ComputeDistanceMatrix()
        {
            int count = _grabbables.Count;
            distanceMatrix = new float[count + 1, count + 1];
            Vector3 agentStartPos = transform.position;
            for(int i = 0; i < count; i++)
            {
                Vector3 grabbablePos = _grabbables[i].transform.position;
                NavMeshPath path = new NavMeshPath();
                NavMesh.CalculatePath(agentStartPos, grabbablePos, NavMesh.AllAreas, path);

                if(path.status == NavMeshPathStatus.PathComplete)
                {
                    float dist = path.corners.Zip(path.corners.Skip(1), Vector3.Distance).Sum();
                    distanceMatrix[count, i] = dist;
                    distanceMatrix[i, count] = dist;
                }
                else
                {
                    distanceMatrix[count, i] = float.MaxValue; // Set to an unreachable value
                    distanceMatrix[i, count] = float.MaxValue;
                }
            }

            for(int i = 0; i < count; i++)
            {
                for(int j = 0; j < count; j++)
                {
                    if(i == j) continue;

                    Vector3 start = _grabbables[i].transform.position;
                    Vector3 end = _grabbables[j].transform.position;

                    NavMeshPath path = new NavMeshPath();
                    if(NavMesh.CalculatePath(start, end, NavMesh.AllAreas, path))
                    {
                        distanceMatrix[i, j] = path.corners.Zip(path.corners.Skip(1), Vector3.Distance).Sum();
                    }
                    else
                    {
                        distanceMatrix[i, j] = float.MaxValue; // Set to an unreachable value if no path exists
                    }
                }
            }
        }

        /// <summary>
        /// 回溯法解决TSP
        /// </summary>
        /// <returns></returns>
        private List<int> SolveTSP()
        {
            int n = _grabbables.Count;
            List<int> path = new List<int>();
            List<int> bestPath = new List<int>(); // 用来存储最短路径
            float bestDistance = float.MaxValue;  // 用来存储最短路径的距离

            bool[] visited = new bool[n];  // 标记是否访问过某个节点

            // 递归回溯函数
            void Backtrack(int currentNode, float currentDistance, List<int> currentPath)
            {
                // 如果所有节点都访问过了，检查是否是最短路径
                if(currentPath.Count == n)
                {
                    if(currentDistance < bestDistance)
                    {
                        bestDistance = currentDistance;
                        bestPath = new List<int>(currentPath);  // 更新最短路径
                    }
                    return;
                }

                // 递归地访问每一个未访问的节点
                for(int i = 0; i < n; i++)
                {
                    if(visited[i]) continue;

                    // 访问当前节点
                    visited[i] = true;
                    currentPath.Add(i);
                    float newDistance = currentDistance + distanceMatrix[currentNode, i];  // 更新当前路径的距离

                    // 递归
                    Backtrack(i, newDistance, currentPath);

                    // 回溯，撤销选择
                    visited[i] = false;
                    currentPath.RemoveAt(currentPath.Count - 1);
                }
            }

            // 从初始节点（即代理的起始位置）开始，执行回溯
            Backtrack(n, 0, path);  // 从起点开始回溯

            return bestPath;
        }

        /// <summary>
        /// 重置加载所有可抓取物体的位置和旋转
        /// </summary>
        protected override void ResetMonoPos()
        {
            base.ResetMonoPos();
            ComputeDistanceMatrix();
            hamiltonianPath = SolveTSP();

            string pathString = string.Join(" -> ", hamiltonianPath.Select(i => i.ToString()).ToArray());
            //Debug.Log("Hamiltonian Path: " + pathString);

            curGrabbableIndex = 0;
        }

        /// <summary>
        /// 获取最近的可抓取物体
        /// </summary>
        protected override void GetNextMono(out MonoBehaviour mono)
        {
            mono = _grabbables[hamiltonianPath[curGrabbableIndex]].GetComponent<MonoBehaviour>();
            curGrabbableIndex += 1;
        }
    }
}