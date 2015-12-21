using UnityEngine;
using System.Collections;
using UnityEditor;
using System.Collections.Generic;

namespace JSpline
{
    [CustomEditor(typeof(WaypointMgr))]
    public class WaypointEditor : Editor
    {
        static string pathName = "way";
        bool startPath = false;
        void OnSceneGUI()
        {
            if (startPath)
            {
                if (Event.current.type == EventType.keyDown && Event.current.keyCode == KeyCode.P)
                {
                    Ray worldRay = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);
                    RaycastHit hit;

                    if (Physics.Raycast(worldRay, out hit, int.MaxValue))
                    {
                        Event.current.Use();
                        AddPoint(hit.point);
                    }
                }
            }
        }

        public override void OnInspectorGUI()
        {
            pathName = EditorGUILayout.TextField(pathName);
            if (startPath == false)
            {
                if (GUILayout.Button("StartPath"))
                {
                    startPath = true;
                    SceneView.currentDrawingSceneView.Focus();
                }
            }
            else
            {
                if (GUILayout.Button("StopPath"))
                {
                    startPath = false;
                }
            }
        }

        void AddPoint(Vector3 pos)
        {
            GameObject pointObj = new GameObject();
            pointObj.transform.position = pos;
            WaypointMgr.instance.AddChild(pathName, pointObj.transform);
        }

        [UnityEditor.MenuItem("Waypoint/CreateMgr")]
        public static void CreateMgr()
        {
            new GameObject("WaypointMgr").AddComponent<WaypointMgr>();
        }

        [UnityEditor.MenuItem("Waypoint/GetPath")]
        public static void GetPath()
        {
            foreach (var trans in Selection.transforms)
            {
                GetPath(trans);
            }
        }

        public static void GetPath(Transform trans)
        {
            List<Transform> tList = new List<Transform>();
            for (int i = 0; i < trans.childCount; i++)
            {
                tList.Add(trans.GetChild(i));
            }
            tList.Sort((t1, t2) =>
            {
                //排除标记--
                string name1 = t1.name;
                string name2 = t2.name;
                int idx = name1.IndexOf(PARAM.SPLIT);
                if (idx >= 0)
                {
                    name1 = name1.Substring(0, idx);
                }
                idx = name2.IndexOf(PARAM.SPLIT);
                if (idx >= 0)
                {
                    name2 = name2.Substring(0, idx);
                }
                if (name1.Length != name2.Length)
                {
                    return name1.Length.CompareTo(name2.Length);
                }
                return name1.CompareTo(name2);
            });
            foreach (var t in tList)
            {
                WaypointMgr.instance.AddChild(trans.name, t);
            }
            
        }

        [UnityEditor.MenuItem("Waypoint/ClearPath")]
        public static void ClearPath()
        {
            Transform trans = Selection.transforms[0];
            WaypointMgr.instance.ClearPath(trans.name);
        }
    }
}