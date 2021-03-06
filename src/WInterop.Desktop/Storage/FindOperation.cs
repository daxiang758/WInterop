﻿// ------------------------
//    WInterop Framework
// ------------------------

// Copyright (c) Jeremy W. Kuhne. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.ConstrainedExecution;
using WInterop.Errors;
using WInterop.Storage.Unsafe;

namespace WInterop.Storage
{
    public partial class FindOperation<T> : IEnumerable<T>
    {
        private unsafe partial class FindEnumerator : CriticalFinalizerObject, IEnumerator<T>
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private IntPtr CreateDirectoryHandle(string fileName, string subDirectory)
            {
                return Storage.CreateDirectoryHandle(_directory, fileName);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private void GetData()
            {
                NTStatus status = Imports.NtQueryDirectoryFile(
                    FileHandle: _directory,
                    Event: IntPtr.Zero,
                    ApcRoutine: null,
                    ApcContext: IntPtr.Zero,
                    IoStatusBlock: out IO_STATUS_BLOCK statusBlock,
                    FileInformation: _buffer.VoidPointer,
                    Length: (uint)_buffer.ByteCapacity,
                    FileInformationClass: FileInformationClass.FileFullDirectoryInformation,
                    ReturnSingleEntry: false,
                    FileName: null,
                    RestartScan: false);

                switch (status)
                {
                    case NTStatus.STATUS_NO_MORE_FILES:
                        NoMoreFiles();
                        return;
                    case NTStatus.STATUS_SUCCESS:
                        Debug.Assert(statusBlock.Information.ToInt64() != 0);
                        break;
                    default:
                        throw status.GetException();
                }
            }
        }
    }
}
