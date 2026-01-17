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
        private System.Timers.Timer _timer;

        // Delegate
        private WinEventDelegate _callback;
        private volatile uint _currentForegroundProcessId = 0;

        public uint CurrentForegroundProcessId
        {
            get => _currentForegroundProcessId;
            private set
            {
                _currentForegroundProcessId = value;
                Console.WriteLine($"Forground process changed to: {_currentForegroundProcessId}");
                ForegroundProcessChanged?.Invoke(_currentForegroundProcessId);
            }
        }

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
            _timer = new System.Timers.Timer();
            _timer.Interval = 1000 * 10;
            _timer.AutoReset = true;
            _timer.Elapsed += OnFocusedTimerElapsed;
            _timer.Start();
            
            _callback = WinHookEventCallback;
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

        private void WinHookEventCallback(IntPtr hWinEventHook, uint eventType, IntPtr hwnd, int idObject, int idChild,
            uint dwEventThread, uint dwmsEventTime)
        {
            ProcessUtilities.GetWindowThreadProcessId(hwnd, out uint processId);
            CurrentForegroundProcessId = processId;
        }

        private void OnFocusedTimerElapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            uint curProcessId = ProcessUtilities.GetForegroundWindowProcessId();
            if (curProcessId == CurrentForegroundProcessId) return;
            CurrentForegroundProcessId = curProcessId;
        }

        public void Dispose()
        {
            _running = false;
            _timer.Stop();
            _timer.Dispose();

            if (_hook != IntPtr.Zero)
            {
                UnhookWinEvent(_hook);
                _hook = IntPtr.Zero;
            }
        }
    }
}