﻿// ------------------------
//    WInterop Framework
// ------------------------

// Copyright (c) Jeremy W. Kuhne. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Drawing;
using WInterop.Windows;

namespace PopPad1
{
    /// <summary>
    /// Sample from Programming Windows, 5th Edition.
    /// Original (c) Charles Petzold, 1998
    /// Figure 9-7, Pages 396-397.
    /// </summary>
    static class Program
    {
        [STAThread]
        static void Main()
        {
            Windows.CreateMainWindowAndRun(new PopPad1(), "Popup editor using child window edit box");
        }
    }

    class PopPad1 : WindowClass
    {
        WindowHandle hwndEdit;
        const int ID_EDIT = 1;

        protected unsafe override LResult WindowProcedure(WindowHandle window, MessageType message, WParam wParam, LParam lParam)
        {
            switch (message)
            {
                case MessageType.Create:
                    hwndEdit = Windows.CreateWindow("edit", "",
                        WindowStyles.Child | WindowStyles.Visible | WindowStyles.HorizontalScroll | WindowStyles.VerticalScroll
                        | WindowStyles.Border | (WindowStyles)EditStyles.Left | (WindowStyles)EditStyles.Multiline
                        | (WindowStyles)EditStyles.AutoHorizontalScroll | (WindowStyles)EditStyles.AutoVerticalScroll,
                        ExtendedWindowStyles.Default, new Rectangle(), window, (MenuHandle)ID_EDIT, ModuleInstance, IntPtr.Zero);
                    return 0;
                case MessageType.SetFocus:
                    hwndEdit.SetFocus();
                    return 0;
                case MessageType.Size:
                    hwndEdit.MoveWindow(new Rectangle(0, 0, lParam.LowWord, lParam.HighWord), repaint: true);
                    return 0;
                case MessageType.Command:
                    if (wParam.LowWord == ID_EDIT
                        && (wParam.HighWord == (ushort)EditNotification.ErrorSpace || wParam.HighWord == (ushort)EditNotification.MaxText))
                        window.MessageBox("Edit control out of space.", "PopPad1", MessageBoxType.Ok | MessageBoxType.IconStop);
                    return 0;
                case MessageType.Destroy:
                    Windows.PostQuitMessage(0);
                    return 0;
            }

            return base.WindowProcedure(window, message, wParam, lParam);
        }
    }
}
