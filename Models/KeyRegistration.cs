using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

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

            _timer = new System.Timers.Timer(5000);
            _timer.Elapsed += (s, e) =>
            {
                if (_lastInputTime != DateTime.MinValue)
                {
                    Console.WriteLine($"Última entrada detectada a las {_lastInputTime:HH:mm:ss}");
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
                    Console.WriteLine($"Tecla presionada: {vkCode} a las {_lastInputTime:HH:mm:ss}");
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
                if (wParam == (IntPtr)WM_LBUTTONDOWN)
                {
                    _lastInputTime = DateTime.Now;
                    Console.WriteLine($"Botón izquierdo del ratón presionado a las {_lastInputTime:HH:mm:ss}");
                }
                else if (wParam == (IntPtr)WM_RBUTTONDOWN)
                {
                    _lastInputTime = DateTime.Now;
                    Console.WriteLine($"Botón derecho del ratón presionado a las {_lastInputTime:HH:mm:ss}");
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
    }
}
