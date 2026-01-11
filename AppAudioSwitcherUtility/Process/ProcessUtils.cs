using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

namespace AppAudioSwitcherUtility.Process
{
    static class ProcessUtilities
    {
        [DllImport("user32.dll")]
        public static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        public static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

        [DllImport("user32.dll")]
        public static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);
        
        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool QueryFullProcessImageName(
            IntPtr hProcess,
            int flags,
            StringBuilder exeName,
            ref int size);

        public static string GetExecutablePath(int pid)
        {
            try
            {
                using (System.Diagnostics.Process process = System.Diagnostics.Process.GetProcessById(pid))
                {
                    IntPtr hProcess = process.Handle;

                    int capacity = 1024;
                    StringBuilder builder = new StringBuilder(capacity);

                    if (QueryFullProcessImageName(hProcess, 0, builder, ref capacity))
                        return builder.ToString();
                }
            }
            catch
            {
                // ignore and fall through
            }

            return null;
        }

        public static uint GetForegroundWindowProcessId()
        {
            IntPtr foregroundWindow = GetForegroundWindow();
            if (foregroundWindow != IntPtr.Zero)
            {
                uint processId = 0;
                GetWindowThreadProcessId(foregroundWindow, out processId);
                return processId;
            }

            return 0;
        }

        public static string GetForegroundWindowText()
        {
            IntPtr foregroundWindow = GetForegroundWindow();
            if (foregroundWindow != IntPtr.Zero)
            {
                StringBuilder windowText = new StringBuilder(1024);
                GetWindowText(foregroundWindow, windowText, 1024);
                return windowText.ToString();
            }

            return "";
        }

        public static string GetProcessName(int pid)
        {
            try
            {
                System.Diagnostics.Process process = System.Diagnostics.Process.GetProcessById(pid);
                return process.ProcessName;
            }
            catch
            {
                // Fallthrough
            }

            return null;
        }

        public static string GetFriendlyName(int pid)
        {
            try
            {
                string exePath = GetExecutablePath(pid);
                FileVersionInfo info = FileVersionInfo.GetVersionInfo(exePath);

                if (!string.IsNullOrWhiteSpace(info.FileDescription))
                {
                    return info.FileDescription;
                }

                if (!string.IsNullOrWhiteSpace(info.ProductName))
                {
                    return info.ProductName;
                }
                
                // Last fallback: just the file name 
                return System.IO.Path.GetFileNameWithoutExtension(exePath);
            }
            catch
            {
                // Fallthrough
            }
            
            return null;
        }
    }
}