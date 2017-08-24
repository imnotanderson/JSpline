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

    public struct Layer
    {
        public const int ThreeDRoad = 0;
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
            return new List<Vector3>(new Vector3[] { Vector3.zero, Vector3.zero, Vector3.zero });
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
            Transform pTrans = transform.Find(pathName);
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
            foreach (var k in wayDict)
            {
                List<Transform> list = k.Value;
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

        public static bool CheckMark(Transform trans, char c)
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
            Vector3 pos1 = GetPos(pathName, p1);
            Vector3 pos2 = GetPos(pathName, p2);
            Vector3 dir = pos1 - pos2;
            return dir;
        }

        /// <summary>
        /// 重新计算路径长度--
        /// </summary>
        /// <param name="pathName"></param>
        public float CalcPathLength(string pathName)
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

        public Vector3 GetPosByPointIdx(string pathName, int pointIdx)
        {
            var list = GetPath(pathName);
            if (list.Count > pointIdx)
            {
                return list[pointIdx].position;
            }
            Debug.LogError(string.Format("获得 {0} 路径点坐标 {1} 越界。", pathName, pointIdx));
            return Vector3.zero;
        }
        public Vector3 GetPosByPointIdxLast(string pathName, int pointIdx)
        {
            var list = GetPath(pathName);
            if (list.Count > pointIdx)
            {
                return list[list.Count - pointIdx - 1].position;
            }
            Debug.LogError(string.Format("倒序获得 {0} 路径点坐标 {1} 越界。", pathName, pointIdx));
            return Vector3.zero;
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
                perc = GetWaypointPercByPointIdx(pathName, i);// (float)i / (count - 1);
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


        #region New

        public struct Point
        {
            public float perc;
            public float distance;
            public Vector3 pos;
        }

        public class Path
        {
            public float maxLen;
            public string name;
            public List<Point> list = new List<Point>();
        }

        Dictionary<string,Path>pathDict = new Dictionary<string,Path>();

        void InitPathDict(string pathName)
        {
            if (pathDict.ContainsKey(pathName)) return;
            Path path = new Path();
            path.name = pathName;
            int count = 300;
            float step = 1f / count;
            float len = 0;
            Vector3? dir = null;
            Vector3 prePosForLen = Vector3.zero;
            for (int i= 0; i<=count; i++)
            {
                float perc = 0 + step * i;
                var pos = GetPos(pathName, perc);
                if (path.list.Count >0)
                {
                    len += Vector3.Distance(prePosForLen, pos);
                    if (path.list.Count > 1)
                    {
                        dir = (Vector3)(path.list[path.list.Count - 1].pos - path.list[path.list.Count - 2].pos);
                    }
                    else {
                        dir = null;
                    }
                    if (i!=count && dir != null && Check(pos, path.list[path.list.Count - 1].pos, dir.Value) == false)
                    {
                        prePosForLen = pos;
                        continue;
                    }
                }
                prePosForLen = pos;
                Point point = new Point();
                point.pos = GetPos(pathName, perc);
                point.perc = perc;
                point.distance = len;
                path.list.Add(point);
            }
            path.maxLen = len;
            pathDict[pathName] = path;
        }

        public Path GetNewPath(string pathName)
        {
            InitPathDict(pathName);
            return pathDict[pathName];
        }

        bool Check(Vector3 pos, Vector3 prePos, Vector3 dir)
        {
            float ignoreDistance = 0.1f;
            var v = pos - prePos;
            var projectVec3 = Vector3.Project(v, dir);
            float d = v.magnitude * v.magnitude - projectVec3.magnitude * projectVec3.magnitude;
            ignoreDistance *= ignoreDistance;
            if (d < ignoreDistance)
            {
                return false;
            }
            return true;
        }

        public void ShowSamplePoint(string pathName)
        {
            GameObject obj = GameObject.Find("sample");
            if (obj == null) obj = new GameObject("sample");
            
            var path = pathDict[pathName];
            for (int i = 0; i < path.list.Count; i++)
            {
                var p = path.list[i];
                var s = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                s.name = i.ToString();
                s.transform.parent = obj.transform;
                s.transform.localScale = Vector3.one * 0.1f;
                s.transform.position = p.pos;
            }
        }

        public void ClearPathDict()
        {
            pathDict.Clear();
        }

        public float GetPathLen(string pathName)
        {
            InitPathDict(pathName);
            if (pathDict.ContainsKey(pathName))
            {
                return pathDict[pathName].maxLen;
            }
            return 0;
        }

        /// <summary>
        /// 获得长度对应的真实百分比长度--
        /// </summary>
        /// <returns></returns>
        public float GetTruePercByDistance(string pathName, float distance)
        {
            return distance / GetNewPath(pathName).maxLen;
        }

        public Vector3 GetPosByTruePerc(string pathName,float truePerc)
        {
            InitPathDict(pathName);
            var path = pathDict[pathName];
            if (truePerc <= 0)
            {
                return path.list[0].pos;
            }
            float trueLen = path.maxLen * truePerc;
            Point prePoint = new Point();
            Point nextPoint = new Point();
            for (int i = 0; i < path.list.Count; i++)
            {
                var p = path.list[i];
                if (trueLen > p.distance)
                {
                    prePoint = p;
                }
                else
                {
                    nextPoint = p;
                    break;
                }
            }
            
            Vector3 convertPos = (trueLen - prePoint.distance) / (nextPoint.distance - prePoint.distance) * (nextPoint.pos - prePoint.pos) + prePoint.pos;
            if (float.IsNaN(convertPos.x) || float.IsNaN(convertPos.y) || float.IsNaN(convertPos.z))
            {
                Debug.LogWarning("GetPosByTruePerc is NaN.");
                return Vector3.zero;
            }
            return convertPos;
        }

        public Vector3 GetChuidian(Vector3 linePoint1,Vector3 linePoint2,Vector3 point)
        {
            var v1 = point - linePoint1;
            var v2 = linePoint2 - linePoint1;

            var v3 = point - linePoint2;
            var v4 = linePoint1 - linePoint2;

            bool isIn = Vector3.Dot(v1, v2) >= 0 && Vector3.Dot(v3, v4) >= 0;

            var dir = linePoint2 - linePoint1;
            var project = Vector3.Project(point-linePoint1, dir);
            var chuidianPos = project + linePoint1;
            var pl1 = Tools.Distance(chuidianPos, linePoint1);
            var pl2 = Tools.Distance(chuidianPos, linePoint2);
            if (isIn == false)
            {
                if (pl1 < pl2)
                {
                    return linePoint1;
                }
                else
                {
                    return linePoint2;
                }
            }
            return chuidianPos;
        }

        //获得垂点，可用--
        public void DebugPos(string wayName,Vector3 pos, ref Vector3 prePos,ref Vector3 nextPos,ref Vector3 chuidian) {
            var path = GetNewPath(wayName);
            var d = double.MaxValue;
            Point prePoint = new Point(), nextPoint = new Point();
            Vector3 chuidianPos = Vector3.zero;
            for (int i = 0; i < path.list.Count - 1; i++)
            {
                Point
                    p1 = path.list[i],
                    p2 = path.list[i + 1];
                chuidianPos = GetChuidian(p1.pos, p2.pos, pos);
                var newDistance = Tools.Distance(chuidianPos, pos);
                if (newDistance < d)
                {
                    prePoint = p1;
                    nextPoint = p2;
                    d = newDistance;
                    chuidian = chuidianPos;
                    prePos = prePoint.pos;
                    nextPos = nextPoint.pos;
                }
            }
        }

        /// <summary>
        /// 归位--
        /// </summary>
        /// <param name="wayName"></param>
        /// <param name="pos"></param>
        /// <param name="prePos"></param>
        /// <param name="nextPos"></param>
        /// <param name="chuidian"></param>
        public float GetWayTruePrecByPos(string wayName, Vector3 pos)
        {
            Vector3 resultCdPos = Vector3.zero;
            Point resultPrePoint = new Point(), resultNextPoint = new Point();
            var path = GetNewPath(wayName);
            var d = double.MaxValue;
            Point prePoint = new Point(), nextPoint = new Point();
            Vector3 chuidianPos = Vector3.zero;
            for (int i = 0; i < path.list.Count - 1; i++)
            {
                Point
                    p1 = path.list[i],
                    p2 = path.list[i + 1];
                chuidianPos = GetChuidian(p1.pos, p2.pos, pos);
                var newDistance = Tools.Distance(chuidianPos, pos);
                if (newDistance < d)
                {
                    prePoint = p1;
                    nextPoint = p2;
                    d = newDistance;

                    resultCdPos = chuidianPos;
                    resultPrePoint = prePoint;
                    resultNextPoint = nextPoint;

                }
            }

            float precOffset = Vector3.Distance(resultCdPos, resultPrePoint.pos) / Vector3.Distance(resultNextPoint.pos, resultPrePoint.pos);
            float perc = (resultPrePoint.distance + (resultNextPoint.distance - resultPrePoint.distance) * precOffset) / path.maxLen;
            return perc;
        }


        /// <summary>
        /// 获得切线方向,负方向--
        /// </summary>
        /// <returns></returns>
        public Vector3 GetTangentByTruePerc(string pathName, float truePerc)
        {
            return GetTangent(pathName, TruePerc2Perc(pathName, truePerc));
        }

        public float TruePerc2Perc(string pathName, float truePerc)
        {
            var path = GetNewPath(pathName);
            float trueLen = truePerc * path.maxLen;
            for (int i = 0; i < path.list.Count - 1; i++)
            {
                var prePoint = path.list[i];
                var nextPoint = path.list[i + 1];
                if (prePoint.distance <= trueLen && trueLen <= nextPoint.distance)
                {
                    float tmPerc = prePoint.perc + (trueLen - prePoint.distance) / (nextPoint.distance - prePoint.distance) * (nextPoint.perc - prePoint.perc);
                    return tmPerc;// return GetTangent(pathName, tmPerc);
                }
            }
            return 0;// Vector3.zero;
        }


        #endregion

        #region 等分

        //struct Point {
        //    /// <summary>
        //    /// 不均百分百--
        //    /// </summary>
        //    public float perc;
        //    /// <summary>
        //    /// 从0到perc的路径长度--
        //    /// </summary>
        //    public float distance;
        //    public Vector3 pos;
        //}

        //class Path
        //{
        //    public float len = 0;
        //    public Dictionary<float, Point> pointDict = new Dictionary<float, Point>();
        //}

        //Dictionary<string, Path> pathDict = new Dictionary<string, Path>();

        //float len = 0;
        //void InitPointDict(string pathName) {
        //    if (pathDict.ContainsKey(pathName)) return;
        //    Path path = new Path();
        //    float step = 0.001f;
        //    Vector3 lastPos = Vector3.zero;
        //    float lenMax = 0;
        //    for (float perc = 0; perc <= 1; perc += step)
        //    {
        //        Point p = new Point();
        //        p.perc = perc;
        //        p.pos = WaypointMgr.instance.GetPos(pathName, perc);
        //        p.distance = Vector3.Distance(p.pos, lastPos)+len;
        //        lastPos = p.pos;
        //        if (p.perc <= 0) continue;
        //        len = p.distance;
        //        lenMax = len;
        //        path.pointDict[perc] = p;
        //    }
        //    path.len = lenMax;
        //    pathDict[pathName] = path;
        //}

        //public Vector3 GetPosByTruePerc(string pathName, float perc)
        //{
        //    InitPointDict(pathName);
        //    Path p = pathDict[pathName];
        //    var tmLen = p.len * perc;
        //    Point prePoint = new Point(), nextPoint =new Point();
        //    p.pointDict.Loop((point) =>
        //    {
        //        if (point.Value.distance > tmLen)
        //        {
        //            prePoint = point.Value;
        //            return true;
        //        }
        //        nextPoint = point.Value;
        //        return false;
        //    });
        //    float tmPerc = (tmLen - prePoint.distance) / (nextPoint.distance - prePoint.distance) * (nextPoint.perc - prePoint.perc) + prePoint.perc;
        //    return WaypointMgr.instance.GetPos(pathName, tmPerc);
        //}

        #endregion

        #region 权重
        //Dictionary<string, List<float>> pathWeightDict = new Dictionary<string, List<float>>();

        //public void InitWeightData(string pathName)
        //{
        //    if (pathWeightDict.ContainsKey(pathName)) return;
        //    List<Transform> trans = GetPath(pathName);
        //    List<float> weightList = new List<float>();
        //    float precA = 0, precB = 0;
        //    for (int i = 1; i < trans.Count; i++)
        //    {
        //        precB = GetWaypointPercByPointIdx(pathName, i);
        //        weightList.Add(getWeight(pathName, precA, precB));
        //        precA = precB;
        //    }
        //    pathWeightDict[pathName] = weightList;
        //}

        //public float getWeight(string pathName, float precA, float precB)
        //{
        //    int count = 1;
        //    float step = (precB - precA) / count;
        //    Vector3 pos = GetPos(pathName, precA);
        //    float distance = 0;
        //    for (float prec = precA; prec <= precB; prec += step)
        //    {
        //        Vector3 tmPos = GetPos(pathName, prec);
        //        distance += Vector3.Distance(pos, tmPos);
        //        pos = tmPos;
        //    }
        //    return distance;
        //}

        //float getPathPercByTruePerc(string pathName, float truePerc)
        //{
        //    truePerc = Mathf.Clamp01(truePerc);
        //    //总长度--
        //    float len = 0;
        //    pathWeightDict[pathName].Loop((f) =>
        //    {
        //        len += f;
        //    });

        //    float trueLen = truePerc * len;
        //    len = 0;
        //    float nextLen = 0;
        //    int preIdx = 0;
        //    float preLen = 0;
        //    pathWeightDict[pathName].Loop((f) =>
        //    {
        //        if (f + len >= trueLen)
        //        {
        //            nextLen = f;
        //            return true;
        //        }
        //        len += f;
        //        preLen += f;
        //        preIdx++;
        //        return false;
        //    });
        //    float prePerc = WaypointMgr.instance.GetWaypointPercByPointIdx(pathName, preIdx);
        //    float nextPerc = WaypointMgr.instance.GetWaypointPercByPointIdx(pathName, preIdx+1);
        //    float perc = prePerc + (nextPerc - prePerc) * (trueLen - preLen) / nextLen;
        //    return perc;
        //}

        //public Vector3 GetPosByTruePrec(string pathName, float truePerc)
        //{
        //    WaypointMgr.instance.InitWeightData(pathName);
        //    float perc = getPathPercByTruePerc(pathName, truePerc);
        //    return GetPos(pathName, perc);
        //}

        //public static void Test(float truePerc) {
        //    string pathName = "way";
        //    WaypointMgr.instance.InitWeightData(pathName);
        //    GameObject obj = GameObject.Find("debug");
        //    obj.transform.position = WaypointMgr.instance.GetPosByTruePrec(pathName, truePerc);
        //}
        #endregion
    }
}