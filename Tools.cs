using UnityEngine;
using System.Collections;
using System.Collections.Generic;


namespace JSpline
{
    public class Tools
    {
        public static Vector3 GetPoint(float perc, List<Vector3> wps, bool isClosed)
        {
            if (wps.Count <= 1)
            {
                return Vector3.zero;
            }
            perc = Mathf.Clamp01(perc);
            Vector3[] controlPoints = null;
            if (isClosed)
            {
                controlPoints = new Vector3[]{
                    wps[wps.Count - 1],
                    wps[0]
                };
                wps.Insert(0, controlPoints[1]);
                wps.Add(wps[0]);
            }
            else
            {
                controlPoints = new Vector3[]{
                wps[0]+(wps[0]-wps[1]).normalized,
                wps[wps.Count - 1]+(wps[wps.Count - 1]-wps[wps.Count - 2]).normalized
                };
            }
            int numSections = wps.Count - 1;
            int tSec = (int)Mathf.Floor(perc * numSections);
            int currPt = numSections - 1;
            if (currPt > tSec) currPt = tSec;
            float u = perc * numSections - currPt;

            Vector3 a = currPt == 0 ? controlPoints[0] : wps[currPt - 1];
            Vector3 b = wps[currPt];
            Vector3 c = wps[currPt + 1];
            Vector3 d = currPt + 2 > wps.Count - 1 ? controlPoints[1] : wps[currPt + 2];

            return .5f * (
                (-a + 3f * b - 3f * c + d) * (u * u * u)
                + (2f * a - 5f * b + 4f * c - d) * (u * u)
                + (-a + c) * u
                + 2f * b
            );
        }



        public static Vector3 GetPointByTrans(float perc, List<Transform> tList, bool isClosed)
        {
            List<Vector3> vList = new List<Vector3>();
            for (int i = 0; i < tList.Count; i++)
            {
                vList.Add(tList[i].position);
            }
            return GetPoint(perc, vList, isClosed);
        }

        public static Vector3 GetPointByVec3(float perc, List<Vector3> tList, bool isClosed)
        {
            return GetPoint(perc, tList, isClosed);
        }

        public static float GetWayProcessByPos(string pathName, Vector3 pos)
        {
            float len = WaypointMgr.instance.GetPathLength(pathName);

//            List<Transform> tList = WaypointMgr.instance.GetPath(pathName);
            var tList = WaypointMgr.instance.GetNewPath(pathName);// GetPath(pathName);
            Vector3? tmPos = null;
            float min = 0, max = 0;
            float d = float.MaxValue;
            for (int i = 0; i < tList.list.Count; i++)
            {
                var t = tList.list[i];
                if (tmPos == null)
                {
                    tmPos = t.pos;
                }
                else
                {
                    var tmD = Vector3.Distance(GetChuidian(tmPos.Value, t.pos, pos), pos);
                    if (d > tmD)
                    {
                        d = tmD;
                        min = JSpline.WaypointMgr.instance.GetWaypointPercByPointIdx(pathName, i - 1);
                        max = JSpline.WaypointMgr.instance.GetWaypointPercByPointIdx(pathName, i);
                    }
                    tmPos = t.pos;
                }
            }

            float process = GetWayProcessByPos(pathName, pos, min, max, len);
            return CheckWayProcess(pathName, pos, process);
        }


        static float GetWayProcessByPos(string pathName, Vector3 pos, float min, float max, float len)
        {
            //精度--
            float precision = 0.03f;
            Vector3 currPos = pos;
            Vector3 minPos = WaypointMgr.instance.GetPosByTruePerc(pathName, min);
            Vector3 maxPos = WaypointMgr.instance.GetPosByTruePerc(pathName, max);

            if (Vector3.Distance(minPos, pos) < precision)
            {
                return min;
            }
            if (Vector3.Distance(maxPos, pos) < precision)
            {
                return max;
            }
            if (Mathf.Abs(min - max) * len < precision)
            {
                return (min + max) / 2;
            }
            if (Vector3.Distance(currPos, minPos) < Vector3.Distance(currPos, maxPos))
            {
                max = (min + max) / 2;
                return GetWayProcessByPos(pathName, pos, min, max, len);
            }
            else
            {
                min = (min + max) / 2;
                return GetWayProcessByPos(pathName, pos, min, max, len);
            }
        }

        /// <summary>
        /// xz平面求pos到from,to连线垂足坐标
        /// </summary>
        /// <param name="from"></param>
        /// <param name="to"></param>
        /// <param name="pos"></param>
        /// <returns></returns>
        public static Vector3 GetChuidian(Vector3 from, Vector3 to, Vector3 pos)
        {
            if (from.z == to.z) from.z += 0.01f;
            if (from.x == to.x) from.x += 0.01f;

            float a = from.x, b = from.z, c = to.x, d = to.z, e = pos.x, f = pos.z;
            float g = 0, h = 0;
            g = e * (a - c) * (a - c) - (b - d) * b * (a - c) + a * (b - d) * (b - d) + f * (a - c) * (b - d);
            g = g / ((a - c) * (a - c) + (b - d) * (b - d));
            pos.x = g;
            h = b - (a - g) * (b - d) / (a - c);
            pos.z = h;
            //判断在线段内--
            if (Vector3.Dot(pos - from, pos - to) < 0)
            {
                return pos;
            }
            else
            {
                return (Vector3.Distance(pos, from) < Vector3.Distance(pos, from)) ? from : to;
            }
        }

        public static double Distance(Vector3 v1, Vector3 v2)
        {
            var v = v1 - v2;
            double
                x = v.x,
                y = v.y,
                z = v.z;
            return System.Math.Sqrt( x * x + y * y + z * z);
        }

        static float CheckWayProcess(string pathName, Vector3 pos, float baseProcess)
        {
            int count = 100;
            float offset = 0.05f;
            float step = offset * 2 / count;
            float d = 999;
            float finalProcess = baseProcess;
            for (int i = 0; i < count; i++)
            {
                float tmProcess = baseProcess - offset + step * i;
                Vector3 v = WaypointMgr.instance.GetPosByTruePerc(pathName, tmProcess);
                float tmD = Vector3.Distance(pos, v);
                if (tmD < d)
                {
                    d = tmD;
                    finalProcess = tmProcess;
                }
            }
            return UnityEngine.Mathf.Clamp01(finalProcess);
        }

    }
}