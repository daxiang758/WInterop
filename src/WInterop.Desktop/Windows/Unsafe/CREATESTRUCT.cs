﻿// ------------------------
//    WInterop Framework
// ------------------------

// Copyright (c) Jeremy W. Kuhne. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.InteropServices;
using WInterop.Windows.Unsafe;
using WInterop.Support;

namespace WInterop.Windows.Unsafe
{
    // https://msdn.microsoft.com/en-us/library/windows/desktop/ms632603.aspx
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    public unsafe readonly ref struct CREATESTRUCT
    {
        public readonly IntPtr lpCreateParams;
        public readonly IntPtr hInstance;
        public readonly HMENU hMenu;
        public readonly HWND hwndParent;
        public readonly int cy;
        public readonly int cx;
        public readonly int y;
        public readonly int x;
        public readonly WindowStyles style;
        public readonly char* lpszName;
        public readonly char* lpszClass;
        public readonly ExtendedWindowStyles dwExStyle;

        public Atom Atom
            => (lpszClass != null && Atom.IsAtom((IntPtr)lpszClass)) ? new Atom((uint)lpszClass) : default;

        public ReadOnlySpan<char> ClassName
            => (lpszClass != null && !Atom.IsAtom((IntPtr)lpszClass)) ? Conversion.NullTerminatedStringToSpan(lpszClass) : default;

        public ReadOnlySpan<char> WindowName
            => Conversion.NullTerminatedStringToSpan(lpszName);
    }
}
