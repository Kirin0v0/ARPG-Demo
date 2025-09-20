using System;
using Character;
using UnityEngine;
using UnityEngine.Serialization;

namespace Damage.Data
{
    /// <summary>
    /// 伤害数值类，我们规定伤害为正，治疗为负
    /// </summary>
    [Serializable]
    public struct DamageValue
    {
        public int noType;
        public int physics;
        public int fire;
        public int ice;
        public int wind;
        public int lightning;

        public static DamageValue Zero = new DamageValue
        {
            noType = 0,
            physics = 0,
            fire = 0,
            ice = 0,
            wind = 0,
            lightning = 0,
        };

        /// <summary>
        /// 检查函数，我们规定治疗一定小等于0，攻击一定大等于0
        /// </summary>
        /// <param name="asHeal">是否作为治疗</param>
        /// <returns></returns>
        public DamageValue Check(bool asHeal)
        {
            return asHeal
                ? new DamageValue
                {
                    noType = Mathf.Min(0, noType),
                    physics = Mathf.Min(0, physics),
                    fire = Mathf.Min(0, fire),
                    ice = Mathf.Min(0, ice),
                    wind = Mathf.Min(0, wind),
                    lightning = Mathf.Min(0, lightning),
                }
                : new DamageValue
                {
                    noType = Mathf.Max(0, noType),
                    physics = Mathf.Max(0, physics),
                    fire = Mathf.Max(0, fire),
                    ice = Mathf.Max(0, ice),
                    wind = Mathf.Max(0, wind),
                    lightning = Mathf.Max(0, lightning),
                };
        }

        public int Total()
        {
            return noType + physics + fire + ice + wind + lightning;
        }

        public static DamageValue operator +(DamageValue a, DamageValue b)
        {
            return new DamageValue
            {
                noType = a.noType + b.noType,
                physics = a.physics + b.physics,
                fire = a.fire + b.fire,
                ice = a.ice + b.ice,
                wind = a.wind + b.wind,
                lightning = a.lightning + b.lightning,
            };
        }

        public static DamageValue operator -(DamageValue a)
        {
            return new DamageValue
            {
                noType = -a.noType,
                physics = -a.physics,
                fire = -a.fire,
                ice = -a.ice,
                wind = -a.wind,
                lightning = -a.lightning,
            };
        }

        public static DamageValue operator -(DamageValue a, DamageValue b)
        {
            return new DamageValue
            {
                noType = a.noType - b.noType,
                physics = a.physics - b.physics,
                fire = a.fire - b.fire,
                ice = a.ice - b.ice,
                wind = a.wind - b.wind,
                lightning = a.lightning - b.lightning,
            };
        }

        public static DamageValue operator *(DamageValue a, float b)
        {
            return new DamageValue
            {
                noType = (int)(a.noType * b),
                physics = (int)(a.physics * b),
                fire = (int)(a.fire * b),
                ice = (int)(a.ice * b),
                wind = (int)(a.wind * b),
                lightning = (int)(a.lightning * b),
            };
        }

        public static DamageValue operator *(float a, DamageValue b)
        {
            return b * a;
        }

        public static DamageValue operator /(DamageValue a, float b)
        {
            return new DamageValue
            {
                noType = (int)(1f * a.noType / b),
                physics = (int)(1f * a.physics / b),
                fire = (int)(1f * a.fire / b),
                ice = (int)(1f * a.ice / b),
                wind = (int)(1f * a.wind / b),
                lightning = (int)(1f * a.lightning / b),
            };
        }

        public static DamageValue operator /(float a, DamageValue b)
        {
            return b / a;
        }
    }

    /// <summary>
    /// 伤害乘区类
    /// </summary>
    [Serializable]
    public struct DamageValueMultiplier
    {
        public float noType;
        public float physics;
        public float fire;
        public float ice;
        public float wind;
        public float lightning;

        public static DamageValueMultiplier Zero = new DamageValueMultiplier
        {
            noType = 0,
            physics = 0,
            fire = 0,
            ice = 0,
            wind = 0,
            lightning = 0,
        };

        public static DamageValueMultiplier One = new DamageValueMultiplier
        {
            noType = 1,
            physics = 1,
            fire = 1,
            ice = 1,
            wind = 1,
            lightning = 1,
        };

        public static DamageValueMultiplier operator -(DamageValueMultiplier a)
        {
            return new DamageValueMultiplier
            {
                noType = -a.noType,
                physics = -a.physics,
                fire = -a.fire,
                ice = -a.ice,
                wind = -a.wind,
                lightning = -a.lightning,
            };
        }

        /// <summary>
        /// 检查函数，我们规定治疗一定小等于0，攻击一定大等于0
        /// </summary>
        /// <param name="asHeal">是否作为治疗</param>
        /// <returns></returns>
        public DamageValueMultiplier Check(bool asHeal)
        {
            return asHeal
                ? new DamageValueMultiplier
                {
                    noType = Mathf.Min(0, noType),
                    physics = Mathf.Min(0, physics),
                    fire = Mathf.Min(0, fire),
                    ice = Mathf.Min(0, ice),
                    wind = Mathf.Min(0, wind),
                    lightning = Mathf.Min(0, lightning),
                }
                : new DamageValueMultiplier
                {
                    noType = Mathf.Max(0, noType),
                    physics = Mathf.Max(0, physics),
                    fire = Mathf.Max(0, fire),
                    ice = Mathf.Max(0, ice),
                    wind = Mathf.Max(0, wind),
                    lightning = Mathf.Max(0, lightning),
                };
        }
    }
}