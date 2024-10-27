﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace MMDK.Util
{
    /// <summary>
    /// 全局功能
    /// </summary>
    class StaticUtil
    {
        private static readonly HashSet<char> symbols = new HashSet<char>
        {
            '，', '。', '、', '；', '：', '【', '】', '？', '“', '”', '‘', '’', '《', '》',
            '！', '￥', '…', '—', '{', '}', '[', ']', '(', ')', '+', '=', '-', '*', '/',
            '!', '@', '#', '$', '%', '^', '&', '_', '|', ',', '.', '?', ':', ';', '\\',
            '\'', '\"', '\t', '\r', '\n'
        };

        // 检查字符是否为符号
        public static bool IsSymbol(char ch)
        {
            return symbols.Contains(ch);
        }


        /// <summary>
        /// 去除字符串中的中英文标点和特殊字符
        /// </summary>
        /// <param name="ori">原始字符串</param>
        /// <returns>去除符号后的字符串</returns>
        public static string RemoveSymbol(string ori)
        {
            StringBuilder sb = new StringBuilder();

            foreach (var c in ori)
            {
                // 仅当字符不是符号时才添加到结果中
                if (!IsSymbol(c))
                {
                    sb.Append(c);
                }
            }

            return sb.ToString();
        }


        #region DateTime时间与unix时间戳互转
        /// <summary>
        /// 将 Unix 时间戳转换为 DateTime
        /// </summary>
        /// <param name="timestamp">Unix 时间戳</param>
        /// <param name="isMilliseconds">是否为毫秒级时间戳，默认是 false（秒级）</param>
        /// <returns>转换后的 DateTime 对象（本地时间）</returns>
        public static DateTime ConvertTimestampToDateTime(long timestamp, bool isMilliseconds = false)
        {
            DateTime dateTime;

            if (isMilliseconds)
            {
                dateTime = DateTimeOffset.FromUnixTimeMilliseconds(timestamp).DateTime;
            }
            else
            {
                dateTime = DateTimeOffset.FromUnixTimeSeconds(timestamp).DateTime;
            }

            // 转换为本地时间
            return dateTime.ToLocalTime();
        }

        /// <summary>
        /// 将 DateTime 转换为 Unix 时间戳
        /// </summary>
        /// <param name="dateTime">需要转换的 DateTime 对象</param>
        /// <param name="toMilliseconds">是否转换为毫秒级时间戳，默认是 false（秒级）</param>
        /// <returns>对应的 Unix 时间戳</returns>
        public static long ConvertDateTimeToTimestamp(DateTime dateTime, bool toMilliseconds = false)
        {
            DateTimeOffset dateTimeOffset = new DateTimeOffset(dateTime);

            if (toMilliseconds)
            {
                return dateTimeOffset.ToUnixTimeMilliseconds();
            }
            else
            {
                return dateTimeOffset.ToUnixTimeSeconds();
            }
        }

        #endregion




        /// <summary>
        /// 获取本程序集的编译日期
        /// </summary>
        /// <param name="assembly">目标程序集</param>
        /// <returns>编译日期</returns>
        public static DateTime GetBuildDate()
        {
            Assembly assembly = Assembly.GetExecutingAssembly();
            // 获取程序集文件的路径
            var filePath = assembly.Location;
            var fileInfo = new System.IO.FileInfo(filePath);

            // 获取编译日期，文件的最后写入时间
            return fileInfo.LastWriteTime;
        }



        
    }
}
