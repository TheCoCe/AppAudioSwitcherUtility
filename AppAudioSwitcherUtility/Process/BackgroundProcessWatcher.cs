using System;
using System.Runtime.InteropServices;
using System.Threading;

namespace AppAudioSwitcherUtility.Process
{
    public class BackgroundProcessWatcher : IDisposable
    {
        private Thread _watcherThread;
        private IntPtr _hook;
        private bool _running;

        // Delegate
        private WinEventDelegate _callback;

        // WinEvent constants
        private const uint EVENT_SYSTEM_FOREGROUND = 3;
        private const uint WINEVENT_OUTOFCONTEXT = 0;

        delegate void WinEventDelegate(IntPtr hWinEventHook, uint eventType, IntPtr hwnd, int idObject, int idChild,
            uint dwEventThread, uint dwmsEventTime);

        // P/Invoke
        [DllImport("user32.dll")]
        static extern IntPtr SetWinEventHook(uint eventMin, uint eventMax, IntPtr hmodWinEventProc,
            WinEventDelegate lpfnWinEventProc, uint idProcess, uint idThread, uint dwFlags);

        [DllImport("user32.dll")]
        static extern bool UnhookWinEvent(IntPtr hWinEventHook);

        [DllImport("user32.dll")]
        private static extern int GetMessage(out MSG lpMsg, IntPtr hWnd, uint wMsgFilterMin, uint wMsgFilterMax);

        [StructLayout(LayoutKind.Sequential)]
        private struct MSG
        {
            public IntPtr hwnd;
            public uint message;
            public UIntPtr wParam;
            public IntPtr lParam;
            public uint time;
            public int pt_x;
            public int pt_y;
        }

        public event Action<uint> ForegroundProcessChanged;
        
        public void Start()
        {
            if (_running) return;

            _running = true;
            _watcherThread = new Thread(Listen)
            {
                IsBackground = true
            };
            _watcherThread.Start();
        }

        private void Listen()
        {
            _callback = Callback;
            _hook = SetWinEventHook(EVENT_SYSTEM_FOREGROUND, EVENT_SYSTEM_FOREGROUND, IntPtr.Zero, _callback, 0, 0, WINEVENT_OUTOFCONTEXT);

            if (_hook == IntPtr.Zero)
            {
                throw new Exception("Failed to set WinEvent Hook");
            }

            while (_running)
            {
                GetMessage(out _, IntPtr.Zero, 0, 0);
            }
        }

        private void Callback(IntPtr hWinEventHook, uint eventType, IntPtr hwnd, int idObject, int idChild,
            uint dwEventThread, uint dwmsEventTime)
        {
            ProcessUtilities.GetWindowThreadProcessId(hwnd, out uint processId);
            ForegroundProcessChanged?.Invoke(processId);
        }

        public void Dispose()
        {
            _running = false;

            if (_hook != IntPtr.Zero)
            {
                UnhookWinEvent(_hook);
                _hook = IntPtr.Zero;
            }
        }
    }
}