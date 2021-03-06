﻿// ------------------------
//    WInterop Framework
// ------------------------

// Copyright (c) Jeremy W. Kuhne. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using WInterop.GdiPlus.Unsafe;

namespace WInterop.GdiPlus
{
    public readonly struct GpBrush : IDisposable
    {
        public IntPtr Handle { get; }

        public void Dispose()
        {
            if (Handle != IntPtr.Zero)
                GdiPlusMethods.ThrowIfFailed(Imports.GdipDeleteBrush(Handle));
        }
    }
}