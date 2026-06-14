using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace FrostNova.WindowEventPickers
{
    public static class KeyEventPicker
    {
        private static LowLevelKeyboardProc _callback = HookCallback;

        public delegate void KeyPressedHandler(object? sender, KeyEventData e);
        private static KeyPressedHandler? keyDelegate;        
        private static KeyPressedHandler? keyDownDelegate;        
        private static KeyPressedHandler? keyUpDelegate;        
        private const int WH_KEYBOARD_LL = 13;
        private const int WM_KEYDOWN = 0x0100;
        private const int WM_SYSKEYDOWN = 0x0104;
        private const int WM_KEYUP = 0x0101;
        private const int WM_SYSKEYUP = 0x0105;

        private static IntPtr hookID = IntPtr.Zero;

        public static void SetHook(KeyPressedHandler handler)
        {
            keyDelegate = handler;
            SetHook();
        }

        public static void SetHook(KeyPressedHandler downHandler, KeyPressedHandler upHandler)
        {
            keyDownDelegate = downHandler;
            keyUpDelegate = upHandler;
            SetHook();
        }


        public static void SetHook()
        {
            hookID = SetWindowsHookEx(WH_KEYBOARD_LL, _callback, IntPtr.Zero, 0);
        }


        public static void Unhook()
        {
            UnhookWindowsHookEx(hookID);
        }

        private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);

        private static IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            KeyEventData data;
            if (nCode >= 0 && (wParam == (IntPtr)WM_KEYDOWN || wParam == (IntPtr)WM_SYSKEYDOWN))
            {
                int vkCode = Marshal.ReadInt32(lParam);
                Key key = KeyInterop.KeyFromVirtualKey(vkCode);
                if (vkCode == 0x0D) // Enter key
                {
                    data = new KeyEventData(key, IsNumpadEnterKey(lParam));
                }
                else
                {
                    data = new KeyEventData(key);
                }

                keyDelegate?.Invoke(null, data);
                keyDownDelegate?.Invoke(null, data);
            }
            else if (nCode >= 0 && (wParam == (IntPtr)WM_KEYUP || wParam == (IntPtr)WM_SYSKEYUP))
            {
                int vkCode = Marshal.ReadInt32(lParam);
                Key key = KeyInterop.KeyFromVirtualKey(vkCode);
                if (vkCode == 0x0D) // Enter key
                {
                    data = new KeyEventData(key, IsNumpadEnterKey(lParam));
                }
                else
                {
                    data = new KeyEventData(key);
                }
                keyUpDelegate?.Invoke(null, data);
            }


            return CallNextHookEx(hookID, nCode, wParam, lParam);
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct KBDLLHOOKSTRUCT
        {
            public uint vkCode;
            public uint scanCode;
            public uint flags;
            public uint time;
            public UIntPtr dwExtraInfo;
        }

        private const uint LLKHF_EXTENDED = 0x01;

        private static bool IsNumpadEnterKey(IntPtr lParam)
        {
            var s = Marshal.PtrToStructure<KBDLLHOOKSTRUCT>(lParam);
            // ドライバによって拡張キー情報の格納先が異なる:
            // - flags の LLKHF_EXTENDED ビット (0x01) に格納するもの
            // - scanCode のビット8 (0x100) に拡張プレフィックスを込めるもの
            return (s.flags & LLKHF_EXTENDED) != 0 || (s.scanCode & 0x100) != 0;
        }



        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

    }


}
