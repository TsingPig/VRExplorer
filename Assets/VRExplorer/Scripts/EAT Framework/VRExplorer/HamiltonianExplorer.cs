using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.AI;

namespace VRExplorer
{
    public class HamiltonianExplorer : BaseExplorer
    {
        private float[,] distanceMatrix; // �������
        private List<int> hamiltonianPath; // ���ܶ�·�����
        private int curGrabbableIndex = 0;

        /// <summary>
        /// ����������
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
        /// ���ݷ����TSP
        /// </summary>
        /// <returns></returns>
        private List<int> SolveTSP()
        {
            int n = _grabbables.Count;
            List<int> path = new List<int>();
            List<int> bestPath = new List<int>(); // �����洢���·��
            float bestDistance = float.MaxValue;  // �����洢���·���ľ���

            bool[] visited = new bool[n];  // ����Ƿ���ʹ�ĳ���ڵ�

            // �ݹ���ݺ���
            void Backtrack(int currentNode, float currentDistance, List<int> currentPath)
            {
                // ������нڵ㶼���ʹ��ˣ�����Ƿ������·��
                if(currentPath.Count == n)
                {
                    if(currentDistance < bestDistance)
                    {
                        bestDistance = currentDistance;
                        bestPath = new List<int>(currentPath);  // �������·��
                    }
                    return;
                }

                // �ݹ�ط���ÿһ��δ���ʵĽڵ�
                for(int i = 0; i < n; i++)
                {
                    if(visited[i]) continue;

                    // ���ʵ�ǰ�ڵ�
                    visited[i] = true;
                    currentPath.Add(i);
                    float newDistance = currentDistance + distanceMatrix[currentNode, i];  // ���µ�ǰ·���ľ���

                    // �ݹ�
                    Backtrack(i, newDistance, currentPath);

                    // ���ݣ�����ѡ��
                    visited[i] = false;
                    currentPath.RemoveAt(currentPath.Count - 1);
                }
            }

            // �ӳ�ʼ�ڵ㣨���������ʼλ�ã���ʼ��ִ�л���
            Backtrack(n, 0, path);  // ����㿪ʼ����

            return bestPath;
        }

        /// <summary>
        /// ���ü������п�ץȡ�����λ�ú���ת
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
        /// ��ȡ����Ŀ�ץȡ����
        /// </summary>
        protected override void GetNextMono(out MonoBehaviour mono)
        {
            mono = _grabbables[hamiltonianPath[curGrabbableIndex]].GetComponent<MonoBehaviour>();
            curGrabbableIndex += 1;
        }
    }
}