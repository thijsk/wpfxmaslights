using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;
using System.Reflection;
using System.Windows;
using System.Threading;
using System.Windows.Input;
using System.Diagnostics;
using System.Windows.Threading;

namespace xmaslights
{
	class Hooks
	{
		// Handle to the global low-level mouse hook procedure
		private IntPtr hGlobalLLMouseHook = IntPtr.Zero;
		private HookProc globalLLMouseHookCallback = null;

		public delegate void MouseUpEvent(Point pt);
		public delegate void KeyUpEvent();

		public event MouseUpEvent OnMouseUp;
		public event KeyUpEvent OnKeyUp;

		private Dispatcher _backgroundMouseDispatcher;
		private Dispatcher _backgroundKeyboardDispatcher;

		/// <summary>
		/// Set global low-level mouse hook, running in a separate background thread
		/// </summary>
		/// <returns></returns>
        private void SetBackgroundHook(Action set, Action remove, Action<Dispatcher> saveDispatcher)
        {
            Thread backgroundThread = new Thread(() =>
            {
                Dispatcher threadDispatcher = Dispatcher.CurrentDispatcher;
                saveDispatcher(threadDispatcher);
                threadDispatcher.BeginInvoke(new Action(() =>
                {
                    set();
                }));
                Dispatcher.Run();
                remove();
            });
            backgroundThread.IsBackground = true;
            backgroundThread.SetApartmentState(ApartmentState.STA);
            backgroundThread.Priority = ThreadPriority.AboveNormal;
            backgroundThread.Start();
        }
			

		public void SetBackgroundGlobalLLMouseHook()
        {
            RemoveBackgroundGlobalLLMouseHook();
			SetBackgroundHook(new Action(() => { SetGlobalLLMouseHook(); }), new Action(() => { RemoveGlobalLLMouseHook(); }), SaveMouseDispatcher);
		}
	  

		public void SetBackgroundGlobalLLKeyboardHook()
		{
            RemoveBackgroundGlobalLLKeyboardHook();
            SetBackgroundHook(new Action(() => { SetGlobalLLKeyboardHook(); }), new Action(() => { RemoveGlobalLLKeyboardHook(); }), SaveKeyboardDispatcher);
		}

        private void SaveMouseDispatcher(Dispatcher saveDispatcher)
        {
            this._backgroundMouseDispatcher = saveDispatcher;
        }

        private void SaveKeyboardDispatcher(Dispatcher saveDispatcher)
        {
            this._backgroundKeyboardDispatcher = saveDispatcher;
        }

		/// <summary>
		/// Set global low-level mouse hook
		/// </summary>
		/// <returns></returns>
		private bool SetGlobalLLMouseHook()
		{
			// Create an instance of HookProc.
			globalLLMouseHookCallback = new HookProc(this.LowLevelMouseProc);

			hGlobalLLMouseHook = NativeMethod.SetWindowsHookEx(
				HookType.WH_MOUSE_LL,  // Must be LL for the global hook
				globalLLMouseHookCallback,
				// Get the handle of the current module
				Marshal.GetHINSTANCE(Assembly.GetExecutingAssembly().GetModules()[0]),
				// The hook procedure is associated with all existing threads running 
				// in the same desktop as the calling thread.
				0);

			if (hGlobalLLMouseHook == IntPtr.Zero)
			{
				using (Process curProcess = Process.GetCurrentProcess())
				using (ProcessModule curModule = curProcess.MainModule)
				{
					hGlobalLLMouseHook = NativeMethod.SetWindowsHookEx(HookType.WH_MOUSE_LL, globalLLMouseHookCallback, NativeMethod.GetModuleHandle(curModule.ModuleName), 0);
				}
			}

			return hGlobalLLMouseHook != IntPtr.Zero;
		}

		/// <summary>
		/// Remove the global low-level mouse hook
		/// </summary>
		/// <returns></returns>
		private bool RemoveGlobalLLMouseHook()
		{
			if (hGlobalLLMouseHook != IntPtr.Zero)
			{
				// Unhook the low-level mouse hook
				if (!NativeMethod.UnhookWindowsHookEx(hGlobalLLMouseHook))
					return false;

				hGlobalLLMouseHook = IntPtr.Zero;
			}
			return true;
		}

		public void RemoveBackgroundGlobalLLMouseHook()
		{
			if (_backgroundMouseDispatcher != null)
			{
				_backgroundMouseDispatcher.BeginInvokeShutdown(DispatcherPriority.Normal);
			}
		}

		public void RemoveBackgroundGlobalLLKeyboardHook()
		{
			if (_backgroundKeyboardDispatcher != null)
			{
				_backgroundKeyboardDispatcher.BeginInvokeShutdown(DispatcherPriority.Normal);
			}
		}

		/// <summary>
		/// Low-level mouse hook procedure
		/// The system call this function every time a new mouse input event is 
		/// about to be posted into a thread input queue. The mouse input can come 
		/// from the local mouse driver or from calls to the mouse_event function. 
		/// If the input comes from a call to mouse_event, the input was 
		/// "injected". However, the WH_MOUSE_LL hook is not injected into another 
		/// process. Instead, the context switches back to the process that 
		/// installed the hook and it is called in its original context. Then the 
		/// context switches back to the application that generated the event. 
		/// </summary>
		/// <param name="nCode">
		/// The hook code passed to the current hook procedure.
		/// When nCode equals HC_ACTION, the wParam and lParam parameters contain 
		/// information about a mouse message.
		/// </param>
		/// <param name="wParam">
		/// This parameter can be one of the following messages: 
		/// WM_LBUTTONDOWN, WM_LBUTTONUP, WM_MOUSEMOVE, WM_MOUSEWHEEL, 
		/// WM_MOUSEHWHEEL, WM_RBUTTONDOWN, or WM_RBUTTONUP. 
		/// </param>
		/// <param name="lParam">Pointer to an MSLLHOOKSTRUCT structure.</param>
		/// <returns></returns>
		/// <see cref="http://msdn.microsoft.com/en-us/library/ms644986.aspx"/>
		public int LowLevelMouseProc(int nCode, IntPtr wParam, IntPtr lParam)
		{
			if (nCode >= 0)
			{
				// Marshal the MSLLHOOKSTRUCT data from the callback lParam
				MSLLHOOKSTRUCT mouseLLHookStruct = (MSLLHOOKSTRUCT)
					Marshal.PtrToStructure(lParam, typeof(MSLLHOOKSTRUCT));

				// Get the mouse WM from the wParam parameter
				MouseMessage wmMouse = (MouseMessage)wParam;

				if (wmMouse == MouseMessage.WM_LBUTTONUP)
				{
					MouseUpEvent mouseUp = OnMouseUp;
					if (mouseUp != null)
					{
						ThreadPool.QueueUserWorkItem((o) => mouseUp(new Point(mouseLLHookStruct.pt.x, mouseLLHookStruct.pt.y)));
					}
				}
			}

			// Pass the hook information to the next hook procedure in chain
			return NativeMethod.CallNextHookEx(hGlobalLLMouseHook, nCode, wParam, lParam);
		}


		// Handle to the global low-level keyboard hook procedure
		private IntPtr hGlobalLLKeyboardHook = IntPtr.Zero;
		private HookProc globalLLKeyboardHookCallback = null;

		/// <summary>
		/// Set global low-level keyboard hook
		/// </summary>
		/// <returns></returns>
		public bool SetGlobalLLKeyboardHook()
		{
			// Create an instance of HookProc.
			globalLLKeyboardHookCallback = new HookProc(this.LowLevelKeyboardProc);

			hGlobalLLKeyboardHook = NativeMethod.SetWindowsHookEx(
				HookType.WH_KEYBOARD_LL,  // Must be LL for the global hook
				globalLLKeyboardHookCallback,
				// Get the handle of the current module
				Marshal.GetHINSTANCE(Assembly.GetExecutingAssembly().GetModules()[0]),
				// The hook procedure is associated with all existing threads running 
				// in the same desktop as the calling thread.
				0);

			if (hGlobalLLKeyboardHook == IntPtr.Zero)
			{
				using (Process curProcess = Process.GetCurrentProcess())
				using (ProcessModule curModule = curProcess.MainModule)
				{
					hGlobalLLKeyboardHook = NativeMethod.SetWindowsHookEx(HookType.WH_KEYBOARD_LL, globalLLKeyboardHookCallback, NativeMethod.GetModuleHandle(curModule.ModuleName), 0);
				}
			}

			return hGlobalLLKeyboardHook != IntPtr.Zero;
		}

		/// <summary>
		/// Remove the global low-level keyboard hook
		/// </summary>
		/// <returns></returns>
		private bool RemoveGlobalLLKeyboardHook()
		{
			if (hGlobalLLKeyboardHook != IntPtr.Zero)
			{
				// Unhook the mouse hook
				if (!NativeMethod.UnhookWindowsHookEx(hGlobalLLKeyboardHook))
					return false;

				hGlobalLLKeyboardHook = IntPtr.Zero;
			}
			return true;
		}

		/// <summary>
		/// Low-level keyboard hook procedure.
		/// The system calls this function every time a new keyboard input event 
		/// is about to be posted into a thread input queue. The keyboard input 
		/// can come from the local keyboard driver or from calls to the 
		/// keybd_event function. If the input comes from a call to keybd_event, 
		/// the input was "injected". However, the WH_KEYBOARD_LL hook is not 
		/// injected into another process. Instead, the context switches back 
		/// to the process that installed the hook and it is called in its 
		/// original context. Then the context switches back to the application 
		/// that generated the event. 
		/// </summary>
		/// <param name="nCode">
		/// The hook code passed to the current hook procedure.
		/// When nCode equals HC_ACTION, it means that the wParam and lParam 
		/// parameters contain information about a keyboard message.
		/// </param>
		/// <param name="wParam">
		/// The parameter can be one of the following messages: 
		/// WM_KEYDOWN, WM_KEYUP, WM_SYSKEYDOWN, or WM_SYSKEYUP.
		/// </param>
		/// <param name="lParam">Pointer to a KBDLLHOOKSTRUCT structure.</param>
		/// <returns></returns>
		/// <see cref="http://msdn.microsoft.com/en-us/library/ms644985.aspx"/>
		public int LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam)
		{
			if (nCode >= 0)
			{
				// Marshal the KeyboardHookStruct data from the callback lParam
				KBDLLHOOKSTRUCT keyboardLLHookStruct = (KBDLLHOOKSTRUCT)
					Marshal.PtrToStructure(lParam, typeof(KBDLLHOOKSTRUCT));

				// Get the virtual key code from KBDLLHOOKSTRUCT.vkCode
				// http://msdn.microsoft.com/en-us/library/dd375731.aspx
				Key vkCode = (Key)keyboardLLHookStruct.vkCode;

				// Get the keyboard WM from the wParam parameter
				KeyboardMessage wmKeyboard = (KeyboardMessage)wParam;

				if (wmKeyboard == KeyboardMessage.WM_KEYUP)
				{
					KeyUpEvent keyUp = OnKeyUp;
					if (keyUp != null)
					{
						ThreadPool.QueueUserWorkItem((o) => keyUp());
					}
				}
			}

			// Pass the hook information to the next hook procedure in chain
			return NativeMethod.CallNextHookEx(hGlobalLLKeyboardHook, nCode, wParam, lParam);
		}


	}
}
