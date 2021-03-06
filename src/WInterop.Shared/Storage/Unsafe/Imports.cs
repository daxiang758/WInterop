﻿// ------------------------
//    WInterop Framework
// ------------------------

// Copyright (c) Jeremy W. Kuhne. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Win32.SafeHandles;
using System;
using System.Runtime.InteropServices;
using WInterop.Errors;
using WInterop.Handles;
using WInterop.Synchronization;

namespace WInterop.Storage.Unsafe
{
    /// <summary>
    /// Direct usage of Imports isn't recommended. Use the wrappers that do the heavy lifting for you.
    /// </summary>
    public static partial class Imports
    {
        // NTFS Technical Reference
        // https://technet.microsoft.com/en-us/library/cc758691.aspx

        // https://docs.microsoft.com/en-us/windows/desktop/api/fileapi/nf-fileapi-createfile2 (kernel32)
        [DllImport(ApiSets.api_ms_win_core_file_l1_2_0, CharSet = CharSet.Unicode, SetLastError = true, ExactSpelling = true)]
        public static extern SafeFileHandle CreateFile2(
            ref char lpFileName,
            DesiredAccess dwDesiredAccess,
            ShareModes dwShareMode,
            CreationDisposition dwCreationDisposition,
            ref CREATEFILE2_EXTENDED_PARAMETERS pCreateExParams);

        // https://docs.microsoft.com/en-us/windows/desktop/api/winbase/nf-winbase-copyfileexw
        // CopyFile calls CopyFileEx with COPY_FILE_FAIL_IF_EXISTS if fail if exists is set
        // (Neither are available in WinRT- use CopyFile2)
        [DllImport(Libraries.Kernel32, SetLastError = true, CharSet = CharSet.Unicode, ExactSpelling = true)]
        public static extern bool CopyFileExW(
            string lpExistingFileName,
            string lpNewFileName,
            CopyProgressRoutine lpProgressRoutine,
            IntPtr lpData,
            ref bool pbCancel,
            CopyFileFlags dwCopyFlags);

        // https://docs.microsoft.com/en-us/windows/desktop/api/winbase/nf-winbase-copyfile2 (kernel32)
        [DllImport(Libraries.Kernel32, CharSet = CharSet.Unicode, ExactSpelling = true)]
        public static extern HResult CopyFile2(
            string pwszExistingFileName,
            string pwszNewFileName,
            ref COPYFILE2_EXTENDED_PARAMETERS pExtendedParameters);

        // https://docs.microsoft.com/en-us/windows/desktop/api/winbase/nf-winbase-replacefilew
        [DllImport(Libraries.Kernel32, CharSet = CharSet.Unicode, ExactSpelling = true)]
        public static extern bool ReplaceFileW(
            string lpReplacedFileName,
            string lpReplacementFileName,
            string lpBackupFileName,
            ReplaceFileFlags dwReplaceFlags,
            IntPtr lpExclude,
            IntPtr lpReserved);

        // https://docs.microsoft.com/en-us/windows/desktop/api/fileapi/nf-fileapi-getfileattributesw
        [DllImport(Libraries.Kernel32, CharSet = CharSet.Unicode, SetLastError = true, ExactSpelling = true)]
        public static extern FileAttributes GetFileAttributesW(
            string lpFileName);

        // https://docs.microsoft.com/en-us/windows/desktop/api/fileapi/nf-fileapi-getfileattributesexw (kernel32)
        [DllImport(ApiSets.api_ms_win_core_file_l1_1_0, CharSet = CharSet.Unicode, SetLastError = true, ExactSpelling = true)]
        public static extern bool GetFileAttributesExW(
            string lpFileName,
            GetFileExtendedInformationLevels fInfoLevelId,
            out Win32FileAttributeData lpFileInformation);

        // https://docs.microsoft.com/en-us/windows/desktop/api/fileapi/nf-fileapi-setfileattributesw (kernel32)
        [DllImport(ApiSets.api_ms_win_core_file_l1_1_0, CharSet = CharSet.Unicode, SetLastError = true, ExactSpelling = true)]
        public static extern bool SetFileAttributesW(
            string lpFileName,
            FileAttributes dwFileAttributes);

        // https://docs.microsoft.com/en-us/windows/desktop/api/fileapi/nf-fileapi-getfullpathnamew (kernel32)
        [DllImport(ApiSets.api_ms_win_core_file_l1_1_0, CharSet = CharSet.Unicode, SetLastError = true, ExactSpelling = true)]
        public static extern uint GetFullPathNameW(
            string lpFileName,
            uint nBufferLength,
            SafeHandle lpBuffer,
            IntPtr lpFilePart);

        // https://docs.microsoft.com/en-us/windows/desktop/api/fileapi/nf-fileapi-getfinalpathnamebyhandlew (kernel32)
        [DllImport(ApiSets.api_ms_win_core_file_l1_1_0, CharSet = CharSet.Unicode, SetLastError = true, ExactSpelling = true)]
        public static extern uint GetFinalPathNameByHandleW(
            SafeFileHandle hFile,
            SafeHandle lpszFilePath,
            uint cchFilePath,
            GetFinalPathNameByHandleFlags dwFlags);

        // https://docs.microsoft.com/en-us/windows/desktop/api/fileapi/nf-fileapi-getlongpathnamew (kernel32)
        [DllImport(ApiSets.api_ms_win_core_file_l1_1_0, CharSet = CharSet.Unicode, SetLastError = true, ExactSpelling = true)]
        public static extern uint GetLongPathNameW(
            string lpszShortPath,
            SafeHandle lpszLongPath,
            uint cchBuffer);

        // https://docs.microsoft.com/en-us/windows/desktop/api/fileapi/nf-fileapi-findfirstfilew
        [DllImport(ApiSets.api_ms_win_core_file_l1_1_0, SetLastError = true, CharSet = CharSet.Unicode, ExactSpelling = true)]
        public static extern IntPtr FindFirstFileW(
            string lpFileName,
            out Win32FindData lpFindFileData);

        // https://docs.microsoft.com/en-us/windows/desktop/api/fileapi/nf-fileapi-findfirstfileexw (kernel32)
        [DllImport(ApiSets.api_ms_win_core_file_l1_1_0, SetLastError = true, CharSet = CharSet.Unicode, ExactSpelling = true)]
        public static extern IntPtr FindFirstFileExW(
                string lpFileName,
                FindExtendedInfoLevels fInfoLevelId,
                out Win32FindData lpFindFileData,
                uint fSearchOp,                        // This never actually has meaning and is likely a holdover of 9x
                                                        // set it to 0 to avoid failing parameter checks.
                IntPtr lpSearchFilter,                 // Reserved
                FindFirstFileExtendedFlags dwAdditionalFlags);

        // https://docs.microsoft.com/en-us/windows/desktop/api/fileapi/nf-fileapi-findnextfilew (kernel32)
        [DllImport(ApiSets.api_ms_win_core_file_l1_1_0, SetLastError = true, CharSet = CharSet.Unicode, ExactSpelling = true)]
        public static extern bool FindNextFileW(
            IntPtr hFindFile,
            out Win32FindData lpFindFileData);

        // https://docs.microsoft.com/en-us/windows/desktop/api/fileapi/nf-fileapi-findclose (kernel32)
        [DllImport(ApiSets.api_ms_win_core_file_l1_1_0, SetLastError = true, ExactSpelling = true)]
        public static extern bool FindClose(
            IntPtr hFindFile);

        // https://docs.microsoft.com/en-us/windows/desktop/api/winbase/nf-winbase-getfileinformationbyhandleex (kernel32)
        [DllImport(ApiSets.api_ms_win_core_file_l2_1_0, SetLastError = true, CharSet = CharSet.Unicode, ExactSpelling = true)]
        public unsafe static extern bool GetFileInformationByHandleEx(
            IntPtr hFile,
            FileInfoClass FileInformationClass,
            void* lpFileInformation,
            uint dwBufferSize);

        public unsafe static bool GetFileInformationByHandleEx(
            SafeFileHandle hFile,
            FileInfoClass FileInformationClass,
            void* lpFileInformation,
            uint dwBufferSize)
        {
            using (var handle = new UnwrapHandle(hFile))
            {
                return GetFileInformationByHandleEx(handle, FileInformationClass, lpFileInformation, dwBufferSize);
            }
        }

        // https://docs.microsoft.com/en-us/windows/desktop/api/fileapi/nf-fileapi-deletefilew (kernel32)
        [DllImport(ApiSets.api_ms_win_core_file_l1_1_0, SetLastError = true, CharSet = CharSet.Unicode, ExactSpelling = true)]
        public static extern bool DeleteFileW(
            string lpFilename);

        // https://docs.microsoft.com/en-us/windows/desktop/api/fileapi/nf-fileapi-readfile
        [DllImport(Libraries.Kernel32, SetLastError = true, ExactSpelling = true)]
        public unsafe static extern bool ReadFile(
            SafeFileHandle hFile,
            ref byte lpBuffer,
            uint nNumberOfBytesToRead,
            out uint lpNumberOfBytesRead,
            OVERLAPPED* lpOverlapped);

        // https://docs.microsoft.com/en-us/windows/desktop/api/fileapi/nf-fileapi-writefile
        [DllImport(Libraries.Kernel32, SetLastError = true, ExactSpelling = true)]
        public unsafe static extern bool WriteFile(
            SafeFileHandle hFile,
            ref byte lpBuffer,
            uint nNumberOfBytesToWrite,
            out uint lpNumberOfBytesWritten,
            OVERLAPPED* lpOverlapped);

        // https://docs.microsoft.com/en-us/windows/desktop/api/fileapi/nf-fileapi-setfilepointerex
        [DllImport(Libraries.Kernel32, SetLastError = true, ExactSpelling = true)]
        public static extern bool SetFilePointerEx(
            SafeFileHandle hFile,
            long liDistanceToMove,
            out long lpNewFilePointer,
            MoveMethod dwMoveMethod);

        // https://docs.microsoft.com/en-us/windows/desktop/api/fileapi/nf-fileapi-getfilesizeex
        // This returns FILE_STANDARD_INFO.EndOfFile
        [DllImport(Libraries.Kernel32, SetLastError = true, ExactSpelling = true)]
        public static extern bool GetFileSizeEx(
            SafeFileHandle hFile,
            out long lpFileSize);

        // https://docs.microsoft.com/en-us/windows/desktop/api/fileapi/nf-fileapi-getfiletype
        [DllImport(Libraries.Kernel32, SetLastError = true, ExactSpelling = true)]
        public static extern FileType GetFileType(
            SafeFileHandle hFile);

        // https://docs.microsoft.com/en-us/windows/desktop/api/fileapi/nf-fileapi-flushfilebuffers (kernel32)
        [DllImport(ApiSets.api_ms_win_core_file_l1_1_0, SetLastError = true, ExactSpelling = true)]
        public static extern bool FlushFileBuffers(
            SafeFileHandle hFile);

        // https://docs.microsoft.com/en-us/windows/desktop/api/fileapi/nf-fileapi-gettemppathw (kernel32)
        [DllImport(ApiSets.api_ms_win_core_file_l1_2_0, SetLastError = true, ExactSpelling = true)]
        public static extern uint GetTempPathW(
            uint nBufferLength,
            SafeHandle lpBuffer);

        // https://docs.microsoft.com/en-us/windows/desktop/api/fileapi/nf-fileapi-gettempfilenamew (kernel32)
        [DllImport(ApiSets.api_ms_win_core_file_l1_1_0, CharSet = CharSet.Unicode, SetLastError = true, ExactSpelling = true)]
        public static extern uint GetTempFileNameW(
            string lpPathName,
            string lpPrefixString,
            uint uUnique,
            SafeHandle lpTempFileName);

        // https://docs.microsoft.com/en-us/windows/desktop/FileIO/cancelioex-func
        [DllImport(Libraries.Kernel32, SetLastError = true, ExactSpelling = true)]
        public unsafe static extern bool CancelIoEx(
            SafeFileHandle hFile,
            OVERLAPPED* lpOverlapped);

        // https://docs.microsoft.com/en-us/windows/desktop/api/ioapiset/nf-ioapiset-getoverlappedresultex
        [DllImport(Libraries.Kernel32, SetLastError = true, ExactSpelling = true)]
        public static extern bool GetOverlappedResultEx(
            SafeFileHandle hFile,
            ref OVERLAPPED lpOverlapped,
            out uint lpNumberOfBytesTransferred,
            uint dwMilliseconds,
            bool bAlertable);

        // https://docs.microsoft.com/en-us/windows/desktop/api/fileapi/nf-fileapi-unlockfileex
        [DllImport(Libraries.Kernel32, SetLastError = true, ExactSpelling = true)]
        public static extern bool UnlockFileEx(
            SafeFileHandle hFile,
            uint dwReserved,
            uint nNumberOfBytesToUnlockLow,
            uint nNumberOfBytesToUnlockHigh,
            ref OVERLAPPED lpOverlapped);

        // https://docs.microsoft.com/en-us/windows/desktop/api/fileapi/nf-fileapi-lockfileex
        [DllImport(Libraries.Kernel32, SetLastError = true, ExactSpelling = true)]
        public static extern bool LockFileEx(
            SafeFileHandle hFile,
            uint dwFlags,
            uint dwReserved,
            uint nNumberOfBytesToUnlockLow,
            uint nNumberOfBytesToUnlockHigh,
            ref OVERLAPPED lpOverlapped);

        // https://docs.microsoft.com/en-us/windows/desktop/api/winbase/nf-winbase-readdirectorychangesw
        [DllImport(Libraries.Kernel32, ExactSpelling = true)]
        public unsafe static extern bool ReadDirectoryChangesW(
            SafeFileHandle hDirectory,
            void* lpBuffer,
            uint nBufferLength,
            bool bWatchSubtree,
            FileNotifyChange dwNotifyFilter,
            out uint lpBytesReturned,
            ref OVERLAPPED lpOverlapped,
            FileIOCompletionRoutine lpCompletionRoutine);

        // https://docs.microsoft.com/en-us/windows/desktop/api/fileapi/nf-fileapi-removedirectoryw
        [DllImport(Libraries.Kernel32, CharSet = CharSet.Unicode, SetLastError = true, ExactSpelling = true)]
        public static extern bool RemoveDirectoryW(
            string lpPathName);

        // https://docs.microsoft.com/en-us/windows/desktop/api/fileapi/nf-fileapi-createdirectoryw
        [DllImport(Libraries.Kernel32, CharSet = CharSet.Unicode, SetLastError = true, ExactSpelling = true)]
        public static extern bool CreateDirectoryW(
            string lpPathName,
            IntPtr lpSecurityAttributes);

        // https://docs.microsoft.com/en-us/windows/desktop/api/winbase/nf-winbase-getcurrentdirectory
        [DllImport(Libraries.Kernel32, SetLastError = true, ExactSpelling = true)]
        public static extern uint GetCurrentDirectoryW(
            uint nBufferLength,
            SafeHandle lpBuffer);

        // https://docs.microsoft.com/en-us/windows/desktop/api/winbase/nf-winbase-setcurrentdirectory
        [DllImport(Libraries.Kernel32, CharSet = CharSet.Unicode, SetLastError = true, ExactSpelling = true)]
        public static extern bool SetCurrentDirectoryW(
            ref char lpPathName);

        // https://docs.microsoft.com/en-us/windows/desktop/api/fileapi/nf-fileapi-getlogicaldrives
        [DllImport(Libraries.Kernel32, SetLastError = true, ExactSpelling = true)]
        public static extern LogicalDrives GetLogicalDrives();

        // https://docs.microsoft.com/en-us/windows/desktop/api/fileapi/nf-fileapi-getvolumeinformationw
        [DllImport(Libraries.Kernel32, CharSet = CharSet.Unicode, SetLastError = true, ExactSpelling = true)]
        public static extern bool GetVolumeInformationW(
            string lpRootPathName,
            SafeHandle lpVolumeNameBuffer,
            uint nVolumeNameSize,
            out uint lpVolumeSerialNumber,
            out uint lpMaximumComponentLength,
            out FileSystemFeature lpFileSystemFlags,
            SafeHandle lpFileSystemNameBuffer,
            uint nFileSystemNameSize);

        // https://docs.microsoft.com/en-us/windows/desktop/api/fileapi/nf-fileapi-getdrivetypew
        [DllImport(Libraries.Kernel32, CharSet = CharSet.Unicode, ExactSpelling = true)]
        public static extern DriveType GetDriveTypeW(
            string lpRootPathName);

        // https://docs.microsoft.com/en-us/windows/desktop/api/fileapi/nf-fileapi-getdiskfreespacew
        [DllImport(Libraries.Kernel32, CharSet = CharSet.Unicode, SetLastError = true, ExactSpelling = true)]
        public unsafe static extern bool GetDiskFreeSpaceW(
            string lpRootPathName,
            uint* lpSectorsPerCluster,
            uint* lpBytesPerSector,
            uint* lpNumberOfFreeClusters,
            uint* lpTotalNumberOfClusters);

        // https://docs.microsoft.com/en-us/windows/desktop/api/fileapi/nf-fileapi-getdiskfreespaceexw
        [DllImport(Libraries.Kernel32, CharSet = CharSet.Unicode, SetLastError = true, ExactSpelling = true)]
        public unsafe static extern bool GetDiskFreeSpaceExW(
            string lpDirectoryName,
            ulong* lpFreeBytesAvailable,
            ulong* lpTotalNumberOfBytes,
            ulong* lpTotalNumberOfFreeBytes);
    }
}
