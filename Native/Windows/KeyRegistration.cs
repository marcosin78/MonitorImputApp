#if WINDOWS
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using MonitorInputApp.Models;
using MonitorInputApp; // <-- Añade este using

namespace MonitorInputApp.Models
{
    public class KeyRegistration
    {
        private IntPtr _keyboardHookID = IntPtr.Zero;
        private IntPtr _mouseHookID = IntPtr.Zero;
        private LowLevelKeyboardProc _keyboardProc;
        private LowLevelMouseProc _mouseProc;
        private DateTime _lastInputTime;
        private System.Timers.Timer? _timer;

        // Variable para la última interacción
        private InteractionInfo? _lastInteraction;

        public KeyRegistration()
        {
            _keyboardProc = KeyboardHookCallback;
            _mouseProc = MouseHookCallback;
            _lastInputTime = DateTime.MinValue;
        }

        public void Start()
        {
            _keyboardHookID = SetKeyboardHook(_keyboardProc);
            _mouseHookID = SetMouseHook(_mouseProc);

            _timer = new System.Timers.Timer(30 * 1000); // 30 segundos para depuración
            _timer.Elapsed += (s, e) =>
            {
                if (_lastInteraction != null)
                {
                    Console.WriteLine("----- Última interacción registrada para el log de este lapso de 30 segundos -----");
                    string json = System.Text.Json.JsonSerializer.Serialize(_lastInteraction, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
                    Console.WriteLine(json);

                    // Guardar en log usando LogManager
                    LogManager.SaveInteraction(_lastInteraction);
                }
                else
                {
                    Console.WriteLine("No hubo interacción en este lapso de 30 segundos.");
                }
            };
            _timer.Start();
        }

        public void Stop()
        {
            UnhookWindowsHookEx(_keyboardHookID);
            UnhookWindowsHookEx(_mouseHookID);
            _timer?.Stop();
            _timer?.Dispose();
            _timer = null;
        }

        private IntPtr SetKeyboardHook(LowLevelKeyboardProc proc)
        {
            using (Process curProcess = Process.GetCurrentProcess())
            using (ProcessModule curModule = curProcess.MainModule!)
            {
                return SetWindowsHookEx(WH_KEYBOARD_LL, proc,
                    GetModuleHandle(curModule.ModuleName), 0);
            }
        }

        private IntPtr SetMouseHook(LowLevelMouseProc proc)
        {
            using (Process curProcess = Process.GetCurrentProcess())
            using (ProcessModule curModule = curProcess.MainModule!)
            {
                return SetWindowsHookEx(WH_MOUSE_LL, proc,
                    GetModuleHandle(curModule.ModuleName), 0);
            }
        }

        private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);
        private delegate IntPtr LowLevelMouseProc(int nCode, IntPtr wParam, IntPtr lParam);

        private IntPtr KeyboardHookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0)
            {
                int WM_KEYDOWN = 0x0100;
                if (wParam == (IntPtr)WM_KEYDOWN)
                {
                    int vkCode = Marshal.ReadInt32(lParam);
                    _lastInputTime = DateTime.Now;
                    string? monitorName = GetActiveMonitorName();
                    string? appName = GetActiveProcessName();

                    _lastInteraction = new InteractionInfo
                    {
                        Timestamp = _lastInputTime,
                        Monitor = monitorName,
                        App = appName,
                        EventType = "Tecla",
                        KeyCode = vkCode
                    };

                    // Actualiza la propiedad global
                    App.LastInteraction = _lastInteraction;

                    Console.WriteLine($"Tecla presionada: {vkCode} a las {_lastInputTime:HH:mm:ss} en monitor: {monitorName} | App: {appName}");
                }
            }
            return CallNextHookEx(_keyboardHookID, nCode, wParam, lParam);
        }

        private IntPtr MouseHookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0)
            {
                int WM_LBUTTONDOWN = 0x0201;
                int WM_RBUTTONDOWN = 0x0204;
                string? monitorName = GetActiveMonitorName();
                string? appName = GetActiveProcessName();

                if (wParam == (IntPtr)WM_LBUTTONDOWN)
                {
                    _lastInputTime = DateTime.Now;
                    _lastInteraction = new InteractionInfo
                    {
                        Timestamp = _lastInputTime,
                        Monitor = monitorName,
                        App = appName,
                        EventType = "RatonIzq"
                    };
                    App.LastInteraction = _lastInteraction; // Actualiza la propiedad global
                    Console.WriteLine($"Botón izquierdo del ratón presionado a las {_lastInputTime:HH:mm:ss} en monitor: {monitorName} | App: {appName}");
                }
                else if (wParam == (IntPtr)WM_RBUTTONDOWN)
                {
                    _lastInputTime = DateTime.Now;
                    _lastInteraction = new InteractionInfo
                    {
                        Timestamp = _lastInputTime,
                        Monitor = monitorName,
                        App = appName,
                        EventType = "RatonDer"
                    };
                    App.LastInteraction = _lastInteraction; // Actualiza la propiedad global
                    Console.WriteLine($"Botón derecho del ratón presionado a las {_lastInputTime:HH:mm:ss} en monitor: {monitorName} | App: {appName}");
                }
            }
            return CallNextHookEx(_mouseHookID, nCode, wParam, lParam);
        }

        private const int WH_KEYBOARD_LL = 13;
        private const int WH_MOUSE_LL = 14;

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(int idHook, Delegate lpfn, IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr GetModuleHandle(string lpModuleName);

        // Devuelve el nombre del monitor donde está la ventana activa
        public string? GetActiveMonitorName()
        {
            IntPtr hwnd = GetForegroundWindow();
            if (hwnd == IntPtr.Zero)
                return null;

            IntPtr monitor = MonitorFromWindow(hwnd, MONITOR_DEFAULTTONEAREST);
            MONITORINFOEX info = new MONITORINFOEX();
            info.cbSize = (uint)System.Runtime.InteropServices.Marshal.SizeOf(typeof(MONITORINFOEX));
            if (GetMonitorInfo(monitor, ref info))
            {
                return info.szDevice;
            }
            return null;
        }

        // Devuelve el nombre del proceso de la ventana activa (app en foco)
        public string? GetActiveProcessName()
        {
            IntPtr hwnd = GetForegroundWindow();
            if (hwnd == IntPtr.Zero)
                return null;

            uint pid;
            GetWindowThreadProcessId(hwnd, out pid);
            try
            {
                var proc = Process.GetProcessById((int)pid);
                return proc.ProcessName + ".exe";
            }
            catch
            {
                return null;
            }
        }

        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        private static extern IntPtr MonitorFromWindow(IntPtr hwnd, uint dwFlags);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern bool GetMonitorInfo(IntPtr hMonitor, ref MONITORINFOEX lpmi);

        private const uint MONITOR_DEFAULTTONEAREST = 2;

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        private struct MONITORINFOEX
        {
            public uint cbSize;
            public RECT rcMonitor;
            public RECT rcWork;
            public uint dwFlags;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
            public string szDevice;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct RECT
        {
            public int left;
            public int top;
            public int right;
            public int bottom;
        }

        [DllImport("user32.dll")]
        private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);
    }
}
#endif