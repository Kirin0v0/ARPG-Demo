using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UIElements;
using Random = UnityEngine.Random;

namespace Framework.Common.Util
{
    public static class MathUtil
    {
        public static string RandomId()
        {
            return Guid.NewGuid().ToString();
            return $"{DateTimeOffset.Now.ToUnixTimeMilliseconds()}{Random.Range(0, 100000)}";
        }

        /// <summary>
        /// 角度转弧度的方法
        /// </summary>
        /// <param name="deg">角度值</param>
        /// <returns>弧度值</returns>
        public static float Deg2Rad(float deg)
        {
            return deg * Mathf.Deg2Rad;
        }

        /// <summary>
        /// 弧度转角度的方法
        /// </summary>
        /// <param name="rad">弧度值</param>
        /// <returns>角度值</returns>
        public static float Rad2Deg(float rad)
        {
            return rad * Mathf.Rad2Deg;
        }

        public enum TwoDimensionAxisType
        {
            XY,
            XZ,
            YZ,
        }

        /// <summary>
        /// 获取相应两轴平面上两点的距离
        /// </summary>
        /// <param name="srcPosition">点1</param>
        /// <param name="targetPosition">点2</param>
        /// <param name="axisType">两轴类型</param>
        /// <returns>距离值</returns>
        public static float GetDistance(Vector3 srcPosition, Vector3 targetPosition, TwoDimensionAxisType axisType)
        {
            switch (axisType)
            {
                case TwoDimensionAxisType.XZ:
                    srcPosition.y = 0;
                    targetPosition.y = 0;
                    break;
                case TwoDimensionAxisType.YZ:
                    srcPosition.x = 0;
                    targetPosition.x = 0;
                    break;
                case TwoDimensionAxisType.XY:
                    srcPosition.z = 0;
                    targetPosition.z = 0;
                    break;
            }

            return Vector3.Distance(srcPosition, targetPosition);
        }

        /// <summary>
        /// 判断相应两轴平面上两点的距离是否小于目标距离
        /// </summary>
        /// <param name="srcPosition">点1</param>
        /// <param name="targetPosition">点2</param>
        /// <param name="distance">距离</param>
        /// <param name="axisType">两轴类型</param>
        /// <returns>是否小于目标距离</returns>
        public static bool IsLessThanDistance(Vector3 srcPosition, Vector3 targetPosition, float distance,
            TwoDimensionAxisType axisType)
        {
            switch (axisType)
            {
                case TwoDimensionAxisType.XZ:
                    srcPosition.y = 0;
                    targetPosition.y = 0;
                    break;
                case TwoDimensionAxisType.YZ:
                    srcPosition.x = 0;
                    targetPosition.x = 0;
                    break;
                case TwoDimensionAxisType.XY:
                    srcPosition.z = 0;
                    targetPosition.z = 0;
                    break;
            }

            return (targetPosition - srcPosition).sqrMagnitude < distance * distance;
        }

        /// <summary>
        /// 判断相应两轴平面上两点的距离是否大于目标距离
        /// </summary>
        /// <param name="srcPosition">点1</param>
        /// <param name="targetPosition">点2</param>
        /// <param name="distance">距离</param>
        /// <param name="axisType">两轴类型</param>
        /// <returns>是否大于目标距离</returns>
        public static bool IsMoreThanDistance(Vector3 srcPosition, Vector3 targetPosition, float distance,
            TwoDimensionAxisType axisType)
        {
            switch (axisType)
            {
                case TwoDimensionAxisType.XZ:
                    srcPosition.y = 0;
                    targetPosition.y = 0;
                    break;
                case TwoDimensionAxisType.YZ:
                    srcPosition.x = 0;
                    targetPosition.x = 0;
                    break;
                case TwoDimensionAxisType.XY:
                    srcPosition.z = 0;
                    targetPosition.z = 0;
                    break;
            }

            return (targetPosition - srcPosition).sqrMagnitude > distance * distance;
        }

        /// <summary>
        /// 判断相应两轴平面上两点的距离是否等于目标距离
        /// </summary>
        /// <param name="srcPosition">点1</param>
        /// <param name="targetPosition">点2</param>
        /// <param name="distance">距离</param>
        /// <param name="axisType">两轴类型</param>
        /// <returns>是否大于目标距离</returns>
        public static bool IsEqualDistance(Vector3 srcPosition, Vector3 targetPosition, float distance,
            TwoDimensionAxisType axisType)
        {
            switch (axisType)
            {
                case TwoDimensionAxisType.XZ:
                    srcPosition.y = 0;
                    targetPosition.y = 0;
                    break;
                case TwoDimensionAxisType.YZ:
                    srcPosition.x = 0;
                    targetPosition.x = 0;
                    break;
                case TwoDimensionAxisType.XY:
                    srcPosition.z = 0;
                    targetPosition.z = 0;
                    break;
            }

            return Mathf.Approximately((targetPosition - srcPosition).sqrMagnitude, distance * distance);
        }

        /// <summary>
        /// 获取盒子顶点
        /// </summary>
        /// <param name="center"></param>
        /// <param name="boxSize"></param>
        /// <returns></returns>
        public static Vector3[] GetBoxVertices(Vector3 center, Vector3 boxSize)
        {
            Vector3[] vertices = new Vector3[8];

            var halfX = boxSize.x / 2.0f;
            var halfY = boxSize.y / 2.0f;
            var halfZ = boxSize.z / 2.0f;

            vertices[0] = center + new Vector3(-halfX, -halfY, -halfZ);
            vertices[1] = center + new Vector3(halfX, -halfY, -halfZ);
            vertices[2] = center + new Vector3(halfX, halfY, -halfZ);
            vertices[3] = center + new Vector3(-halfX, halfY, -halfZ);
            vertices[4] = center + new Vector3(-halfX, -halfY, halfZ);
            vertices[5] = center + new Vector3(halfX, -halfY, halfZ);
            vertices[6] = center + new Vector3(halfX, halfY, halfZ);
            vertices[7] = center + new Vector3(-halfX, halfY, halfZ);

            return vertices;
        }

        /// <summary>
        /// 判断世界坐标系下的某一个点是否在屏幕可见范围内
        /// </summary>
        /// <param name="camera">屏幕相机</param>
        /// <param name="position">世界坐标系下的一个点的位置</param>
        /// <returns>如果在可见范围内返回true，否则返回false</returns>
        public static bool IsWorldPositionInScreen(UnityEngine.Camera camera, Vector3 position)
        {
            // 不在相机视野内就不用执行后续逻辑
            if (!camera)
            {
                return false;
            }

            var viewportPoint = camera.WorldToViewportPoint(position);
            if (viewportPoint.x < 0 || viewportPoint.x > 1 ||
                viewportPoint.y < 0 || viewportPoint.y > 1 ||
                viewportPoint.z <= 0)
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// 判断世界坐标系下的某个盒装物体是否在屏幕可见范围内
        /// </summary>
        /// <param name="camera">屏幕相机</param>
        /// <param name="center">世界坐标系下的盒装物体的中心点</param>
        /// <param name="boxSize">世界坐标系下的盒装物体的尺寸</param>
        /// <returns>如果在可见范围内返回true，否则返回false</returns>
        /// <returns></returns>
        public static bool IsWorldBoxInScreen(UnityEngine.Camera camera, Vector3 center, Vector3 boxSize)
        {
            foreach (var vertex in GetBoxVertices(center, boxSize))
            {
                if (IsWorldPositionInScreen(camera, vertex))
                {
                    return true;
                }
            }

            return false;
        }

        public static Vector2 GetGUIScreenPosition(UnityEngine.Camera camera, Vector3 worldPosition)
        {
            var screenPoint = camera.WorldToScreenPoint(worldPosition);
            var screenPosition = new Vector2(screenPoint.x, Screen.height - screenPoint.y);
            return screenPosition;
        }

        public static Vector2 GetCanvasScreenPosition(UnityEngine.Camera camera, Vector3 worldPosition)
        {
            var screenPoint = camera.WorldToScreenPoint(worldPosition);
            var screenPosition = new Vector2(screenPoint.x, screenPoint.y);
            return screenPosition;
        }

        /// <summary>
        /// 判断某一个位置是否在指定四个顶点组成的四边形范围内（注意：传入的坐标向量都必须是基于同一个坐标系下的）
        /// </summary>
        /// <param name="vertexAPosition">四边形A顶点位置</param>
        /// <param name="vertexBPosition">四边形B顶点位置</param>
        /// <param name="vertexCPosition">四边形C顶点位置</param>
        /// <param name="vertexDPosition">四边形D顶点位置</param>
        /// <param name="targetPosition">目标对象位置</param>
        /// <returns></returns>
        public static bool InQuadrilateral(Vector3 vertexAPosition, Vector3 vertexBPosition, Vector3 vertexCPosition,
            Vector3 vertexDPosition, Vector3 targetPosition)
        {
            //先计算出所需要的叉积
            Vector3 v0 = Vector3.Cross(vertexAPosition - vertexDPosition, targetPosition - vertexDPosition);
            Vector3 v1 = Vector3.Cross(vertexBPosition - vertexAPosition, targetPosition - vertexAPosition);
            Vector3 v2 = Vector3.Cross(vertexCPosition - vertexBPosition, targetPosition - vertexBPosition);
            Vector3 v3 = Vector3.Cross(vertexDPosition - vertexCPosition, targetPosition - vertexCPosition);
            if (Vector3.Dot(v0, v1) < 0 || Vector3.Dot(v0, v2) < 0 || Vector3.Dot(v0, v3) < 0)
            {
                return false;
            }
            else
            {
                return true;
            }
        }

        /// <summary>
        /// 判断某一个位置是否在指定扇形范围内（注意：传入的坐标向量都必须是基于同一个坐标系下的）
        /// </summary>
        /// <param name="position">扇形中心点位置</param>
        /// <param name="centerForward">扇形中心线的面朝向</param>
        /// <param name="targetPosition">目标对象位置</param>
        /// <param name="radius">半径</param>
        /// <param name="angle">扇形的角度</param>
        /// <param name="axisType">坐标轴类型</param>
        /// <returns>是否处于范围内</returns>
        public static bool InSector(
            Vector3 position,
            Vector3 centerForward,
            Vector3 targetPosition,
            float radius,
            float angle,
            TwoDimensionAxisType axisType
        )
        {
            switch (axisType)
            {
                case TwoDimensionAxisType.XY:
                    position.z = 0;
                    centerForward.z = 0;
                    targetPosition.z = 0;
                    break;
                case TwoDimensionAxisType.XZ:
                    position.y = 0;
                    centerForward.y = 0;
                    targetPosition.y = 0;
                    break;
                case TwoDimensionAxisType.YZ:
                    position.x = 0;
                    centerForward.x = 0;
                    targetPosition.x = 0;
                    break;
            }

            return Vector3.Distance(position, targetPosition) <= radius &&
                   Vector3.Angle(centerForward, targetPosition - position) <= angle / 2f;
        }

        /// <summary>
        /// 判断某一个位置是否在指定椭圆范围内（注意：传入的坐标向量都必须是基于同一个坐标系下的）
        /// </summary>
        /// <param name="position">中心点位置</param>
        /// <param name="targetPosition">目标对象位置</param>
        /// <param name="radiusAxis1">坐标轴1的椭圆半径,例如坐标轴为XY时，该值指X轴半径</param>
        /// <param name="radiusAxis2">坐标轴2的椭圆半径,例如坐标轴为XY时，该值指Y轴半径</param>
        /// <param name="axisType">坐标轴类型</param>
        /// <returns>是否处于范围内</returns>
        public static bool InOval(
            Vector3 position,
            Vector3 targetPosition,
            float radiusAxis1,
            float radiusAxis2,
            TwoDimensionAxisType axisType
        )
        {
            var result = axisType switch
            {
                TwoDimensionAxisType.XY => Mathf.Pow(position.x - targetPosition.x, 2) / Mathf.Pow(radiusAxis1, 2) +
                                           Mathf.Pow(position.y - targetPosition.y, 2) / Mathf.Pow(radiusAxis2, 2),
                TwoDimensionAxisType.XZ => Mathf.Pow(position.x - targetPosition.x, 2) / Mathf.Pow(radiusAxis1, 2) +
                                           Mathf.Pow(position.z - targetPosition.z, 2) / Mathf.Pow(radiusAxis2, 2),
                TwoDimensionAxisType.YZ => Mathf.Pow(position.y - targetPosition.y, 2) / Mathf.Pow(radiusAxis1, 2) +
                                           Mathf.Pow(position.z - targetPosition.z, 2) / Mathf.Pow(radiusAxis2, 2),
            };
            return result <= 1;
        }

        /// <summary>
        /// 射线检测获取指定距离指定层级的第一个碰撞对象
        /// </summary>
        /// <param name="ray">射线</param>
        /// <param name="maxDistance">最大距离</param>
        /// <param name="layerNames">层级名称数组，空代表全部层级</param>
        public static RaycastHit Raycast(Ray ray, float maxDistance, string[] layerNames = null)
        {
            RaycastHit hitInfo;
            if (layerNames == null)
            {
                Physics.Raycast(ray, out hitInfo, maxDistance);
            }
            else
            {
                Physics.Raycast(ray, out hitInfo, maxDistance, LayerMask.GetMask(layerNames));
            }

            return hitInfo;
        }

        /// <summary>
        /// 射线检测获取指定距离指定层级的第一个碰撞对象
        /// </summary>
        /// <param name="ray">射线</param>
        /// <param name="callBack">回调函数（会把碰到的RayCastHit信息传递出去）</param>
        /// <param name="maxDistance">最大距离</param>
        /// <param name="layerNames">层级名称数组，空代表全部层级</param>
        public static void Raycast(Ray ray, UnityAction<RaycastHit> callBack, float maxDistance,
            string[] layerNames = null)
        {
            RaycastHit hitInfo;
            if (layerNames == null)
            {
                if (Physics.Raycast(ray, out hitInfo, maxDistance))
                {
                    callBack(hitInfo);
                }
            }
            else
            {
                if (Physics.Raycast(ray, out hitInfo, maxDistance, LayerMask.GetMask(layerNames)))
                {
                    callBack(hitInfo);
                }
            }
        }

        /// <summary>
        /// 射线检测获取指定距离指定层级的全部碰撞对象
        /// </summary>
        /// <param name="ray">射线</param>
        /// <param name="maxDistance">最大距离</param>
        /// <param name="layerNames">层级名称数组，空代表全部层级</param>
        public static RaycastHit[] RaycastAll(Ray ray, float maxDistance, string[] layerNames = null)
        {
            var hitInfos = layerNames == null
                ? Physics.RaycastAll(ray, maxDistance)
                : Physics.RaycastAll(ray, maxDistance, LayerMask.GetMask(layerNames));
            return hitInfos;
        }

        /// <summary>
        /// 射线检测获取指定距离指定层级的全部碰撞对象
        /// </summary>
        /// <param name="ray">射线</param>
        /// <param name="maxDistance">最大距离</param>
        /// <param name="layerNames">层级名称数组，空代表全部层级</param>
        public static void RaycastAll(
            Ray ray,
            UnityAction<RaycastHit> callBack,
            float maxDistance,
            string[] layerNames = null
        )
        {
            RaycastHit[] hitInfos;
            if (layerNames == null)
            {
                hitInfos = Physics.RaycastAll(ray, maxDistance);
            }
            else
            {
                hitInfos = Physics.RaycastAll(ray, maxDistance, LayerMask.GetMask(layerNames));
            }

            for (int i = 0; i < hitInfos.Length; i++)
                callBack.Invoke(hitInfos[i]);
        }

        /// <summary>
        /// 进行盒状范围检测
        /// </summary>
        /// <typeparam name="T">想要获取的信息类型，可以填写Collider、GameObject以及对象上依附的组件类型</typeparam>
        /// <param name="center">盒状中心点</param>
        /// <param name="rotation">盒子的角度</param>
        /// <param name="halfExtents">长宽高的一半</param>
        /// <param name="callBack">回调函数</param>
        /// <param name="layerNames">层级名称数组，空代表全部层级</param>
        public static void OverlapBox<T>(
            Vector3 center,
            Quaternion rotation,
            Vector3 halfExtents,
            UnityAction<T> callBack,
            string[] layerNames = null
        ) where T : class
        {
            Type type = typeof(T);
            int layerMask = layerNames != null ? LayerMask.GetMask(layerNames) : Physics.AllLayers;
            Collider[] colliders =
                Physics.OverlapBox(center, halfExtents, rotation, layerMask, QueryTriggerInteraction.Collide);
            for (int i = 0; i < colliders.Length; i++)
            {
                if (type == typeof(Collider))
                    callBack.Invoke(colliders[i] as T);
                else if (type == typeof(GameObject))
                    callBack.Invoke(colliders[i].gameObject as T);
                else
                {
                    var component = colliders[i].gameObject.GetComponent<T>();
                    if (component != null)
                    {
                        callBack.Invoke(component);
                    }
                }
            }
        }

        /// <summary>
        /// 进行球体范围检测
        /// </summary>
        /// <typeparam name="T">想要获取的信息类型，可以填写Collider、GameObject以及对象上依附的组件类型</typeparam>
        /// <param name="center">球体的中心点</param>
        /// <param name="radius">球体的半径</param>
        /// <param name="callBack">回调函数</param>
        /// <param name="layerNames">层级名称数组，空代表全部层级</param>
        public static void OverlapSphere<T>(
            Vector3 center,
            float radius,
            UnityAction<T> callBack,
            string[] layerNames = null
        ) where T : class
        {
            Type type = typeof(T);
            int layerMask = layerNames != null ? LayerMask.GetMask(layerNames) : Physics.AllLayers;
            Collider[] colliders =
                Physics.OverlapSphere(center, radius, layerMask, QueryTriggerInteraction.Collide);
            for (int i = 0; i < colliders.Length; i++)
            {
                if (type == typeof(Collider))
                    callBack.Invoke(colliders[i] as T);
                else if (type == typeof(GameObject))
                    callBack.Invoke(colliders[i].gameObject as T);
                else
                {
                    var component = colliders[i].gameObject.GetComponent<T>();
                    if (component != null)
                    {
                        callBack.Invoke(component);
                    }
                }
            }
        }
    }
}