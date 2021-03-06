﻿// ------------------------
//    WInterop Framework
// ------------------------

// Copyright (c) Jeremy W. Kuhne. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Win32.SafeHandles;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using WInterop.Errors;
using WInterop.Storage.Unsafe;
using WInterop.Handles;
using WInterop.Support;
using WInterop.Support.Buffers;
using WInterop.Security;

namespace WInterop.Storage
{
    public static partial class Storage
    {
        private struct ShortPathNameWrapper : IBufferFunc<StringBuffer, uint>
        {
            public string Path;

            uint IBufferFunc<StringBuffer, uint>.Func(StringBuffer buffer)
            {
                return Imports.GetShortPathNameW(Path, buffer, buffer.CharCapacity);
            }
        }

        /// <summary>
        /// Get the short (8.3) path version of the given path.
        /// </summary>
        public static string GetShortPathName(string path)
        {
            var wrapper = new ShortPathNameWrapper { Path = path };
            return BufferHelper.ApiInvoke(ref wrapper, path);
        }

        /// <summary>
        /// Gets the file information for the given handle.
        /// </summary>
        public static ByHandleFileInformation GetFileInformationByHandle(SafeFileHandle fileHandle)
        {
            if (!Imports.GetFileInformationByHandle(fileHandle, out ByHandleFileInformation fileInformation))
                throw Error.GetExceptionForLastError();

            return fileInformation;
        }

        /// <summary>
        /// Creates symbolic links.
        /// </summary>
        public static void CreateSymbolicLink(string symbolicLinkPath, string targetPath, bool targetIsDirectory = false)
        {
            Error.ThrowLastErrorIfFalse(Imports.CreateSymbolicLinkW(symbolicLinkPath, targetPath,
                targetIsDirectory ? SymbolicLinkFlag.Directory : SymbolicLinkFlag.File));
        }

        /// <summary>
        /// CreateFile wrapper. Desktop only. Prefer File.CreateFile() as it will handle all supported platforms.
        /// </summary>
        /// <remarks>Not available in Windows Store applications.</remarks>
        public unsafe static SafeFileHandle CreateFileW(
            string path,
            DesiredAccess desiredAccess,
            ShareModes shareMode,
            CreationDisposition creationDisposition,
            FileAttributes fileAttributes = FileAttributes.None,
            FileFlags fileFlags = FileFlags.None,
            SecurityQosFlags securityQosFlags = SecurityQosFlags.None)
        {
            uint flags = (uint)fileAttributes | (uint)fileFlags | (uint)securityQosFlags;

            SafeFileHandle handle = Imports.CreateFileW(path, desiredAccess, shareMode, lpSecurityAttributes: null, creationDisposition, flags, hTemplateFile: IntPtr.Zero);
            if (handle.IsInvalid)
                throw Error.GetExceptionForLastError(path);
            return handle;
        }

        /// <summary>
        /// NtCreateFile wrapper.
        /// </summary>
        public unsafe static SafeFileHandle CreateFileDirect(
            string path,
            CreateDisposition createDisposition,
            DesiredAccess desiredAccess = DesiredAccess.GenericReadWrite | DesiredAccess.Synchronize,
            ShareModes shareAccess = ShareModes.ReadWrite,
            FileAttributes fileAttributes = FileAttributes.None,
            CreateOptions createOptions = CreateOptions.SynchronousIoNonalert,
            ObjectAttributes objectAttributes = ObjectAttributes.CaseInsensitive)
        {
            return CreateFileRelative(path, null, createDisposition, desiredAccess, shareAccess,
                fileAttributes, createOptions, objectAttributes);
        }

        /// <summary>
        /// NtCreateFile wrapper.
        /// </summary>
        public unsafe static SafeFileHandle CreateFileDirect(
            ReadOnlySpan<char> path,
            CreateDisposition createDisposition,
            DesiredAccess desiredAccess = DesiredAccess.GenericReadWrite | DesiredAccess.Synchronize,
            ShareModes shareAccess = ShareModes.ReadWrite,
            FileAttributes fileAttributes = FileAttributes.None,
            CreateOptions createOptions = CreateOptions.SynchronousIoNonalert,
            ObjectAttributes objectAttributes = ObjectAttributes.CaseInsensitive)
        {
            return CreateFileRelative(path, null, createDisposition, desiredAccess, shareAccess,
                fileAttributes, createOptions, objectAttributes);
        }

        public unsafe static SafeFileHandle CreateFileRelative(
            ReadOnlySpan<char> path,
            SafeFileHandle rootDirectory,
            CreateDisposition createDisposition,
            DesiredAccess desiredAccess = DesiredAccess.GenericReadWrite | DesiredAccess.Synchronize,
            ShareModes shareAccess = ShareModes.ReadWrite,
            FileAttributes fileAttributes = FileAttributes.None,
            CreateOptions createOptions = CreateOptions.SynchronousIoNonalert,
            ObjectAttributes objectAttributes = ObjectAttributes.CaseInsensitive)
        {
            using (var handle = new UnwrapHandle(rootDirectory))
            {
                return new SafeFileHandle(
                    CreateFileRelative(path, handle, createDisposition, desiredAccess,
                        shareAccess, fileAttributes, createOptions, objectAttributes),
                    true);
            }
        }

        public unsafe static SafeFileHandle CreateFileRelative(
            string path,
            SafeFileHandle rootDirectory,
            CreateDisposition createDisposition,
            DesiredAccess desiredAccess = DesiredAccess.GenericReadWrite | DesiredAccess.Synchronize,
            ShareModes shareAccess = ShareModes.ReadWrite,
            FileAttributes fileAttributes = FileAttributes.None,
            CreateOptions createOptions = CreateOptions.SynchronousIoNonalert,
            ObjectAttributes objectAttributes = ObjectAttributes.CaseInsensitive)
        {
            return CreateFileRelative(path.AsSpan(), rootDirectory, createDisposition, desiredAccess,
                shareAccess, fileAttributes, createOptions, objectAttributes);
        }

        public unsafe static IntPtr CreateFileRelative(
            string path,
            IntPtr rootDirectory,
            CreateDisposition createDisposition,
            DesiredAccess desiredAccess = DesiredAccess.GenericReadWrite | DesiredAccess.Synchronize,
            ShareModes shareAccess = ShareModes.ReadWrite,
            FileAttributes fileAttributes = FileAttributes.None,
            CreateOptions createOptions = CreateOptions.SynchronousIoNonalert,
            ObjectAttributes objectAttributes = ObjectAttributes.CaseInsensitive)
        {
            return CreateFileRelative(path.AsSpan(), rootDirectory, createDisposition, desiredAccess,
                shareAccess, fileAttributes, createOptions, objectAttributes);
        }

        public unsafe static IntPtr CreateFileRelative(
            ReadOnlySpan<char> path,
            IntPtr rootDirectory,
            CreateDisposition createDisposition,
            DesiredAccess desiredAccess = DesiredAccess.GenericReadWrite | DesiredAccess.Synchronize,
            ShareModes shareAccess = ShareModes.ReadWrite,
            FileAttributes fileAttributes = FileAttributes.None,
            CreateOptions createOptions = CreateOptions.SynchronousIoNonalert,
            ObjectAttributes objectAttributes = ObjectAttributes.CaseInsensitive)
        {
            fixed (char* c = &MemoryMarshal.GetReference(path))
            {
                var name = new SafeString.Unsafe.UNICODE_STRING(c, path.Length);
                var attributes = new Handles.Unsafe.OBJECT_ATTRIBUTES(
                    &name,
                    objectAttributes,
                    rootDirectory,
                    null,
                    null);

                Imports.NtCreateFile(
                    out IntPtr handle,
                    desiredAccess,
                    ref attributes,
                    out IO_STATUS_BLOCK statusBlock,
                    AllocationSize: null,
                    FileAttributes: fileAttributes,
                    ShareAccess: shareAccess,
                    CreateDisposition: createDisposition,
                    CreateOptions: createOptions,
                    EaBuffer: null,
                    EaLength: 0)
                    .ThrowIfFailed(path.ToString());

                return handle;
            }
        }

        /// <summary>
        /// Wrapper to create a directory within another directory
        /// </summary>
        public static SafeFileHandle CreateDirectory(SafeFileHandle rootDirectory, string name)
        {
            return CreateFileRelative(
                name,
                rootDirectory,
                CreateDisposition.Create,
                DesiredAccess.ListDirectory | DesiredAccess.Synchronize,
                ShareModes.ReadWrite | ShareModes.Delete,
                FileAttributes.None,
                CreateOptions.SynchronousIoNonalert | CreateOptions.DirectoryFile | CreateOptions.OpenForBackupIntent | CreateOptions.OpenReparsePoint);
        }

        /// <summary>
        /// Creates a directory handle from an existing directory handle.
        /// </summary>
        public static SafeFileHandle CreateDirectoryHandle(SafeFileHandle rootDirectory, string subdirectoryPath)
        {
            return CreateFileRelative(
                subdirectoryPath,
                rootDirectory,
                CreateDisposition.Open,
                DesiredAccess.ListDirectory | DesiredAccess.Synchronize,
                ShareModes.ReadWrite | ShareModes.Delete,
                FileAttributes.None,
                CreateOptions.SynchronousIoNonalert | CreateOptions.DirectoryFile | CreateOptions.OpenForBackupIntent | CreateOptions.OpenReparsePoint);
        }

        /// <summary>
        /// Creates a raw directory handle from an existing directory handle.
        /// </summary>
        public static IntPtr CreateDirectoryHandle(IntPtr rootDirectory, string subdirectoryPath)
        {
            return CreateFileRelative(
                subdirectoryPath,
                rootDirectory,
                CreateDisposition.Open,
                DesiredAccess.ListDirectory | DesiredAccess.Synchronize,
                ShareModes.ReadWrite | ShareModes.Delete,
                FileAttributes.None,
                CreateOptions.SynchronousIoNonalert | CreateOptions.DirectoryFile | CreateOptions.OpenForBackupIntent | CreateOptions.OpenReparsePoint);
        }

        /// <summary>
        /// CopyFileEx wrapper. Desktop only. Prefer File.CopyFile() as it will handle all supported platforms.
        /// </summary>
        /// <param name="overwrite">Overwrite an existing file if true.</param>
        public static void CopyFileEx(string source, string destination, bool overwrite = false)
        {
            bool cancel = false;

            if (!Imports.CopyFileExW(
                lpExistingFileName: source,
                lpNewFileName: destination,
                lpProgressRoutine: null,
                lpData: IntPtr.Zero,
                pbCancel: ref cancel,
                dwCopyFlags: overwrite ? 0 : CopyFileFlags.FailIfExists))
            {
                throw Error.GetExceptionForLastError(source);
            }
        }

        public static string GetFileName(SafeFileHandle fileHandle)
        {
            // https://msdn.microsoft.com/en-us/library/windows/hardware/ff545817.aspx

            //  typedef struct _FILE_NAME_INFORMATION
            //  {
            //      ULONG FileNameLength;
            //      WCHAR FileName[1];
            //  } FILE_NAME_INFORMATION, *PFILE_NAME_INFORMATION;

            return GetFileInformationString(fileHandle, FileInformationClass.FileNameInformation);
        }

        public static string GetVolumeName(SafeFileHandle fileHandle)
        {
            // Same basic structure as FILE_NAME_INFORMATION
            return GetFileInformationString(fileHandle, FileInformationClass.FileVolumeNameInformation);
        }

        /// <summary>
        /// This is the short name for the file/directory name, not the path. Available from WindowsStore.
        /// </summary>
        public static string GetShortName(SafeFileHandle fileHandle)
        {
            // Same basic structure as FILE_NAME_INFORMATION
            return GetFileInformationString(fileHandle, FileInformationClass.FileAlternateNameInformation);
        }

        private unsafe static string GetFileInformationString(SafeFileHandle fileHandle, FileInformationClass fileInformationClass)
        {
            return BufferHelper.BufferInvoke((HeapBuffer buffer) =>
            {
                NTStatus status = NTStatus.STATUS_BUFFER_OVERFLOW;

                // Start with MAX_PATH
                uint byteLength = 260 * sizeof(char);

                FILE_NAME_INFORMATION* value = null;

                while (status == NTStatus.STATUS_BUFFER_OVERFLOW)
                {
                    // Add space for the FileNameLength
                    buffer.EnsureByteCapacity(byteLength + sizeof(uint));

                    status = Imports.NtQueryInformationFile(
                        FileHandle: fileHandle,
                        IoStatusBlock: out _,
                        FileInformation: buffer.VoidPointer,
                        Length: checked((uint)buffer.ByteCapacity),
                        FileInformationClass: fileInformationClass);

                    if (status == NTStatus.STATUS_SUCCESS || status == NTStatus.STATUS_BUFFER_OVERFLOW)
                    {
                        value = (FILE_NAME_INFORMATION*)buffer.VoidPointer;
                        byteLength = value->FileNameLength;
                    }
                }

                status.ThrowIfFailed();

                return value->FileName.CreateString();
            });
        }

        unsafe private static void GetFileInformation(SafeFileHandle fileHandle, FileInformationClass fileInformationClass, void* value, uint size)
        {
            Imports.NtQueryInformationFile(
                FileHandle: fileHandle,
                IoStatusBlock: out _,
                FileInformation: value,
                Length: size,
                FileInformationClass: fileInformationClass)
                .ThrowIfFailed();
        }

        /// <summary>
        /// Gets the file mode for the given handle.
        /// </summary>
        public unsafe static FileAccessModes GetFileMode(SafeFileHandle fileHandle)
        {
            FileAccessModes info;
            GetFileInformation(fileHandle, FileInformationClass.FileModeInformation, &info, sizeof(FileAccessModes));
            return info;
        }

        /// <summary>
        /// Return whether or not the given expression matches the given name. Takes standard
        /// Windows wildcards (*, ?, &lt;, &gt; &quot;).
        /// </summary>
        public unsafe static bool IsNameInExpression(string expression, string name, bool ignoreCase)
        {
            if (string.IsNullOrEmpty(expression) || string.IsNullOrEmpty(name))
                return false;

            // If ignore case is set, the API will uppercase the name *if* an UpcaseTable
            // is not provided. It then flips to case-sensitive. In this state the expression
            // has to be uppercase to match as expected.

            fixed (char* e = ignoreCase ? expression.ToUpperInvariant() : expression)
            fixed (char* n = name)
            {
                SafeString.Unsafe.UNICODE_STRING* eus = null;
                SafeString.Unsafe.UNICODE_STRING* nus = null;

                if (e != null)
                {
                    var temp = new SafeString.Unsafe.UNICODE_STRING(e, expression.Length);
                    eus = &temp;
                }
                if (n != null)
                {
                    var temp = new SafeString.Unsafe.UNICODE_STRING(n, name.Length);
                    nus = &temp;
                }

                return Imports.RtlIsNameInExpression(eus, nus, ignoreCase, IntPtr.Zero);
            }
        }

        /// <summary>
        /// Get the access rights applied to the given file handle.
        /// </summary>
        public unsafe static FileAccessRights GetRights(SafeFileHandle fileHandle)
        {
            FileAccessInformation access = new FileAccessInformation();
            Imports.NtQueryInformationFile(fileHandle, out _,
                &access, (uint)sizeof(FileAccessInformation), FileInformationClass.FileAccessInformation).ThrowIfFailed();
            return access.AccessFlags;
        }

        /// <summary>
        /// Get the ids for all processes that have a handle to this file system object.
        /// Does not include the current process.
        /// </summary>
        public unsafe static IEnumerable<UIntPtr> GetProcessIds(SafeFileHandle fileHandle)
        {
            return BufferHelper.BufferInvoke((HeapBuffer buffer) =>
            {
                NTStatus status = NTStatus.STATUS_INFO_LENGTH_MISMATCH;

                while (status == NTStatus.STATUS_INFO_LENGTH_MISMATCH)
                {
                    status = Imports.NtQueryInformationFile(fileHandle, out IO_STATUS_BLOCK statusBlock,
                        buffer.VoidPointer, (uint)buffer.ByteCapacity, FileInformationClass.FileProcessIdsUsingFileInformation);

                    switch (status)
                    {
                        case NTStatus.STATUS_SUCCESS:
                            break;
                        case NTStatus.STATUS_INFO_LENGTH_MISMATCH:
                            // Not a big enough buffer
                            buffer.EnsureByteCapacity((ulong)statusBlock.Information);
                            break;
                        default:
                            throw status.GetException();
                    }
                }

                return ((FILE_PROCESS_IDS_USING_FILE_INFORMATION*)buffer.VoidPointer)->ProcessIdList.ToArray();
            });
        }

        /// <summary>
        /// Returns the mapping for the specified DOS device name or the full list of DOS device names if passed null.
        /// </summary>
        /// <remarks>
        /// This will look up the symbolic link target from the dos device namespace (\??\) when a name is specified.
        /// It performs the equivalent of NtOpenDirectoryObject, NtOpenSymbolicLinkObject, then NtQuerySymbolicLinkObject.
        /// </remarks>
        public static IEnumerable<string> QueryDosDevice(string deviceName)
        {
            if (deviceName != null) deviceName = Paths.TrimTrailingSeparators(deviceName);

            // Null will return everything defined- this list is quite large so set a higher initial allocation

            var buffer = StringBuffer.Cache.Acquire(deviceName == null ? 16384u : 256);

            try
            {
                uint result = 0;

                // QueryDosDevicePrivate takes the buffer count in TCHARs, which is 2 bytes for Unicode (WCHAR)
                while ((result = Imports.QueryDosDeviceW(deviceName, buffer, buffer.CharCapacity)) == 0)
                {
                    WindowsError error = Error.GetLastError();
                    switch (error)
                    {
                        case WindowsError.ERROR_INSUFFICIENT_BUFFER:
                            buffer.EnsureCharCapacity(buffer.CharCapacity * 2);
                            break;
                        default:
                            throw error.GetException(deviceName);
                    }
                }

                // This API returns a szArray, which is terminated by two nulls
                buffer.Length = checked(result - 2);
                return buffer.Split('\0');
            }
            finally
            {
                StringBufferCache.Instance.Release(buffer);
            }
        }

        /// <summary>
        /// Gets a set of strings for the defined logical drives in the system.
        /// </summary>
        public static IEnumerable<string> GetLogicalDriveStrings()
        {
            return BufferHelper.BufferInvoke((StringBuffer buffer) =>
            {
                uint result = 0;

                // GetLogicalDriveStringsPrivate takes the buffer count in TCHARs, which is 2 bytes for Unicode (WCHAR)
                while ((result = Imports.GetLogicalDriveStringsW(buffer.CharCapacity, buffer)) > buffer.CharCapacity)
                {
                    buffer.EnsureCharCapacity(result);
                }

                if (result == 0)
                    throw Error.GetExceptionForLastError();

                buffer.Length = result;
                return buffer.Split('\0', removeEmptyStrings: true);
            });
        }

        /// <summary>
        /// Returns the path of the volume mount point for the specified path.
        /// </summary>
        public static string GetVolumePathName(string path)
        {
            return BufferHelper.BufferInvoke((StringBuffer buffer) =>
            {
                while (!Imports.GetVolumePathNameW(path, buffer, buffer.CharCapacity))
                {
                    WindowsError error = Error.GetLastError();
                    switch (error)
                    {
                        case WindowsError.ERROR_FILENAME_EXCED_RANGE:
                            buffer.EnsureCharCapacity(buffer.CharCapacity * 2);
                            break;
                        default:
                            throw error.GetException(path);
                    }
                }

                buffer.SetLengthToFirstNull();
                return buffer.ToString();
            });
        }

        /// <summary>
        /// Returns the list of all paths where the given volume name is mounted.
        /// </summary>
        /// <param name="volumeName">A volume GUID path for the volume (\\?\Volume{xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx}\).</param>
        public static IEnumerable<string> GetVolumePathNamesForVolumeName(string volumeName)
        {
            return BufferHelper.BufferInvoke((StringBuffer buffer) =>
            {
                uint returnLength = 0;

                // GetLogicalDriveStringsPrivate takes the buffer count in TCHARs, which is 2 bytes for Unicode (WCHAR)
                while (!Imports.GetVolumePathNamesForVolumeNameW(volumeName, buffer, buffer.CharCapacity, ref returnLength))
                {
                    WindowsError error = Error.GetLastError();
                    switch (error)
                    {
                        case WindowsError.ERROR_MORE_DATA:
                            buffer.EnsureCharCapacity(returnLength);
                            break;
                        default:
                            throw error.GetException(volumeName);
                    }
                }

                Debug.Assert(returnLength != 2, "this should never happen can't have a string array without at least 3 chars");

                // If the return length is 1 there were no mount points. The buffer should be '\0'.
                if (returnLength < 3)
                    return Enumerable.Empty<string>();

                // The return length will be the entire length of the buffer, including the final string's
                // null and the string list's second null. Example: "Foo\0Bar\0\0" would be 9.
                buffer.Length = returnLength - 2;
                return buffer.Split('\0');
            });
        }

        /// <summary>
        /// Gets the GUID format (\\?\Volume{xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx}\) of the given volume mount point path.
        /// </summary>
        public static string GetVolumeNameForVolumeMountPoint(string volumeMountPoint)
        {
            volumeMountPoint = Paths.AddTrailingSeparator(volumeMountPoint);

            return BufferHelper.BufferInvoke((StringBuffer buffer) =>
            {
                // MSDN claims 50 is "reasonable", let's go double.
                buffer.EnsureCharCapacity(100);

                Error.ThrowLastErrorIfFalse(Imports.GetVolumeNameForVolumeMountPointW(volumeMountPoint, buffer, buffer.CharCapacity));

                buffer.SetLengthToFirstNull();
                return buffer.ToString();
            });
        }

        /// <summary>
        /// Get all volume names.
        /// </summary>
        public static IEnumerable<string> GetVolumes() => new VolumeNamesEnumerable();

        /// <summary>
        /// Get all of the folder mount points for the given volume. Requires admin access.
        /// </summary>
        /// <remarks>
        /// This API seems busted/flaky. Use GetVolumePathNamesForVolumeName() instead to
        /// get all mount points (folders *and* drive letter mounts) without requiring admin
        /// access.
        /// </remarks>
        /// <param name="volumeName">Volume name in the form "\\?\Volume{guid}\"</param>
        public static IEnumerable<string> GetVolumeMountPoints(string volumeName) => new VolumeMountPointsEnumerable(volumeName);

        public static IEnumerable<BackupStreamInformation> GetAlternateStreamInformation(string path)
        {
            List<BackupStreamInformation> streams = new List<BackupStreamInformation>();
            using (var fileHandle = CreateFile(
                path: path.AsSpan(),
                // To look at metadata we don't need read or write access
                desiredAccess: 0,
                shareMode: ShareModes.ReadWrite,
                creationDisposition: CreationDisposition.OpenExisting,
                fileAttributes: FileAttributes.None,
                fileFlags: FileFlags.BackupSemantics))
            {
                using (BackupReader reader = new BackupReader(fileHandle))
                {
                    BackupStreamInformation? info;
                    while ((info = reader.GetNextInfo()).HasValue)
                    {
                        if (info.Value.StreamType == BackupStreamType.AlternateData)
                        {
                            streams.Add(new BackupStreamInformation { Name = info.Value.Name, Size = info.Value.Size });
                        }
                    }
                }
            }

            return streams;
        }

        public static void EncryptFile(string path)
            => Error.ThrowLastErrorIfFalse(Imports.EncryptFileW(path), path);

        public static void DecryptFile(string path)
            => Error.ThrowLastErrorIfFalse(Imports.DecryptFileW(path, 0), path);

        public unsafe static IEnumerable<SID> QueryUsersOnEncryptedFile(string path)
        {
            ENCRYPTION_CERTIFICATE_HASH_LIST* hashList;
            Imports.QueryUsersOnEncryptedFile(path, &hashList).ThrowIfFailed(path);
            if (hashList->nCert_Hash == 0)
            {
                Imports.FreeEncryptionCertificateHashList(hashList);
                return Enumerable.Empty<SID>();
            }

            ENCRYPTION_CERTIFICATE_HASH* users = *hashList->pUsers;
            SID[] sids = new SID[hashList->nCert_Hash];
            for (int i = 0; i < sids.Length; i++)
            {
                sids[0] = new SID(users->pUserSid);
                users++;
            }

            Imports.FreeEncryptionCertificateHashList(hashList);
            return sids;
        }

        public unsafe static bool RemoveUser(in SID sid, string path)
        {
            ENCRYPTION_CERTIFICATE_HASH_LIST* hashList;
            Imports.QueryUsersOnEncryptedFile(path, &hashList).ThrowIfFailed(path);

            try
            {
                ENCRYPTION_CERTIFICATE_HASH* users = *hashList->pUsers;
                for (int i = 0; i < hashList->nCert_Hash; i++)
                {
                    if (sid.Equals(users->pUserSid))
                    {
                        var removeList = new ENCRYPTION_CERTIFICATE_HASH_LIST
                        {
                            nCert_Hash = 1,
                            pUsers = &users
                        };

                        Imports.RemoveUsersFromEncryptedFile(path, &removeList).ThrowIfFailed(path);
                        return true;
                    }
                    users++;
                }

                return false;
            }
            finally
            {
                Imports.FreeEncryptionCertificateHashList(hashList);
            }
        }
    }
}
