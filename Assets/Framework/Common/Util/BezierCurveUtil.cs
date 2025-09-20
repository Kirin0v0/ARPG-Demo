using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Framework.Common.Util
{
    public static class BezierCurveUtil
    {
        /// <summary>
        /// 获取指定参数的一阶贝塞尔曲线的某一点
        /// </summary>
        /// <param name="a">曲线起始点</param>
        /// <param name="b">曲线结束点</param>
        /// <param name="t">某点在从起始点到结束点的曲线的位置（0~1区间内）</param>
        /// <returns>某点位置</returns>
        public static Vector3 CalculateBezierCurve(Vector3 a, Vector3 b, float t)
        {
            return Vector3.Lerp(a, b, t);
        }

        /// <summary>
        /// 获取指定参数的二阶贝塞尔曲线的某一点
        /// </summary>
        /// <param name="a">曲线起始点</param>
        /// <param name="b">曲线控制点1</param>
        /// <param name="c">曲线结束点</param>
        /// <param name="t">某点在从起始点到结束点的曲线的位置（0~1区间内）</param>
        /// <returns>某点位置</returns>
        public static Vector3 CalculateBezierCurve(Vector3 a, Vector3 b, Vector3 c, float t)
        {
            var ab = Vector3.Lerp(a, b, t);
            var bc = Vector3.Lerp(b, c, t);
            return Vector3.Lerp(ab, bc, t);
        }

        /// <summary>
        /// 获取指定参数的三阶贝塞尔曲线的某一点
        /// </summary>
        /// <param name="a">曲线起始点</param>
        /// <param name="b">曲线控制点1</param>
        /// <param name="c">曲线控制点2</param>
        /// <param name="d">曲线结束点</param>
        /// <param name="t">某点在从起始点到结束点的曲线的位置（0~1区间内）</param>
        /// <returns>某点位置</returns>
        public static Vector3 CalculateBezierCurve(Vector3 a, Vector3 b, Vector3 c, Vector3 d, float t)
        {
            var ab = Vector3.Lerp(a, b, t);
            var bc = Vector3.Lerp(b, c, t);
            var cd = Vector3.Lerp(c, d, t);
            var abc = Vector3.Lerp(ab, bc, t);
            var bcd = Vector3.Lerp(bc, cd, t);
            return Vector3.Lerp(abc, bcd, t);
        }

        /// <summary>
        /// 获取指定参数的四阶贝塞尔曲线的某一点
        /// </summary>
        /// <param name="a">曲线起始点</param>
        /// <param name="b">曲线控制点1</param>
        /// <param name="c">曲线控制点2</param>
        /// <param name="d">曲线控制点3</param>
        /// <param name="e">曲线结束点</param>
        /// <param name="t">某点在从起始点到结束点的曲线的位置（0~1区间内）</param>
        /// <returns>某点位置</returns>
        public static Vector3 CalculateBezierCurve(Vector3 a, Vector3 b, Vector3 c, Vector3 d, Vector3 e, float t)
        {
            var ab = Vector3.Lerp(a, b, t);
            var bc = Vector3.Lerp(b, c, t);
            var cd = Vector3.Lerp(c, d, t);
            var de = Vector3.Lerp(d, e, t);
            var abc = Vector3.Lerp(ab, bc, t);
            var bcd = Vector3.Lerp(bc, cd, t);
            var cde = Vector3.Lerp(cd, de, t);
            var abcd = Vector3.Lerp(abc, bcd, t);
            var bcde = Vector3.Lerp(bcd, cde, t);
            return Vector3.Lerp(abcd, bcde, t);
        }

        /// <summary>
        /// 获取指定参数的五阶贝塞尔曲线的某一点
        /// </summary>
        /// <param name="a">曲线起始点</param>
        /// <param name="b">曲线控制点1</param>
        /// <param name="c">曲线控制点2</param>
        /// <param name="d">曲线控制点3</param>
        /// <param name="e">曲线控制点4</param>
        /// <param name="f">曲线结束点</param>
        /// <param name="t">某点在从起始点到结束点的曲线的位置（0~1区间内）</param>
        /// <returns>某点位置</returns>
        public static Vector3 CalculateBezierCurve(Vector3 a, Vector3 b, Vector3 c, Vector3 d, Vector3 e, Vector3 f,
            float t)
        {
            var ab = Vector3.Lerp(a, b, t);
            var bc = Vector3.Lerp(b, c, t);
            var cd = Vector3.Lerp(c, d, t);
            var de = Vector3.Lerp(d, e, t);
            var ef = Vector3.Lerp(e, f, t);
            var abc = Vector3.Lerp(ab, bc, t);
            var bcd = Vector3.Lerp(bc, cd, t);
            var cde = Vector3.Lerp(cd, de, t);
            var def = Vector3.Lerp(de, ef, t);
            var abcd = Vector3.Lerp(abc, bcd, t);
            var bcde = Vector3.Lerp(bcd, cde, t);
            var cdef = Vector3.Lerp(cde, def, t);
            var abcde = Vector3.Lerp(abcd, bcde, t);
            var bcdef = Vector3.Lerp(bcde, cdef, t);
            return Vector3.Lerp(abcde, bcdef, t);
        }
    }
}