#if UNITY_EDITOR

using UnityEditor;
using UnityEngine;

namespace VRExplorer
{
    [InitializeOnLoad]
    public static class TagInitializer
    {
        static TagInitializer()
        {
            AddTagIfNotExists(Str.TempTargetTag);
        }

        private static void AddTagIfNotExists(string tag)
        {
            SerializedObject tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
            SerializedProperty tagsProp = tagManager.FindProperty("tags");

            bool found = false;
            for(int i = 0; i < tagsProp.arraySize; i++)
            {
                SerializedProperty t = tagsProp.GetArrayElementAtIndex(i);
                if(t.stringValue.Equals(tag)) { found = true; break; }
            }

            if(!found)
            {
                tagsProp.InsertArrayElementAtIndex(tagsProp.arraySize);
                SerializedProperty newTag = tagsProp.GetArrayElementAtIndex(tagsProp.arraySize - 1);
                newTag.stringValue = tag;
                tagManager.ApplyModifiedProperties();
                Debug.Log($"Tag '{tag}' was added to the project.");
            }
        }
    }

#endif
}