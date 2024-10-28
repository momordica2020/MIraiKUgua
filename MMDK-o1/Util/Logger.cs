﻿using Microsoft.Windows.Themes;
using System;
using System.ComponentModel;
using System.Data;
using System.IO;


namespace MMDK.Util
{
    public enum LogType
    {
        System,
        Virtual,
        Mirai,
        Debug
    }

    public class Logger
    {

        
        public class LogInfo
        {
 
            public string Message { get; set; }

            public LogType Type { get; set; }

            public DateTime HappendTime { get; set; }


        }
        

        private static readonly Lazy<Logger> instance = new Lazy<Logger>(() => new Logger());
        private readonly StreamWriter writer;
        private static readonly object lockObject = new object(); // 用于线程安全


        public readonly string logFilePath;

        public delegate void BroadcastLog(string message);

        public event BroadcastLog OnBroadcastLogEvent;

        //public Logger(string logFilePath)
        //{
        //    writer = new StreamWriter(logFilePath, true); // 以追加模式打开
        //}

        // 私有构造函数
        private Logger()
        {
            string logDict = $"{Directory.GetCurrentDirectory()}/Log";
            if (!Directory.Exists(logDict)) Directory.CreateDirectory(logDict);
            logFilePath = $"{logDict}/{DateTime.Today.ToString("yyyyMMdd")}.log";
            writer = new StreamWriter(logFilePath, true) { AutoFlush = true }; // 以追加模式打开文件
        }


        // 公共静态属性获取实例
        public static Logger Instance => instance.Value;

        // 记录日志
        public void Log(string message, LogType logType = LogType.System)
        {
            lock (lockObject) // 确保线程安全
            {
                try
                {
                    LogInfo info = new LogInfo
                    {
                        Message = message,
                        Type = logType,
                        HappendTime = DateTime.Now,
                    };
                    string msg = $"[{info.HappendTime}][{GetLogTypeName(logType)}]{message}";
                    OnBroadcastLogEvent?.Invoke(msg);

                    writer.WriteLine(msg);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message + "\r\n" + ex.StackTrace);
                }
            }
        }
        public void Log(Exception ex, LogType logType = LogType.System)
        {
            Log($"{ex.Message}\r\n{ex.StackTrace}", logType);
        }

        // 关闭日志文件
        public void Close()
        {
            lock (lockObject)
            {
                writer?.Close();
            }
        }


        public static string GetLogTypeName(LogType logType)
        {
            switch (logType)
            {
                case LogType.System: return "系统";
                case LogType.Mirai: return "Mirai组件";
                case LogType.Virtual: return "本地";
                case LogType.Debug: return "调试";
                default: return "未知";
            }
        }




    }
}
