/****************************** Module Header ******************************\
* Module Name:	NativeMethod.cs
* Project:		CSWindowsHook
* Copyright (c) Microsoft Corporation.
* 
* The P/Invoke signatures of some native APIs.
* 
* This source is subject to the Microsoft Public License.
* See http://www.microsoft.com/opensource/licenses.mspx#Ms-PL.
* All other rights reserved.
* 
* History:
* * 4/6/2009 10:57 AM Jialiang Ge Created
\***************************************************************************/

#region Using directives
using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;
#endregion


/// <summary>
/// Native methods
/// </summary>
internal class NativeMethod
{
    /// <summary>
    /// Get current thread ID.
    /// </summary>
    /// <returns></returns>
    [DllImport("kernel32.dll")]
    internal static extern uint GetCurrentThreadId();

    /// <summary>
    /// Get current process ID.
    /// </summary>
    [DllImport("kernel32.dll")]
    internal static extern uint GetCurrentProcessId();

    /// <summary>
    /// Get the Hwnd for the module name
    /// </summary>
    /// <param name="lpModuleName"></param>
    /// <returns></returns>
    [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    internal static extern IntPtr GetModuleHandle(string lpModuleName);

    /// <summary>
    /// The SetWindowsHookEx function installs an application-defined hook 
    /// procedure into a hook chain. You would install a hook procedure to monitor 
    /// the system for certain types of events. These events are associated either 
    /// with a specific thread or with all threads in the same desktop as the 
    /// calling thread. 
    /// </summary>
    /// <param name="hookType">
    /// Specifies the type of hook procedure to be installed
    /// </param>
    /// <param name="callback">Pointer to the hook procedure.</param>
    /// <param name="hMod">
    /// Handle to the DLL containing the hook procedure pointed to by the lpfn 
    /// parameter. The hMod parameter must be set to NULL if the dwThreadId 
    /// parameter specifies a thread created by the current process and if the 
    /// hook procedure is within the code associated with the current process. 
    /// </param>
    /// <param name="dwThreadId">
    /// Specifies the identifier of the thread with which the hook procedure is 
    /// to be associated.
    /// </param>
    /// <returns>
    /// If the function succeeds, the return value is the handle to the hook 
    /// procedure. If the function fails, the return value is 0.
    /// </returns>
    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    public static extern IntPtr SetWindowsHookEx(HookType hookType,
        HookProc callback, IntPtr hMod, uint dwThreadId);

    /// <summary>
    /// The UnhookWindowsHookEx function removes a hook procedure installed in 
    /// a hook chain by the SetWindowsHookEx function. 
    /// </summary>
    /// <param name="hhk">Handle to the hook to be removed.</param>
    /// <returns>
    /// If the function succeeds, the return value is true.
    /// If the function fails, the return value is false.
    /// </returns>
    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    public static extern bool UnhookWindowsHookEx(IntPtr hhk);

    /// <summary>
    /// The CallNextHookEx function passes the hook information to the next hook 
    /// procedure in the current hook chain. A hook procedure can call this 
    /// function either before or after processing the hook information. 
    /// </summary>
    /// <param name="idHook">Handle to the current hook.</param>
    /// <param name="nCode">
    /// Specifies the hook code passed to the current hook procedure.
    /// </param>
    /// <param name="wParam">
    /// Specifies the wParam value passed to the current hook procedure.
    /// </param>
    /// <param name="lParam">
    /// Specifies the lParam value passed to the current hook procedure.
    /// </param>
    /// <returns>
    /// This value is returned by the next hook procedure in the chain.
    /// </returns>
    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    public static extern int CallNextHookEx(IntPtr hhk, int nCode,
        IntPtr wParam, IntPtr lParam);

    [DllImport("kernel32", CharSet = CharSet.Auto)]
    public static extern bool SetProcessWorkingSetSize(IntPtr handle, int minSize, int maxSize);
}
