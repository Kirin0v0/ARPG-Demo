using System;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace Common
{
    public class CheckTMPInputFieldLengthBehaviour : MonoBehaviour
    {
        private enum SplitType
        {
            ASCII,
            GB,
            Unicode,
            UTF8,
        }

        [SerializeField] private TMP_InputField input;
        [SerializeField] private SplitType splitType = SplitType.ASCII;

        private int _characterLimit;

        private void OnEnable()
        {
            input.onValueChanged.AddListener(Check);
        }

        private void Start()
        {
            _characterLimit = input.characterLimit;
        }

        private void OnDisable()
        {
            input.onValueChanged.RemoveListener(Check);
        }

        private void Check(string text)
        {
            input.text = GetSplitName(text, (int)splitType);
        }

        private string GetSplitName(string text, int checkType)
        {
            string temp = text.Substring(0,
                (text.Length < _characterLimit + 1) ? text.Length : _characterLimit + 1);
            if (checkType == (int)SplitType.ASCII)
            {
                return SplitNameByASCII(temp);
            }
            else if (checkType == (int)SplitType.GB)
            {
                return SplitNameByGB(temp);
            }
            else if (checkType == (int)SplitType.Unicode)
            {
                return SplitNameByUnicode(temp);
            }
            else if (checkType == (int)SplitType.UTF8)
            {
                return SplitNameByUTF8(temp);
            }

            return "";
        }

        /// <summary>
        /// UTF8编码格式,目前是最常用的，汉字3byte，英文1byte
        /// </summary>
        /// <param name="temp"></param>
        /// <returns></returns>
        private string SplitNameByUTF8(string temp)
        {
            string outputStr = "";
            int count = 0;

            for (int i = 0; i < temp.Length; i++)
            {
                string tempStr = temp.Substring(i, 1);
                byte[] encodedBytes = System.Text.ASCIIEncoding.UTF8.GetBytes(tempStr); // Unicode用两个字节对字符进行编码
                string output = "[" + temp + "]";
                for (int byteIndex = 0; byteIndex < encodedBytes.Length; byteIndex++)
                {
                    output += Convert.ToString((int)encodedBytes[byteIndex], 2) + "  "; // 二进制
                }

                int byteCount = System.Text.ASCIIEncoding.UTF8.GetByteCount(tempStr);

                if (byteCount > 1)
                {
                    count += 2;
                }
                else
                {
                    count += 1;
                }

                if (count <= _characterLimit)
                {
                    outputStr += tempStr;
                }
                else
                {
                    break;
                }
            }

            return outputStr;
        }

        /// <summary>
        /// Unicode编码格式
        /// </summary>
        /// <param name="temp"></param>
        /// <returns></returns>
        private string SplitNameByUnicode(string temp)
        {
            string outputStr = "";
            int count = 0;

            for (int i = 0; i < temp.Length; i++)
            {
                string tempStr = temp.Substring(i, 1);
                byte[] encodedBytes = System.Text.ASCIIEncoding.Unicode.GetBytes(tempStr); // Unicode用两个字节对字符进行编码
                if (encodedBytes.Length == 2)
                {
                    int byteValue = (int)encodedBytes[1];
                    if (byteValue == 0) //这里是单个字节
                    {
                        count += 1;
                    }
                    else
                    {
                        count += 2;
                    }
                }

                if (count <= _characterLimit)
                {
                    outputStr += tempStr;
                }
                else
                {
                    break;
                }
            }

            return outputStr;
        }

        /// <summary>
        /// GB编码格式
        /// </summary>
        /// <param name="temp"></param>
        /// <returns></returns>
        private string SplitNameByGB(string temp)
        {
            string outputStr = "";
            int count = 0;

            for (int i = 0; i < temp.Length; i++)
            {
                string tempStr = temp.Substring(i, 1);
                byte[] encodedBytes = System.Text.ASCIIEncoding.Default.GetBytes(tempStr);
                if (encodedBytes.Length == 1)
                {
                    //单字节
                    count += 1;
                }
                else
                {
                    //双字节
                    count += 2;
                }

                if (count <= _characterLimit)
                {
                    outputStr += tempStr;
                }
                else
                {
                    break;
                }
            }

            return outputStr;
        }

        /// <summary>
        /// ASCII编码格式
        /// </summary>
        /// <param name="temp"></param>
        /// <returns></returns>
        private string SplitNameByASCII(string temp)
        {
            byte[] encodedBytes = System.Text.ASCIIEncoding.ASCII.GetBytes(temp);

            string outputStr = "";
            int count = 0;

            for (int i = 0; i < temp.Length; i++)
            {
                if ((int)encodedBytes[i] == 63) //双字节
                    count += 2;
                else
                    count += 1;

                if (count <= _characterLimit)
                    outputStr += temp.Substring(i, 1);
                else if (count > _characterLimit)
                    break;
            }

            if (count <= _characterLimit)
            {
                outputStr = temp;
            }

            return outputStr;
        }
    }
}