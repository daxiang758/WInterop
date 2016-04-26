﻿// ------------------------
//    WInterop Framework
// ------------------------

// Copyright (c) Jeremy W. Kuhne. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace WInterop.Handles
{
    /// <summary>
    /// Safe handle for a file.
    /// </summary>
    /// <remarks>
    /// For some reason the Win32.SafeHandles. SafeFileHandle isn't available on portable.
    /// </remarks>
    public class SafeFileHandle : SafeCloseHandle
    {
        public SafeFileHandle() : base() { }
    }
}
