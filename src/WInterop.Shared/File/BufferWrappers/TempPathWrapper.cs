﻿// ------------------------
//    WInterop Framework
// ------------------------

// Copyright (c) Jeremy W. Kuhne. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using WInterop.Support.Buffers;

namespace WInterop.File.BufferWrappers
{
    public struct TempPathWrapper : IBufferFunc<StringBuffer, uint>
    {
        uint IBufferFunc<StringBuffer, uint>.Func(StringBuffer buffer)
        {
            return FileMethods.Imports.GetTempPathW(buffer.CharCapacity, buffer);
        }
    }
}
