using System.Collections.Generic;
using UnityEngine;

namespace VRExplorer
{
    public class FileIdManagerMono : MonoBehaviour
    {
        public List<string> fileIds = new List<string>();
        public List<GameObject> objects = new List<GameObject>();

        public void Clear()
        {
            fileIds.Clear();
            objects.Clear();
        }

        public void Add(string fileId, GameObject go)
        {
            fileIds.Add(fileId);
            objects.Add(go);
        }

        public GameObject GetObject(string fileId)
        {
            int index = fileIds.IndexOf(fileId);
            if(index >= 0 && index < objects.Count)
                return objects[index];
            return null;
        }
    }
}