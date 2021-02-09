﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;

namespace SimpleClassicThemeTaskbar
{
    static class ApplicationEntryPoint
    {
        public static int ErrorCount = 0;
        public static UserControlEx ErrorSource;

        delegate bool EnumThreadDelegate(IntPtr hWnd, IntPtr lParam);
        [DllImport("user32.dll")]
        static extern bool EnumThreadWindows(int dwThreadId, EnumThreadDelegate lpfn, IntPtr lParam);
        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        static extern int GetClassName(IntPtr hWnd, StringBuilder lpClassName, int nMaxCount);
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        public static extern IntPtr SendMessage(IntPtr hWnd, int Msg, IntPtr wParam, IntPtr lParam);
        public const int WM_SCT = 0x0420;
        public const int SCTWP_EXIT = 0x0001;
        public const int SCTWP_ISSCT = 0x0003;
        public const int SCTLP_FORCE = 0x0001;

        public static bool SCTCompatMode = false;
        internal static CodeBridge d = new CodeBridge();
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main(string[] args)
        {
            if (args.Contains("--dutch"))
			{
                System.Globalization.CultureInfo ci = new System.Globalization.CultureInfo("nl-NL");
                System.Threading.Thread.CurrentThread.CurrentCulture = ci;
                System.Threading.Thread.CurrentThread.CurrentUICulture = ci;
            }
            if (args.Contains("--exit"))
            { 
                static List<IntPtr> EnumerateProcessWindowHandles(int processId, string name)
                {
                    List<IntPtr> handles = new List<IntPtr>();

                    foreach (ProcessThread thread in Process.GetProcessById(processId).Threads)
                    {
                        EnumThreadWindows(thread.Id, (hWnd, lParam) =>
                        {
                            handles.Add(hWnd);
                            return true;
                        }, IntPtr.Zero);
                    }
                    return handles;
                }

                Process[] scttInstances = Process.GetProcessesByName("SimpleClassicThemeTaskbar");
                Array.ForEach(scttInstances, a =>
                {
                    List<IntPtr> handles = EnumerateProcessWindowHandles(a.Id, "SCTT_Shell_TrayWnd");
                    string s = "";
                    foreach (IntPtr handle in handles)
                    {
                        StringBuilder builder = new StringBuilder(1000);
                        GetClassName(handle, builder, 1000);
                        if (builder.Length > 0)
                            s = s + builder.ToString() + "\n";
                        IntPtr returnValue = SendMessage(handle, WM_SCT, new IntPtr(SCTWP_ISSCT), IntPtr.Zero);
                        if (returnValue != IntPtr.Zero)
                        {
                            SendMessage(handle, WM_SCT, new IntPtr(SCTWP_EXIT), IntPtr.Zero);
                        }
                    }
                });
            }
            if (args.Contains("--gui-test")) 
            {
                Application.SetCompatibleTextRenderingDefault(false);
                Application.Run(new Forms.GraphicsTest());
                return;
            }
            if (args.Contains("--network-ui"))
            {
                Application.VisualStyleState = System.Windows.Forms.VisualStyles.VisualStyleState.NoneEnabled;
                Application.SetCompatibleTextRenderingDefault(false);
                Application.Run(new Forms.NetworkUI());
                return;
            }
            if (args.Contains("--unmanaged"))
            {
                Environment.Exit(d.UnmanagedSCTT());
            }
            if (args.Contains("--sct"))
            {
                SCTCompatMode = true;
            }

#if DEBUG
#else
            else 
            {
                MessageBox.Show("SCT Taskbar does not currently work without SCT. Please install SCTT via the Options menu in SCT 1.2.0 or higher", "SCT required");
                //MessageBox.Show("This version of SCTT is part of the SCT Private Alpha.\nPlease use the latest public build instead.", "SCT Private alpha");
                return;
            }
#endif

            List<Taskbar> t = new List<Taskbar>();
            //Setup crash reports
#if DEBUG
#else
            Application.ThreadException += (sender, arg) => 
            {
                foreach (Taskbar bar in t)
                {
                    bar.selfClose = true;
                    bar.Close();
                    bar.Dispose();
                }
                MessageBox.Show("Unhandled exception ocurred", "Exiting");
                if (SCTCompatMode)
                {
                    File.WriteAllLines($"C:\\SCT\\Taskbar\\outside-taskbar-crash{DateTime.Now:yyyy-dd-M--HH-mm-ss}", new string[] {
                                arg.Exception.GetType().Name,
                                arg.Exception.StackTrace,
                                arg.Exception.Message,
                                arg.Exception.Source
                            }); 
                }
                Application.Exit();
            };
#endif
            //Check if system/program architecture matches
            if (Environment.Is64BitOperatingSystem != Environment.Is64BitProcess)
            {
                string sysArch = Environment.Is64BitOperatingSystem ? "x64" : "x86";
                string processArch = Environment.Is64BitProcess ? "x64" : "x86";
                MessageBox.Show($"You are trying to run a {processArch} version of SCT Taskbar on a {sysArch} version of Windows. This is not supported. Please download a {sysArch} version of SCT Taskbar", "Incorrect architecture");
#if DEBUG
#else
                return;
#endif
            }   
            Application.SetCompatibleTextRenderingDefault(false);
            Microsoft.Win32.SystemEvents.DisplaySettingsChanged += delegate
            {
                NewTaskbars();
            };
            NewTaskbars();
            Application.Run();
            Config.SaveToRegistry();
        }

        internal static void NewTaskbars()
        {
            List<Taskbar> activeBars = new List<Taskbar>();
            foreach (Form form in Application.OpenForms)
                if (form is Taskbar bar)
                    activeBars.Add(bar);
            foreach (Taskbar bar in activeBars)
            {
                foreach (Control d in bar.Controls)
                    d.Dispose();
                bar.selfClose = true;
                bar.Close();
                bar.Dispose();
            }
            foreach (Screen screen in Screen.AllScreens)
            {
                Rectangle rect = screen.Bounds;
                Taskbar taskbar = new Taskbar(screen.Primary);
                taskbar.ShowOnScreen(screen);
                if (!Config.ShowTaskbarOnAllDesktops && !screen.Primary)
                    taskbar.NeverShow = true;
            }
        }
    }
}
