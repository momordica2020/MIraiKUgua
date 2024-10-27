﻿using MeowMiraiLib;
using MeowMiraiLib.Event;
using MeowMiraiLib.Msg;
using MeowMiraiLib.Msg.Sender;
using MeowMiraiLib.Msg.Type;

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Windows.Forms;


using MMDK.Mods;
using MMDK.Util;
using static MeowMiraiLib.Msg.Type.ForwardMessage;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.Rebar;
using Microsoft.Win32;
using System.ComponentModel;
using static System.Windows.Forms.AxHost;
using System.Threading.Tasks;
using System.Windows.Interop;

namespace MMDK
{

    partial class FormMonitor : Form
    {






        public static MeowMiraiLib.Client ClientX;


        public List<Mod> Mods = new List<Mod>();

        public HistoryManager history = new HistoryManager();









        #region 窗体相关定义


        SystemInfo systemInfo;
        DateTime beginTime;
        bool IsEnterAutoSend = true;
        bool IsVirtualGroup = false;

        public delegate void sendString(string msg);

        public enum BotRunningState
        {
            stop,
            mmdkInit,
            ok,
            exit
        }
        public BotRunningState _state;
        BotRunningState State
        {
            get => _state;
            set
            {
                _state = value;

                var stateMessages = new Dictionary<BotRunningState, string>
                    {
                        { BotRunningState.stop, "已停止" },
                        { BotRunningState.mmdkInit, "正在启动Bot" },
                        { BotRunningState.ok, "正在运行" }
                    };

                string text = stateMessages.ContainsKey(value) ? stateMessages[value] : string.Empty;
                //更新显示窗口
                try
                {
                    Invoke((MethodInvoker)delegate
                    {
                        lbState.Text = text;
                    });
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error updating state label: {ex.Message}\r\n{ex.StackTrace}");
                }
            }
        }



        #endregion

        public FormMonitor()
        {
            InitializeComponent();
        }

        /// <summary>
        /// 检查并初始化配置
        /// </summary>
        /// <returns></returns>
        bool checkAndSetConfigValid()
        {
            try
            {

                if (string.IsNullOrWhiteSpace(Config.Instance.App.Version)) Config.Instance.App.Version = "v0.0.1";

                // qq info
                if (string.IsNullOrWhiteSpace(Config.Instance.App.Avatar.myQQ.ToString())) Config.Instance.App.Avatar.myQQ = 00000;


                beginTime = DateTime.Now;
                Config.Instance.App.Log.StartTime = beginTime;

                Config.Instance.App.Log.beginTimes += 1;

            }
            catch (Exception ex)
            {
                Logger.Instance.Log(ex);
                return false;
            }

            return true;
        }



        public void logWindow(string str, LogType logType = LogType.System)
        {
            int maxlen = 100000;
            try
            {
                Invoke(new EventHandler(delegate
                {
                    tbMmdk.AppendText($"[{DateTime.Now:G}][{Logger.GetLogTypeName(logType)}]{str}\r\n");
                    if (tbMmdk.TextLength > maxlen)
                    {
                        tbMmdk.Text = tbMmdk.Text.Substring(tbMmdk.TextLength - maxlen);
                    }
                    tbMmdk.ScrollToCaret();
                }));

                Logger.Instance.Log(str, logType);
            }
            catch (Exception ex)
            {
                Logger.Instance.Log(ex);
            }

        }



        public void workRunBot()
        {
            try
            {
                logWindow($"读取配置文件...");
                Config.Instance.Load();


                logWindow($"开始启动bot...");
                Mods = new List<Mod>
                {
                    new ModAdmin(),
                    new ModBank(),
                    new ModDice(),
                    new ModProof(),
                    new ModTextFunction(),
                    new ModZhanbu(),
                    new ModRandomChat(),    // 这个会用闲聊收尾

                };



                //bot = new MainProcess();
                //bot.Init(config);



                if (true)
                {
                    // 打开历史记录，不会是真的吧
                    string HistoryPath = Config.Instance.ResourceFullPath("HistoryPath");
                    if (!Directory.Exists(HistoryPath)) Directory.CreateDirectory(HistoryPath);
                    logWindow($"历史记录保存在 {HistoryPath} 里");
                    history.Init(HistoryPath);
                }
                else
                {
                    logWindow($"历史记录不会有记录");
                }

                foreach (var mod in Mods)
                {
                    try
                    {
                        if (mod.Init(null))
                        {
                            logWindow($"模块{mod.GetType().Name}已初始化");
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.Instance.Log(ex);
                    }
                }

                State = BotRunningState.ok;
                logWindow($"bot启动完成");


                //mirai = new MiraiLink();
                if (Config.Instance.App.IO.MiraiRun)
                {

                    //string verifyKey = "123456";
                    string connectUri = $"{Config.Instance.App.IO.MiraiWS}:{Config.Instance.App.IO.MiraiPort}/all?qq={Config.Instance.App.Avatar.myQQ}";
                    logWindow($"正在连接Mirai...{connectUri}");
                    ClientX = new(connectUri);
                    ClientX._OnServeiceConnected += ServiceConnected;
                    ClientX._OnServeiceError += OnServeiceError;
                    ClientX._OnServiceDropped += OnServiceDropped;


                    ClientX.Connect();


                    ClientX.OnFriendMessageReceive += OnFriendMessageReceive;
                    ClientX.OnGroupMessageReceive += OnGroupMessageReceive;
                    ClientX.OnEventBotInvitedJoinGroupRequestEvent += OnEventBotInvitedJoinGroupRequestEvent;
                    ClientX.OnEventNewFriendRequestEvent += OnEventNewFriendRequestEvent;
                    ClientX.OnEventFriendNickChangedEvent += OnEventFriendNickChangedEvent;
                }
                else
                {
                    logWindow($"不启动Mirai，启动本地应答");

                }

            }
            catch (Exception ex)
            {
                logWindow(ex.Message + "\r\n" + ex.StackTrace);
            }
        }




        public void OnFriendMessageReceive(FriendMessageSender s, MeowMiraiLib.Msg.Type.Message[] e)
        {
            if (s.id == Config.Instance.App.Avatar.myQQ) return;
            logWindow($"好友信息 [qq:{s.id},昵称:{s.nickname},备注:{s.remark}] \n内容:{e.MGetPlainString()}");
            history.saveMsg(0, s.id, e.MGetPlainString());
            string cmd = e.MGetPlainString();
            if (string.IsNullOrWhiteSpace(cmd)) return;
            cmd = cmd.Trim();
            bool talked = false;

            if (cmd.Length > 0)
            {
                List<string> res = new List<string>();
                foreach (var mod in Mods)
                {
                    var succeed = mod.HandleText(s.id, 0, cmd, res);
                    if (succeed)
                    {
                        break;
                    }
                }


                foreach (var msg in res)
                {
                    if (msg.Trim().Length <= 0) continue;
                    var output = new MeowMiraiLib.Msg.Type.Message[]
                    {

                        new Plain(msg)
                    };
                    new FriendMessage(s.id, output).Send(ClientX);
                    talked = true;
                }

            }
            // update player info
            Player p = Config.Instance.GetPlayerInfo(s.id);
            p.Name = s.nickname;
            p.Mark = s.remark;
            // 计数统计
            if (talked)
            {
                p.UseTimes += 1;
                Config.Instance.App.Log.playTimePrivate += 1;
            }
        }



        public void OnGroupMessageReceive(GroupMessageSender s, MeowMiraiLib.Msg.Type.Message[] e)
        {
            if (s.id == Config.Instance.App.Avatar.myQQ) return;
            logWindow($"群友信息 [qq:{s.id},昵称:{s.memberName}] \n内容:{e.MGetPlainString()}");
            history.saveMsg(s.group.id, s.id, e.MGetPlainString());

            // 检查群聊是否需要bot回复
            bool isAtMe = false;
            bool talked = false;
            string cmd = e.MGetPlainString().Trim();
            if (cmd.Length > 0)
            {

            }
            foreach (var v in e)
            {
                if (v.type == "At" && ((MeowMiraiLib.Msg.Type.At)v).target == Config.Instance.App.Avatar.myQQ)
                {
                    isAtMe = true;
                    break;
                }
            }
            if (Config.Instance.isAskMe(cmd))
            {
                isAtMe = true;
                cmd = cmd.Substring(Config.Instance.App.Avatar.askName.Length);
            }
            List<string> res = new List<string>();


            foreach (var mod in Mods)
            {
                if (!isAtMe)
                {
                    //if(mod is ModRandomChat)
                    //{
                    //    //无需at也传输进去

                    //}
                    continue;
                }
                var succeed = mod.HandleText(s.id, s.group.id, cmd, res);
                if (succeed)
                {
                    break;
                }
            }

            var rres = res.ToArray();
            if (rres != null && rres.Length > 0)
            {
                for (int i = 0; i < rres.Length; i++)
                {
                    string text = rres[i];
                    if (text == null) continue;
                    text = text.Trim(); if (text.Length <= 0) continue;
                    if (i == 0)
                    {
                        // 第一条信息 at 一下，后续就不了
                        var output = new MeowMiraiLib.Msg.Type.Message[]
                        {
                             new At(s.id, s.memberName),
                             new Plain(" " + rres[i])
                        };
                        new GroupMessage(s.group.id, output).Send(ClientX);
                        talked = true;
                    }
                    else
                    {
                        var output = new MeowMiraiLib.Msg.Type.Message[]
                        {
                            new Plain(rres[i])
                        };
                        new GroupMessage(s.group.id, output).Send(ClientX);
                        talked = true;
                    }
                }
            }


            // update player info
            Playgroup p = Config.Instance.GetGroupInfo(s.group.id);
            p.Name = s.group.name;
            if (talked)
            {
                p.UseTimes += 1;
                Config.Instance.GetPlayerInfo(s.id).UseTimes += 1;
                Config.Instance.App.Log.playTimeGroup += 1;
            }
        }

        void ServiceConnected(string e)
        {
            logWindow($"连接成功：{e}");
        }

        void OnServeiceError(Exception e)
        {
            logWindow($"连接出错：{e.Message}\r\n{e.StackTrace}");
        }

        void OnServiceDropped(string e)
        {
            logWindow($"连接中断：{e}");
        }

        void OnClientOnlineEvent(OtherClientOnlineEvent e)
        {
            logWindow($"其他平台登录（标识：{e.id}，平台：{e.platform}");
        }
        void OnEventBotInvitedJoinGroupRequestEvent(BotInvitedJoinGroupRequestEvent e)
        {
            logWindow($"受邀进群（用户：{e.fromId}，群：{e.groupName}({e.groupId})消息：{e.message}");
            var g = Config.Instance.GetGroupInfo(e.groupId);
            var u = Config.Instance.GetPlayerInfo(e.fromId);
            if (g.Is("黑名单") || u.Is("黑名单"))
            {
                e.Deny(ClientX);
            }
            if (u.Is("管理员") || u.Is("好友") || e.fromId == Config.Instance.App.Avatar.adminQQ)
            {
                e.Grant(ClientX);
            }
        }

        void OnEventNewFriendRequestEvent(NewFriendRequestEvent e)
        {
            logWindow($"好友申请：{e.nick}({e.fromId})(来自{e.groupId})消息：{e.message}");
            if (!string.IsNullOrWhiteSpace(e.message) && e.message.StartsWith(Config.Instance.App.Avatar.askName))
            {
                e.Grant(ClientX, "来了来了");
                var user = Config.Instance.GetPlayerInfo(e.fromId);
                user.Name = e.nick;
                user.Mark = e.nick;
                user.SetTag("好友");
                user.Type = PlayerType.Normal;
            }
            else
            {
                e.Deny(ClientX);
            }
        }

        void OnEventFriendNickChangedEvent(FriendNickChangedEvent e)
        {
            logWindow($"好友改昵称（{e.friend.id}，{e.from}->{e.to}");
            var user = Config.Instance.GetPlayerInfo(e.friend.id);
            user.Name = e.to;

        }



        private void tbMmdk_MouseDown(object sender, MouseEventArgs e)
        {

        }

        private void tbMirai_MouseDown(object sender, MouseEventArgs e)
        {

        }

        private void 清空日志ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            tbMmdk.Clear();
        }



        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            try
            {
                foreach (var mod in Mods)
                {
                    try
                    {
                        mod.Exit();
                    }
                    catch (Exception ex)
                    {
                        Logger.Instance.Log(ex);
                    }

                }
                history.run = false;
                Config.Instance.Save();

                Logger.Instance.Close();
                //Environment.Exit(0);

                State = BotRunningState.exit;
            }
            catch
            {

            }
        }

        private async void Form1_Load(object sender, EventArgs e)
        {


            logWindow("开始初始化配置文件。");

            Config.Instance.Load();
            bool isValid = checkAndSetConfigValid();
            if (!isValid)
            {
                logWindow("配置文件读取失败，中止运行");
                return;
            }


            logWindow("配置文件读取完毕。");

            systemInfo = new SystemInfo();

            await Task.Run(() =>
            {
                while (State != BotRunningState.exit)
                {
                    UpdateMonitorInfo(); // 更新状态

                    // 控制更新频率，比如每秒更新一次
                    Thread.Sleep(200);
                }
            });
            //new Thread(workMonitor).Start();
        }

        private void StartBot()
        {
            //button1.Enabled = false;

            new Thread(workRunBot).Start();

            textInputTest.Focus();
        }

        /// <summary>
        /// 模拟bot的输入
        /// </summary>
        /// <param name="message"></param>
        public void virtualInput(string message)
        {
            long userId;
            long groupId;
            bool isAtMe = false;

            if (IsVirtualGroup)
            {

                userId = -1;
                groupId = 1;
                textLocalTestGroup.AppendText($"[me]:{message}\r\n");

                if (Config.Instance.isAskMe(message))
                {
                    isAtMe = true;
                    message = message.Substring(Config.Instance.App.Avatar.askName.Length);
                }
            }
            else
            {
                
                userId = -1;
                groupId = 0;
                textLocalTest.AppendText($"[me]:{message}\r\n");

                isAtMe = true;
            }



            try
            {

                List<string> res = new List<string>();
                foreach (var mod in Mods)
                {
                    if (!isAtMe) break;
                    var succeed = mod.HandleText(userId, groupId, message, res);
                    if (succeed)
                    {
                        break;
                    }
                }
                foreach (var result in res)
                {
                    virtualOutput(result);
                }
            }
            catch (Exception ex)
            {
                textLocalTest.AppendText($"[error]:{ex.Message}\r\n{ex.StackTrace}\r\n");
            }


        }



        public void virtualOutput(string result)
        {
            if (IsVirtualGroup)
            {
                textLocalTestGroup.AppendText($"[bot]:{result}\r\n");
            }
            else
            {
                textLocalTest.AppendText($"[bot]:{result}\r\n");
            }
           
        }

        private void button2_Click(object sender, EventArgs e)
        {
            string msg = textInputTest.Text.Trim();
            textInputTest.Clear();
            virtualInput(msg);

        }

        private void textInputTest_TextChanged(object sender, EventArgs e)
        {
            if (IsEnterAutoSend && textInputTest.Text.EndsWith("\n"))
            {
                string msg = textInputTest.Text.Trim();
                textInputTest.Clear();
                virtualInput(msg);
            }
        }

        private void button3_Click(object sender, EventArgs e)
        {

        }

        private void 启动botToolStripMenuItem_Click(object sender, EventArgs e)
        {
            StartBot();
            启动botToolStripMenuItem.Enabled = false;
            启动botToolStripMenuItem.Text = "（正在运行）";
        }

        private void 存档当前配置ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Config.Instance.Save();
            logWindow($"已储存");
        }


        /// <summary>
        /// 更新显示界面信息
        /// </summary>
        private void UpdateMonitorInfo()
        {
            if (State != BotRunningState.exit)
            {
                var cpu = systemInfo.CpuLoad;
                var mem = 100.0 - ((double)systemInfo.MemoryAvailable * 100 / systemInfo.PhysicalMemory);

                try
                {
                    Invoke(new EventHandler(delegate
                    {
                        lbCPU.Text = $"CPU\n({cpu.ToString(".0")}%)";
                        lbMem.Text = $"内存\n({mem.ToString(".0")}%)";
                        lbBeginTime.Text = $"{beginTime.ToString("yyyy-MM-dd")}\r\n{beginTime.ToString("HH:mm:ss")}";
                        lbTimeSpan.Text = $"{(DateTime.Now - beginTime).Days}天\r\n{(DateTime.Now - beginTime).Hours}小时{(DateTime.Now - beginTime).Minutes}分{(DateTime.Now - beginTime).Seconds}秒";
                        lbQQ.Text = $"{Config.Instance.App.Avatar.myQQ}";
                        lbPort.Text = $"{Config.Instance.App.IO.MiraiPort}";
                        lbVersion.Text = $"{Config.Instance.App.Version}";
                        lbUpdateTime.Text = $"{Util.StaticUtil.GetBuildDate().ToString("yyyy-MM-dd")}";
                        //lbFriendNum.Text = $"{config["friendnum"]}";
                        //lbGroupNum.Text = $"{config["groupnum"]}";
                        lbUseNum.Text = $"{Config.Instance.App.Log.playTimePrivate + Config.Instance.App.Log.playTimeGroup}";
                        //if (bot != null)
                        //{
                        //    lbFriendNum.Text = $"{bot.friends.Count}";
                        //    lbGroupNum.Text = $"{bot.groups.Count}";
                        //}

                        pbCPU.Value = (int)(cpu);
                        pbMem.Value = (int)(mem);
                    }));
                }
                catch (Exception ex)
                {

                }



                Thread.Sleep(500);     // 1s
            }
        }

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            IsEnterAutoSend = checkBox1.Checked;
        }

        private void tabControl1_SelectedIndexChanged(object sender, EventArgs e)
        {
            if(tabControl1.SelectedIndex == 0)
            {
                // private
                IsVirtualGroup = false;
                button2.Text = "发送（私聊）";
            }
            else
            {
                IsVirtualGroup = true;
                button2.Text = "发送（群组）";
            }
        }
    }

}
