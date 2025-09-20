using System;
using System.Text;
using UnityEngine.Events;

namespace Framework.Common.Util
{
    public static class TextUtil
    {
        private static readonly StringBuilder ResultStr = new("");

        private static readonly string[] Digits = { "零", "一", "二", "三", "四", "五", "六", "七", "八", "九" };
        private static readonly string[] Units = { "", "十", "百", "千" };
        private static readonly string[] Sections = { "", "万", "亿" };

        /// <summary>
        /// 将非负数自然数数字转为对应的汉字
        /// </summary>
        /// <param name="number"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        public static string ConvertToChineseNumeral(int number)
        {
            if (number < 0)
            {
                throw new ArgumentOutOfRangeException("仅支持非负整数！");
            }

            var result = string.Empty;
            var zeroCount = 0;
            var lastZero = false;

            for (var i = 0; number > 0 || i < 8; i++)
            {
                var section = number % 10000;
                number /= 10000;

                var currentSection = ConvertFourDigits(section, Digits, Units);

                if (section == 0)
                {
                    zeroCount++;
                    lastZero = true;
                    continue;
                }

                if (zeroCount > 0 && !lastZero)
                    result = "零" + result;

                result = currentSection + Sections[i] + result;
                zeroCount = 0;
                lastZero = false;
            }

            return result == string.Empty ? "零" : result;
        }

        private static string ConvertFourDigits(int num, string[] digits, string[] units)
        {
            var result = string.Empty;
            var digitIndex = 0;

            while (num > 0)
            {
                var digit = num % 10;
                if (digit != 0)
                    result = digits[digit] + units[digitIndex] + result;
                else if (result.Length > 0 && result[0] != '零')
                    result = "零" + result;

                num /= 10;
                digitIndex++;
            }

            return result;
        }

        /// <summary>
        /// 拆分字符串并返回字符串数组
        /// </summary>
        /// <param name="str">想要被拆分的字符串</param>
        /// <param name="type">拆分字符类型： 1-; 2-, 3-% 4-: 5-空格 6-| 7-_ </param>
        /// <returns></returns>
        public static string[] SplitStr(string str, int type = 1)
        {
            if (str == "")
                return Array.Empty<string>();
            var newStr = str;
            switch (type)
            {
                case 1:
                    // 为了避免英文符号填成了中文符号 我们先进行一个替换
                    while (newStr.IndexOf("；") != -1)
                        newStr = newStr.Replace("；", ";");
                    return newStr.Split(';');
                case 2:
                    // 为了避免英文符号填成了中文符号 我们先进行一个替换
                    while (newStr.IndexOf("，") != -1)
                        newStr = newStr.Replace("，", ",");
                    return newStr.Split(',');
                case 3:
                    return newStr.Split('%');
                case 4:
                    // 为了避免英文符号填成了中文符号 我们先进行一个替换
                    while (newStr.IndexOf("：") != -1)
                        newStr = newStr.Replace("：", ":");
                    return newStr.Split(':');
                case 5:
                    return newStr.Split(' ');
                case 6:
                    return newStr.Split('|');
                case 7:
                    return newStr.Split('_');
                default:
                    return Array.Empty<string>();
            }
        }

        /// <summary>
        /// 拆分字符串并返回整形数组
        /// </summary>
        /// <param name="str">想要被拆分的字符串</param>
        /// <param name="type">拆分字符类型： 1-; 2-, 3-% 4-: 5-空格 6-| 7-_ </param>
        /// <returns></returns>
        public static int[] SplitStrToIntArr(string str, int type = 1)
        {
            // 得到拆分后的字符串数组
            string[] strs = SplitStr(str, type);
            if (strs.Length == 0)
                return new int[0];
            // 把字符串数组转换成int数组 
            return Array.ConvertAll<string, int>(strs, (str) => { return int.Parse(str); });
        }

        /// <summary>
        /// 专门用来拆分多组键值对形式的数据的，以int返回
        /// </summary>
        /// <param name="str">待拆分的字符串</param>
        /// <param name="typeOne">组间分隔符  1-; 2-, 3-% 4-: 5-空格 6-| 7-_ </param>
        /// <param name="typeTwo">键值对分隔符 1-; 2-, 3-% 4-: 5-空格 6-| 7-_ </param>
        /// <param name="callBack">回调函数</param>
        public static void SplitStrToIntArrTwice(string str, int typeOne, int typeTwo, UnityAction<int, int> callBack)
        {
            string[] strs = SplitStr(str, typeOne);
            if (strs.Length == 0)
                return;
            int[] ints;
            for (int i = 0; i < strs.Length; i++)
            {
                // 拆分单个道具的ID和数量信息
                ints = SplitStrToIntArr(strs[i], typeTwo);
                if (ints.Length == 0)
                    continue;
                callBack.Invoke(ints[0], ints[1]);
            }
        }

        /// <summary>
        /// 专门用来拆分多组键值对形式的数据的，以string返回
        /// </summary>
        /// <param name="str">待拆分的字符串</param>
        /// <param name="typeOne">组间分隔符 1-; 2-, 3-% 4-: 5-空格 6-| 7-_ </param>
        /// <param name="typeTwo">键值对分隔符  1-; 2-, 3-% 4-: 5-空格 6-| 7-_ </param>
        /// <param name="callBack">回调函数</param>
        public static void SplitStrTwice(string str, int typeOne, int typeTwo, UnityAction<string, string> callBack)
        {
            string[] strs = SplitStr(str, typeOne);
            if (strs.Length == 0)
                return;
            string[] strs2;
            for (int i = 0; i < strs.Length; i++)
            {
                // 拆分单个道具的ID和数量信息
                strs2 = SplitStr(strs[i], typeTwo);
                if (strs2.Length == 0)
                    continue;
                callBack.Invoke(strs2[0], strs2[1]);
            }
        }

        /// <summary>
        /// 字符串是否存在多行
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static bool IsMultipleLines(string str)
        {
            return str.Contains("\n");
        }

        /// <summary>
        /// 获取指定行文本
        /// </summary>
        public static string GetLineText(string str, int line)
        {
            var lines = str.Split('\n');
            var index = line < 0 ? 0 : line;
            if (index >= lines.Length)
            {
                return lines.Length == 0 ? "" : lines[^1];
            }

            return lines[index];
        }

        /// <summary>
        /// 得到指定长度的数字转字符串内容，如果长度不够会在前面补0，如果长度超出，会保留原始数值
        /// </summary>
        /// <param name="value">数值</param>
        /// <param name="len">长度</param>
        /// <returns></returns>
        public static string GetNumStr(int value, int len)
        {
            // ToString传入一个Dn的字符串，代表想要将数字转换为长度位n的字符串，如果长度不够，会在前面补0
            return value.ToString($"D{len}");
        }

        /// <summary>
        /// 让指定浮点数保留小数点后n位
        /// </summary>
        /// <param name="value">具体的浮点数</param>
        /// <param name="len">保留小数点后n位</param>
        /// <returns></returns>
        public static string GetDecimalStr(float value, int len)
        {
            // ToString中传入一个Fn的字符串，代表想要保留小数点后几位小数
            return value.ToString($"F{len}");
        }

        /// <summary>
        /// 秒转时分秒格式 其中时分秒可以自己传
        /// </summary>
        /// <param name="s">秒数</param>
        /// <param name="egZero">是否忽略0</param>
        /// <param name="isKeepLen">是否保留至少2位</param>
        /// <param name="hourStr">小时的拼接字符</param>
        /// <param name="minuteStr">分钟的拼接字符</param>
        /// <param name="secondStr">秒的拼接字符</param>
        /// <returns></returns>
        public static string SecondToHMS(int s, bool egZero = false, bool isKeepLen = false, string hourStr = "时",
            string minuteStr = "分", string secondStr = "秒")
        {
            // 时间不会有负数 所以我们如果发现是负数直接归0
            if (s < 0)
                s = 0;
            // 计算小时
            int hour = s / 3600;
            // 计算分钟
            int second = s % 3600;
            int minute = second / 60;
            // 计算秒
            second = s % 60;
            // 拼接
            ResultStr.Clear();
            // 如果小时不为0或者不忽略0 
            if (hour != 0 || !egZero)
            {
                ResultStr.Append(isKeepLen ? GetNumStr(hour, 2) : hour);
                ResultStr.Append(hourStr);
            }

            // 如果分钟不为0或者不忽略0或者小时不为0
            if (minute != 0 || !egZero || hour != 0)
            {
                ResultStr.Append(isKeepLen ? GetNumStr(minute, 2) : minute);
                ResultStr.Append(minuteStr);
            }

            // 如果秒不为0或者不忽略0或者小时和分钟不为0
            if (second != 0 || !egZero || hour != 0 || minute != 0)
            {
                ResultStr.Append(isKeepLen ? GetNumStr(second, 2) : second);
                ResultStr.Append(secondStr);
            }

            // 如果传入的参数是0秒时
            if (ResultStr.Length == 0)
            {
                ResultStr.Append(0);
                ResultStr.Append(secondStr);
            }

            return ResultStr.ToString();
        }

        /// <summary>
        /// 秒转00:00:00格式
        /// </summary>
        /// <param name="s"></param>
        /// <param name="egZero"></param>
        /// <returns></returns>
        public static string SecondToHms2(int s, bool egZero = false)
        {
            return SecondToHMS(s, egZero, true, ":", ":", "");
        }
    }
}