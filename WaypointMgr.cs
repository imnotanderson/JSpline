using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace JSpline
{
    /// <summary>
    /// 标记范围--
    /// </summary>
    public struct MarkRange
    {
        public float from;
        public float to;

        public MarkRange SetFrom(float from)
        {
            this.from = from;
            return this;
        }

        public MarkRange SetTo(float to)
        {
            this.to = to;
            return this;
        }

        /// <summary>
        /// 是否在范围内--
        /// </summary>
        /// <param name="perc"></param>
        /// <returns></returns>
        public bool IsIn(float perc)
        {
            return from <= perc && perc <= to;
        }
    }

    public struct PARAM
    {
        public const char NO_ROTATION = 'r';
        public const char CAMERA_START = 'a';
        public const char CAMERA_END = 'b';
        public const char SPLIT = '_';
    }
 [ExecuteInEditMode]
    public class WaypointMgr : MonoBehaviour
    {
        const float rad = 0.3f;

        static WaypointMgr mMgr = null;
        public static WaypointMgr instance
        {
            get
            {
                if (mMgr == null)
                {
                    mMgr = GameObject.FindObjectOfType<WaypointMgr>();
                    if (mMgr == null)
                    {
                        mMgr = new GameObject("WaypointMgr").AddComponent<WaypointMgr>();
                    }
                }
                return mMgr;
            }
        }

        Dictionary<string, List<Transform>> wayDict = new Dictionary<string, List<Transform>>();
        Dictionary<string, List<Vector3>> wayVec3Dict = new Dictionary<string, List<Vector3>>();
        Dictionary<string, float> pathLengthDict = new Dictionary<string, float>();

        void Awake()
        {
            InitPath(transform);
        }

        void ResetPath()
        {
            wayDict.Clear();
            pathLengthDict.Clear();
        }



        public void InitPath(Transform pTrans)
        {
            ResetPath();
            List<Transform> childList = new List<Transform>();
            for (int i = 0; i < pTrans.childCount; i++)
            {
                childList.Add(pTrans.GetChild(i));
            }
            for (int i = 0; i < childList.Count; i++)
            {
                Transform ctran = childList[i];
                List<Transform> tList = new List<Transform>();
                for (int j = 0; j < ctran.childCount; j++)
                {
                    tList.Add(ctran.GetChild(j));
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
                    AddChild(ctran.name, t);
                }
                CalcPathLength(ctran.name);
            }
        }

        public List<Transform> GetPath(string pathName)
        {
            if (wayDict.ContainsKey(pathName))
            {
                return wayDict[pathName];
            }
            return new List<Transform>();
        }

        public List<Vector3> GetVec3Path(string pathName)
        {
            if (wayDict.ContainsKey(pathName))
            {
                if (wayVec3Dict.ContainsKey(pathName))
                {
                    return wayVec3Dict[pathName];
                }
                else
                {
                    wayVec3Dict[pathName] = new List<Vector3>();
                    foreach (var item in wayDict[pathName])
                    {
                        wayVec3Dict[pathName].Add(item.position);
                    }
                    return wayVec3Dict[pathName];
                }
            }
            return new List<Vector3>();
        }

        public List<Transform> GetList(string pathName)
        {
            if (wayDict.ContainsKey(pathName) == false)
            {
                wayDict[pathName] = new List<Transform>();
            }
            return wayDict[pathName];
        }

        public void ClearPath(string pathName)
        {
            if (wayDict.ContainsKey(pathName))
            {
                wayDict[pathName].Clear();
            }
        }

        public void AddChild(string pathName, Transform trans)
        {
            Transform pTrans = transform.FindChild(pathName);
            if (pTrans == null)
            {
                pTrans = new GameObject(pathName).transform;
            }
            pTrans.parent = transform;
            trans.parent = pTrans;
            var list = GetList(pathName);
            for (int i = list.Count - 1; i >= 0; i--)
            {
                if (list[i] == null)
                {
                    list.RemoveAt(i);
                }
            }
            string markStr = trans.name;
            int idx = markStr.IndexOf(PARAM.SPLIT);
            if (idx >= 0)
            {
                markStr = markStr.Substring(idx, markStr.Length - idx);
            }
            else
            {
                markStr = "";
            }
            trans.name = string.Format("{0}{1}", list.Count, markStr);
            list.Add(trans);
        }

        public void OnDrawGizmos()
        {
            Gizmos.color = Color.red;
            int count = 10;
            foreach (var list in wayDict.Values)
            {
                Vector3? lastPos = null;
                int pointCount = list.Count * count;
                for (int i = 0; i < list.Count; i++)
                {
                    if (list[i] == null)
                    {
                        list.RemoveAt(i);
                        i--;
                        continue;
                    }
                    Gizmos.color = GetMarkColor(list[i]);
                    Gizmos.DrawSphere(list[i].position, rad);
                }
                for (int i = 0; i < pointCount; i++)
                {
                    float perc = (float)i / pointCount;
                    if (CheckMark(list, perc))
                    {
                        Gizmos.color = Color.green;
                    }
                    else
                    {
                        Gizmos.color = Color.red;
                    }
                    var pos = Tools.GetPointByTrans(perc, list, false);
                    if (lastPos == null)
                    {
                        lastPos = pos;
                    }
                    else
                    {
                        Gizmos.DrawLine((Vector3)lastPos, pos);
                        lastPos = pos;
                    }
                }
            }
        }

        Color GetMarkColor(Transform trans)
        {
            if (CheckMark(trans, PARAM.CAMERA_START))
            {
                return Color.yellow;
            }
            if (CheckMark(trans, PARAM.CAMERA_END))
            {
                return Color.blue;
            }
            return Color.red;
        }

       public static bool CheckMark(Transform trans,char c)
        {
            if (trans.name.IndexOf(c) > 0)
            {
                return true;
            }
            return false;
        }

        /// <summary>
        /// 检查百分比是否在标记区间内--
        /// </summary>
        /// <param name="perc"></param>
        /// <returns></returns>
        bool CheckMarkRange(string pathName, float perc)
        {
            var path = GetPath(pathName);
            return CheckMark(path, perc);
        }

        /// <summary>
        /// 检查百分比是否在标记区间内--
        /// </summary>
        /// <param name="perc"></param>
        /// <returns></returns>
        bool CheckMark(List<Transform> path, float perc)
        {
            float fVal = (path.Count - 1) * perc;
            int checkIdx = Mathf.FloorToInt(fVal);
            if (path[checkIdx].name.IndexOf(PARAM.NO_ROTATION) >= 0)
            {
                return true;
            }
            return false;
        }

        public Vector3 GetPos(string pathName, float perc)
        {
            var path = GetVec3Path(pathName);
            return Tools.GetPointByVec3(perc, path, false);
        }

        /// <summary>
        /// 获得切线方向,负方向--
        /// </summary>
        /// <returns></returns>
        public Vector3 GetTangent(string pathName, float perc)
        {
            float p1 = perc - 0.01f;
            p1 = Mathf.Clamp01(p1);
            float p2 = p1 + 0.01f;
            Vector3 dir = GetPos(pathName, p1) - GetPos(pathName, p2);
            return dir;
        }

        /// <summary>
        /// 重新计算路径长度--
        /// </summary>
        /// <param name="pathName"></param>
       public  float CalcPathLength(string pathName)
        {
            if (wayDict.ContainsKey(pathName))
            {
                pathLengthDict[pathName] = 0;
                List<Transform> tList = wayDict[pathName];
                Transform tmTrans = null;
                foreach (var t in tList)
                {
                    //算路径长度--
                    if (tmTrans == null)
                    {
                        tmTrans = t;
                    }
                    else
                    {
                        pathLengthDict[pathName] += Vector3.Distance(tmTrans.position, t.position);
                        tmTrans = t;
                    }
                }
                return pathLengthDict[pathName];
            }
            return 0;
        }

        /// <summary>
        /// 获得路径长度--
        /// </summary>
        /// <param name="pathName"></param>
        /// <returns></returns>
        public float GetPathLength(string pathName)
        {
            if (pathLengthDict.ContainsKey(pathName))
            {
                return pathLengthDict[pathName];
            }
            else
            {
                return CalcPathLength(pathName);
            }
        }

        /// <summary>
        /// 获得路径长度对应的百分比--
        /// </summary>
        /// <param name="pathName"></param>
        /// <param name="length"></param>
        /// <returns></returns>
        public float GetLengthPercent(string pathName, float length)
        {
            return length / GetPathLength(pathName);
        }

        public float GetWayProcessByPos(string pathName, Vector3 pos)
        {
            return Tools.GetWayProcessByPos(pathName, pos);
        }

        public int GetWaypointCount(string pathName)
        {
            if (wayDict.ContainsKey(pathName) == false)
            {
                return 0;
            }
            return wayDict[pathName].Count;
        }

        /// <summary>
        /// 获得第Idx个样本点的曲线百分比--
        /// </summary>
        /// <param name="pathName"></param>
        /// <param name="pointIdx"></param>
        /// <returns></returns>
        public float GetWaypointPercByPointIdx(string pathName, int pointIdx)
        {
            int count = GetWaypointCount(pathName);
            if (count <= 0)
            {
                return 0;
            }
            count--;
            pointIdx = Mathf.Clamp(pointIdx, 0, count);
            float perc = (float)pointIdx / count;
            return perc;
        }

        /// <summary>
        /// 倒叙获得第Idx个样本点的曲线百分比--
        /// </summary>
        /// <param name="pathName"></param>
        /// <param name="pointIdx"></param>
        /// <returns></returns>
        public float GetWaypointPercByPointIdxLast(string pathName, int pointIdx)
        {
            int count = GetWaypointCount(pathName);
            if (count <= 0)
            {
                return 0;
            }
            count--;
            pointIdx = Mathf.Clamp(pointIdx, 0, count);
            pointIdx = count - pointIdx;
            float perc = (float)pointIdx / count;
            return perc;
        }

        /// <summary>
        /// 获得第Idx个样本点坐标--
        /// </summary>
        /// <param name="pathName"></param>
        /// <param name="pointIdx"></param>
        /// <returns></returns>
        public Vector3 GetPosByPointIdx(string pathName, int pointIdx)
        {
            if (wayDict.ContainsKey(pathName) == false)
            {
                return Vector3.zero;
            }
            if (wayDict[pathName].Count <= pointIdx || pointIdx < 0)
            {
                return Vector3.zero;
            }
            return wayDict[pathName][pointIdx].position;
        }

        /// <summary>
        /// 路径百分比获得路径长度--
        /// </summary>
        /// <param name="perc"></param>
        /// <returns></returns>
        public float GetPathLengthByPerc(string pathName, float perc)
        {
            if(perc>=1)return GetPathLength(pathName);
            int count = GetWaypointCount(pathName);
            float leftPerc = 0;
            float len = 0;
            Vector3 leftPos = Vector3.zero;
            Vector3 tmPos;
            for (int i = 0; i < count; i++)
            {
                if (i == 0)
                {
                    leftPos = GetPosByPointIdx(pathName, i);
                    continue;
                }
                leftPerc = GetWaypointPercByPointIdx(pathName, i);
                if (perc >= leftPerc)
                {
                    tmPos = GetPosByPointIdx(pathName, i);
                    len += Vector3.Distance(leftPos, tmPos);
                    leftPos = tmPos;
                }
                else
                {
                    leftPerc = GetWaypointPercByPointIdx(pathName, i-1);
                    tmPos = GetPosByPointIdx(pathName, i);
                    float rightPerc = GetWaypointPercByPointIdx(pathName, i);
                    len += Mathf.Abs(Vector3.Distance(leftPos, tmPos) * (perc - leftPerc) / (leftPerc - rightPerc));
                    break;
                }
            }
            return len;
        }

        /// <summary>
        /// 获得两个路径两个百分比之间的距离--
        /// </summary>
        /// <param name="pathName"></param>
        /// <param name="perc1"></param>
        /// <param name="perc2"></param>
        /// <returns></returns>
        public float GetPercDistance(string pathName,float perc1,float perc2)
        {
            float len1 = GetPathLengthByPerc(pathName, perc1);
            float len2 = GetPathLengthByPerc(pathName, perc2);
            return Mathf.Abs(len2 - len1);
        }

        /// <summary>
        /// 倒叙获得第Idx个样本点坐标--
        /// </summary>
        /// <param name="pathName"></param>
        /// <param name="pointIdx"></param>
        /// <returns></returns>
        public Vector3 GetPosByPointIdxLast(string pathName, int pointIdx)
        {
            if (wayDict.ContainsKey(pathName) == false)
            {
                return Vector3.zero;
            }
            if (wayDict[pathName].Count <= pointIdx || pointIdx < 0)
            {
                return Vector3.zero;
            }
            int count = wayDict[pathName].Count;
            pointIdx = count - pointIdx - 1;
            return wayDict[pathName][pointIdx].position;
        }

        /// <summary>
        /// 获得标记范围--
        /// </summary>
        /// <param name="pathName"></param>
        /// <returns></returns>
        public List<MarkRange> GetMarkRange(string pathName)
        {
            List<MarkRange> markRangeList = new List<MarkRange>();
            var path = GetPath(pathName);
            int count = path.Count;
            float perc = 0;
            MarkRange? range = null;
            for (int i = 0; i < count; i++)
            {
                perc = (float)i / (count - 1);
                if (CheckMarkRange(pathName, perc))
                {
                    if (range == null)
                    {
                        range = new MarkRange().SetFrom(perc);
                    }
                }
                else
                {
                    if (range != null)
                    {
                        range = range.Value.SetTo(perc);
                        markRangeList.Add(range.Value);
                        range = null;
                    }
                }
            }
            if (range != null)
            {
                range = range.Value.SetTo(1);
                markRangeList.Add(range.Value);
            }
            return markRangeList;
        }
        
        public Vector3 GetPosByActualPerc(string pathName, float actualPerc)
        {
            var path = GetVec3Path(pathName);
            float perc = ActualPerc2Perc(pathName, actualPerc);
            return Tools.GetPointByVec3(perc, path, false);
        }
        
        /// <summary>
        /// 真实路径转计算百分比--
        /// </summary>
        /// <param name="pathName"></param>
        /// <param name="actualPerc"></param>
        /// <returns></returns>
        public float ActualPerc2Perc(string pathName,float actualPerc) {
            float totalLen = GetPathLength(pathName);
            List<Transform> pList = GetPath(pathName);
            if (pList.Count <= 2)
            {
                return actualPerc;
            }
            float tmLen = 0;
            int rangeIdx = 1;
            float actualDeltaPerc = 0;
            for (rangeIdx = 1; rangeIdx < pList.Count; rangeIdx++)
            {
                float d = Vector3.Distance(pList[rangeIdx - 1].position, pList[rangeIdx].position);
                tmLen += d;
                if (tmLen / totalLen >= actualPerc)
                {
                    actualDeltaPerc = (actualPerc * totalLen - (tmLen - d) ) / d;
                    break;
                }
            }
            float basePerc = GetWaypointPercByPointIdx(pathName, rangeIdx - 1);
            float deltaPerc = GetWaypointPercByPointIdx(pathName, rangeIdx) - basePerc;
            return basePerc + deltaPerc * actualDeltaPerc;
        }
        
        public NormalPath getNormalPath(string splinePathName, int accuracy = 100)
        {
            accuracy = 10000;
            //忽略距离--
            float ignoreDistance = 10* 0.1f;
            //检查精度--
            float checkAccuracy = 1f / accuracy;

            List<Transform> transList = GetPath(splinePathName);
            List<Vector3> pList = new List<Vector3>();
            for (int i = 0; i <= accuracy; i++)
            {
                float perc = i * checkAccuracy;
                Vector3 pos = GetPos(splinePathName, perc);
                if (pList.Count < 2)
                {
                    pList.Add(pos);
                    continue;
                }
                Vector3
                    p1 = pList[pList.Count - 2],
                    p2 = pList[pList.Count - 1],
                    p3 = pos;
                Vector3 v1 = p2 - p1;
                Vector3 v2 = p3 - p2;
                //float dotLen = Vector3.Project(v2, v1).magnitude;
                Vector3 shadowPos = p2 + Vector3.Project(v2, v1);// v1.normalized* dotLen;
                float d = Vector3.Distance(shadowPos, p3);
                if (d > ignoreDistance|| i==accuracy)
                {
                    pList.Add(p3);
                }
            }
            return new NormalPath(splinePathName, pList);
        }
    }
    
        public class NormalPath
    {
        string name = "";
        public List<Vector3> pList = new List<Vector3>();
        public NormalPath(string pathName, List<Vector3> pList)
        {
            this.name = pathName;
            this.pList = pList;
        }

        public Vector3 GetPosByDistance(float distance)
        {
            if (distance <= 0) {
                return pList[0];
            }
            float len = 0;
            for (int i = 1; i < pList.Count; i++)
            {
                float d = Vector3.Distance(pList[i], pList[i - 1]);
                if (len + d > distance)
                {
                    float deltaDistance = distance - len;
                    return pList[i - 1] + (pList[i] - pList[i - 1]).normalized * deltaDistance;
                }
                else {
                    len += d;
                }
            }
            return pList[pList.Count - 1];
        }

        //原来曲线的切线方向--

    }
    
    //NewSpline:权重曲线
    //...
}
