﻿// ------------------------
//    WInterop Framework
// ------------------------

// Copyright (c) Jeremy W. Kuhne. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using WInterop.Errors;
using WInterop.Handles.Unsafe;

namespace WInterop.Handles
{
    public static partial class Handles
    {
        public static void CloseHandle(IntPtr handle)
        {
            if (!Imports.CloseHandle(handle))
                throw Error.GetExceptionForLastError();
        }
    }
}
