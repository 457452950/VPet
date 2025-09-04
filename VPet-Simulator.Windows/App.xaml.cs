﻿using LinePutScript.Localization.WPF;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Interop;
using VPet_Simulator.Windows.Interface;

namespace VPet_Simulator.Windows
{
    /// <summary>
    /// App.xaml 的交互逻辑
    /// </summary>
    public partial class App : Application
    {
        public App() : base()
        {
#if !DEBUG
            base.DispatcherUnhandledException += (s, e) => { e.Handled = true; UnhandledException(e.Exception, false); };
            AppDomain.CurrentDomain.UnhandledException += (s, e) => { UnhandledException((e.ExceptionObject as Exception), true); };
#endif
            //AppDomain.CurrentDomain.AssemblyResolve += CurrentDomain_AssemblyResolve;
        }
        public static string[] Args { get; set; }
        /// <summary>
        /// 多存档系统名称
        /// </summary>
        public static List<string> MutiSaves { get; set; } = new List<string>();

        public static List<MainWindow> MainWindows { get; set; } = new List<MainWindow>();

        public static HashSet<string> MODType { get; set; } = new HashSet<string>();

        protected override void OnStartup(StartupEventArgs e)
        {
            Args = e.Args;

            //旧版本多开bug修复
            if (File.Exists(ExtensionValue.BaseDirectory + @"\Setting-.lps"))
                File.Delete(ExtensionValue.BaseDirectory + @"\Setting-.lps");

            foreach (var mss in new DirectoryInfo(ExtensionValue.BaseDirectory).GetFiles("Setting*.lps"))
            {
                var n = mss.Name.Substring(7).Trim('-');
                MutiSaves.Add(n.Substring(0, n.Length - 4));
            }

            if (MutiSaves.Count == 0)
            {
                MutiSaves.Add("");
            }
            if (!Args.Any(x => x.Contains("prefix")))
            {
                var file = new DirectoryInfo(ExtensionValue.BaseDirectory).GetFiles("startup_*").FirstOrDefault();
                if (file != null)
                {
                    var su = file.Name.Substring(8);
                    var al = Args.ToList();
                    al.Add($"prefix#{su}:|");
                    Args = al.ToArray();
                }
            }

        }
        HashSet<string> ErrorReport = new HashSet<string>();
        private void UnhandledException(Exception e, bool isFatality)
        {
            var expt = e.ToString();
            if (ErrorReport.Contains(expt))
                return;//防止重复报错
            ErrorReport.Add(expt);
            if (expt.Contains("MainWindow.Close") || expt.Contains("System.Windows.Window.DragMove") ||
                expt.Contains("winConsole"))
                return;
            else if ((!isFatality && MainWindow != null && ((MainWindow)MainWindow).GameSavesData?.GameSave != null &&
                (((MainWindow)MainWindow).GameSavesData.GameSave.Money > int.MaxValue || ((MainWindow)MainWindow).GameSavesData.GameSave.Exp > int.MaxValue)
                ) && ((expt.ToLowerInvariant().Contains("value") && expt.ToLowerInvariant().Contains("nan")) ||
                expt.Contains("System.OverflowException") || expt.Contains("System.DivideByZeroException")))
            {
                MessageBox.Show("由于修改游戏数据导致数据溢出,存档可能会出错\n开发者提醒您请不要使用过于超模的MOD".Translate());
                return;
            }
            else if (expt.Contains("System.IO.FileNotFoundException") && expt.Contains("cache"))
            {
                MessageBox.Show("缓存被其他软件删除,游戏无法继续运行\n请重启游戏重新生成缓存".Translate());
                return;
            }
            else if (expt.Contains("0x80070008"))
            {
                MessageBox.Show("游戏内存不足,请修改设置中渲染分辨率以便降低内存使用".Translate());
                return;
            }
            else if (expt.Contains("UnauthorizedAccessException"))
            {
                MessageBox.Show("游戏权限不足,无法写入游戏存档和设置,请检查设置文件是否被其他软件占用".Translate());
                return;
            }
            else if (expt.Contains("VPet.Plugin"))
            {
                var exptin = expt.Split('\n').First(x => x.Contains("VPet.Plugin"));
                exptin = exptin.Substring(exptin.IndexOf("VPet.Plugin") + 12).Split('.')[0];
                MessageBox.Show("游戏发生错误,可能是".Translate() + $"MOD({exptin.Translate()})" +
                    "导致的\n如有可能请发送 错误信息截图和引发错误之前的操作给相应MOD作者\n感谢您对MOD开发的支持\n".Translate()
                     + expt, "游戏发生错误,可能是".Translate() + exptin);
                return;
            }

            foreach (var modname in MODType)
            {
                if (expt.Contains(modname))
                {
                    var exptin = modname.Split('.').Last();
                    MessageBox.Show("游戏发生错误,可能是".Translate() + $"MOD({modname})" +
                        "导致的\n如有可能请发送 错误信息截图和引发错误之前的操作给相应MOD作者\n感谢您对MOD开发的支持\n".Translate()
                         + expt, "游戏发生错误,可能是".Translate() + exptin);
                    return;
                }
            }


            string errstr = "游戏发生错误,可能是".Translate() + (string.IsNullOrWhiteSpace(CoreMOD.NowLoading) ?
                "游戏或者MOD".Translate() : $"MOD({CoreMOD.NowLoading})") +
                "导致的\n如有可能请发送 错误信息截图和引发错误之前的操作 给开发者:service@exlb.net\n感谢您对游戏开发的支持\n".Translate()
                + expt;
            if (isFatality || MainWindow == null)
            {
                MessageBox.Show(errstr, "游戏致命性错误".Translate());
                return;
            }
            else
            {
                new winReport(((MainWindow)MainWindow), errstr).Show();
                return;
            }
        }
    }
}
