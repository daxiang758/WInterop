﻿// ------------------------
//    WInterop Framework
// ------------------------

// Copyright (c) Jeremy W. Kuhne. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using WInterop.Gdi.Unsafe;

namespace WInterop.Windows.Unsafe
{
    public struct TPMPARAMS
    {
        public uint cbSize;
        public RECT rcExclude;
    }
}
