using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace VRExplorer
{
    public class HamiltonianExplorer : BaseExplorer
    {
        protected override bool TestFinished => throw new System.NotImplementedException();

        //private float[,] distanceMatrix;
        //private List<int> hamiltonianPath;
        //private int curGrabbableIndex = 0;

        //private void ComputeDistanceMatrix()
        //{
        //    int count = _grabbables.Count;
        //    distanceMatrix = new float[count + 1, count + 1];
        //    Vector3 agentStartPos = transform.position;
        //    for(int i = 0; i < count; i++)
        //    {
        //        Vector3 grabbablePos = _grabbables[i].transform.position;
        //        NavMeshPath path = new NavMeshPath();
        //        NavMesh.CalculatePath(agentStartPos, grabbablePos, NavMesh.AllAreas, path);

        //        if(path.status == NavMeshPathStatus.PathComplete)
        //        {
        //            float dist = path.corners.Zip(path.corners.Skip(1), Vector3.Distance).Sum();
        //            distanceMatrix[count, i] = dist;
        //            distanceMatrix[i, count] = dist;
        //        }
        //        else
        //        {
        //            distanceMatrix[count, i] = float.MaxValue;
        //            distanceMatrix[i, count] = float.MaxValue;
        //        }
        //    }

        //    for(int i = 0; i < count; i++)
        //    {
        //        for(int j = 0; j < count; j++)
        //        {
        //            if(i == j) continue;

        //            Vector3 start = _grabbables[i].transform.position;
        //            Vector3 end = _grabbables[j].transform.position;

        //            NavMeshPath path = new NavMeshPath();
        //            if(NavMesh.CalculatePath(start, end, NavMesh.AllAreas, path))
        //            {
        //                distanceMatrix[i, j] = path.corners.Zip(path.corners.Skip(1), Vector3.Distance).Sum();
        //            }
        //            else
        //            {
        //                distanceMatrix[i, j] = float.MaxValue;
        //            }
        //        }
        //    }
        //}

        ///// <summary>
        ///// 回溯法解决TSP
        ///// </summary>
        ///// <returns></returns>
        //private List<int> SolveTSP()
        //{
        //    int n = _grabbables.Count;
        //    List<int> path = new List<int>();
        //    List<int> bestPath = new List<int>();
        //    float bestDistance = float.MaxValue;

        //    bool[] visited = new bool[n];

        //    void Backtrack(int currentNode, float currentDistance, List<int> currentPath)
        //    {
        //        if(currentPath.Count == n)
        //        {
        //            if(currentDistance < bestDistance)
        //            {
        //                bestDistance = currentDistance;
        //                bestPath = new List<int>(currentPath);
        //            }
        //            return;
        //        }

        //        for(int i = 0; i < n; i++)
        //        {
        //            if(visited[i]) continue;

        //            visited[i] = true;
        //            currentPath.Add(i);
        //            float newDistance = currentDistance + distanceMatrix[currentNode, i];  // 更新当前路径的距离
        //            Backtrack(i, newDistance, currentPath);
        //            visited[i] = false;
        //            currentPath.RemoveAt(currentPath.Count - 1);
        //        }
        //    }

        //    Backtrack(n, 0, path);

        //    return bestPath;
        //}

        ///// <summary>
        ///// 重置加载所有可抓取物体的位置和旋转
        ///// </summary>
        //protected override void ResetMonoPos()
        //{
        //    base.ResetMonoPos();
        //    ComputeDistanceMatrix();
        //    hamiltonianPath = SolveTSP();

        //    string pathString = string.Join(" -> ", hamiltonianPath.Select(i => i.ToString()).ToArray());

        //    curGrabbableIndex = 0;
        //}

        ///// <summary>
        ///// 获取最近的可抓取物体
        ///// </summary>
        //protected override void GetNextMono(out MonoBehaviour mono)
        //{
        //    mono = _grabbables[hamiltonianPath[curGrabbableIndex]].GetComponent<MonoBehaviour>();
        //    curGrabbableIndex += 1;
        //}
        protected override void GetNextMono(out MonoBehaviour mono)
        {
            throw new System.NotImplementedException();
        }

        protected override Task SceneExplore()
        {
            throw new System.NotImplementedException();
        }


    }
}