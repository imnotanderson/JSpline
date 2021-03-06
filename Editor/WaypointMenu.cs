﻿using UnityEngine;
using System.Collections;
using UnityEditor;

namespace JSpline
{   

    public class WaypointMenu : EditorWindow
    {


        [UnityEditor.MenuItem("Waypoint/OpenMenu")]
        static void Open()
        {
            EditorWindow.CreateInstance<WaypointMenu>().Show();
        }

        void OnGUI()
        {
            if (GUILayout.Button("禁止旋转"))
            {
                foreach (var item in Selection.transforms)
                {
                    int idx = item.name.IndexOf(PARAM.NO_ROTATION);
                    if (idx >= 0)
                    {
                        continue;
                    }
                    idx = item.name.IndexOf(PARAM.SPLIT);
                    if (idx < 0)
                    {
                        item.name += PARAM.SPLIT;
                    }
                    item.name += PARAM.NO_ROTATION;
                }
            }
            if (GUILayout.Button("清除标记"))
            {
                foreach (var item in Selection.transforms)
                {
                    int idx = item.name.IndexOf(PARAM.SPLIT);
                    if (idx >= 0)
                    {
                        item.name = item.name.Substring(0, idx);
                    }
                }  
            }
        }

    }
}