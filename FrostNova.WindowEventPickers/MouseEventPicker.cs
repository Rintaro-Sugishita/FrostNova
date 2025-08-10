using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace FrostNova.WindowEventPickers
{

    public static class MouseEventPicker
    {
        private const int WH_MOUSE_LL = 14;

        public delegate void MouseClickHandler(object? sender, MouseEventData e);
        public delegate void MouseWheelHandler(object? sender, MouseEventData e);
        public delegate void MouseMoveHandler(object? sender, MouseEventData e);

        private static Action<MouseEventData>? ClickAction;
        private static Action<MouseEventData>? DownAction;
        private static Action<MouseEventData>? UpAction;
        private static Action<MouseEventData>? WheelAction;
        private static Action<MouseEventData>? MoveAction;

        private static LowLevelMouseProc _proc = HookCallback;
        private static IntPtr _hookID = IntPtr.Zero;

        //public static event MouseClickHandler? MouseClick;
        //public static event MouseWheelHandler? MouseWheel;
        //public static event MouseMoveHandler? MouseMove;

        public static void SetHook(Action<MouseEventData>? clickHandler, Action<MouseEventData>? wheelHandler = null, Action<MouseEventData>? moveHandler = null, Action<MouseEventData>? downHandler = null, Action<MouseEventData>? upHandler = null)
        {
            ClickAction = clickHandler;
            WheelAction = wheelHandler;
            MoveAction = moveHandler;
            DownAction = downHandler;
            UpAction = upHandler;
            //MouseClick += clickHandler;
            //MouseMove += moveHandler;
            //MouseWheel += wheelHandler;
            SetHook();
        }

        public static void SetHook()
        {
            _hookID = SetWindowsHookEx(WH_MOUSE_LL, _proc, IntPtr.Zero, 0);
        }

        public static void Unhook()
        {
            UnhookWindowsHookEx(_hookID);
        }

        private delegate IntPtr LowLevelMouseProc(int nCode, IntPtr wParam, IntPtr lParam);

        private static IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {

            if (nCode >= 0)
            {


                if (wParam == (IntPtr)WM_LBUTTONDOWN || wParam == (IntPtr)WM_RBUTTONDOWN || wParam == (IntPtr)WM_MBUTTONDOWN)
                {
                    MSLLHOOKSTRUCT? hookStruct = (MSLLHOOKSTRUCT?)Marshal.PtrToStructure(lParam, typeof(MSLLHOOKSTRUCT));

                    if (hookStruct != null)
                    {
                        //MouseClick?.Invoke(null, new MouseEventData(GetButton(wParam), hookStruct.Value.pt.x, hookStruct.Value.pt.y));
                        DownAction?.Invoke(new MouseEventData(GetButton(wParam), hookStruct.Value.pt.x, hookStruct.Value.pt.y));
                        ClickAction?.Invoke(new MouseEventData(GetButton(wParam), hookStruct.Value.pt.x, hookStruct.Value.pt.y));
                    }
                }
               else if (wParam == (IntPtr)WM_LBUTTONUP || wParam == (IntPtr)WM_RBUTTONUP || wParam == (IntPtr)WM_MBUTTONUP)
                {
                    MSLLHOOKSTRUCT? hookStruct = (MSLLHOOKSTRUCT?)Marshal.PtrToStructure(lParam, typeof(MSLLHOOKSTRUCT));

                    if (hookStruct != null)
                    {
                        //MouseClick?.Invoke(null, new MouseEventData(GetButton(wParam), hookStruct.Value.pt.x, hookStruct.Value.pt.y));
                        UpAction?.Invoke(new MouseEventData(GetButton(wParam), hookStruct.Value.pt.x, hookStruct.Value.pt.y));
                    }
                }
                else if (wParam == (IntPtr)WM_MOUSEWHEEL)
                {
                    MSLLHOOKSTRUCT? hookStruct = (MSLLHOOKSTRUCT?)Marshal.PtrToStructure(lParam, typeof(MSLLHOOKSTRUCT));
                    if (hookStruct != null)
                    {
                        int delta = (short)(hookStruct.Value.mouseData >> 16);
                        //MouseWheel?.Invoke(null, new MouseEventData(MouseButton.Left, hookStruct.Value.pt.x, hookStruct.Value.pt.y, delta));
                       var a = new MouseEventData(MouseButton.Left, hookStruct.Value.pt.x, hookStruct.Value.pt.y, delta);
                        
                        WheelAction?.Invoke(a);
                    }
                }
                else if (wParam == (IntPtr)WM_MOUSEMOVE)
                {
                    MSLLHOOKSTRUCT? hookStruct = (MSLLHOOKSTRUCT?)Marshal.PtrToStructure(lParam, typeof(MSLLHOOKSTRUCT));
                    if (hookStruct != null)
                    {
                        //MouseMove?.Invoke(null, new MouseEventData(MouseButton.Left, hookStruct.Value.pt.x, hookStruct.Value.pt.y, 0));
                        MoveAction?.Invoke(new MouseEventData(MouseButton.Left, hookStruct.Value.pt.x, hookStruct.Value.pt.y));
                    }
                }
            }

            return CallNextHookEx(_hookID, nCode, wParam, lParam);
        }

        private static MouseButton GetButton(IntPtr wParam)
        {
            if (wParam == (IntPtr)WM_LBUTTONDOWN || wParam == (IntPtr)WM_LBUTTONUP)
                return MouseButton.Left;
            else if (wParam == (IntPtr)WM_RBUTTONDOWN || wParam == (IntPtr)WM_RBUTTONUP)
                return MouseButton.Right;
            else if (wParam == (IntPtr)WM_MBUTTONDOWN || wParam == (IntPtr)WM_MBUTTONUP)
                return MouseButton.Middle;
            else
                return MouseButton.Left;
        }


        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelMouseProc lpfn, IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

        private const int WM_LBUTTONDOWN = 0x0201;
        private const int WM_LBUTTONUP = 0x0202;
        private const int WM_RBUTTONDOWN = 0x0204;
        private const int WM_RBUTTONUP = 0x0205;
        private const int WM_MBUTTONDOWN = 0x0207;
        private const int WM_MBUTTONUP = 0x0208;
        private const int WM_MOUSEWHEEL = 0x020A;
        private const int WM_MOUSEMOVE = 0x0200;

        [StructLayout(LayoutKind.Sequential)]
        private struct POINT
        {
            public int x;
            public int y;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct MSLLHOOKSTRUCT
        {
            public POINT pt;
            public int mouseData;
            public int flags;
            public int time;
            public IntPtr dwExtraInfo;
        }
    }

}
