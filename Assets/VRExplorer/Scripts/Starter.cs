using System.Collections;
using System.Collections.Generic;
using UnityEditor.TestTools.CodeCoverage;
using UnityEngine;
using VRExplorer;

public class Starter : MonoBehaviour
{
    private void Awake()
    {
        EntityManager.Instance.RegisterAllEntities();
        CodeCoverage.StartRecording();
    }
    private void Start()
    {
        Invoke("StartRecord", 0.5f);
    }
    void StartRecord()
    {
        MetricManager.Instance.StartRecord();
    }
    private void OnApplicationQuit()
    {
        CodeCoverage.StopRecording();
    }
}
