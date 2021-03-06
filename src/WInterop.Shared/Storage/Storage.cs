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
using WInterop.Security;
using WInterop.Errors;
using WInterop.Storage.BufferWrappers;
using WInterop.Storage.Unsafe;
using WInterop.Support;
using WInterop.Support.Buffers;
using WInterop.Synchronization;

namespace WInterop.Storage
{
    public static partial class Storage
    {
        // Asynchronous Disk I/O Appears as Synchronous on Windows
        // https://support.microsoft.com/en-us/kb/156932
        //
        // NTFS File Attributes
        // https://blogs.technet.microsoft.com/askcore/2010/08/25/ntfs-file-attributes/

        /// <summary>
        /// Get the temporary directory path.
        /// </summary>
        public static string GetTempPath()
        {
            var wrapper = new TempPathWrapper();
            return BufferHelper.ApiInvoke(ref wrapper);
        }

        /// <summary>
        /// Get the fully resolved path name.
        /// </summary>
        public static string GetFullPathName(string path)
        {
            var wrapper = new FullPathNameWrapper { Path = path };
            return BufferHelper.ApiInvoke(ref wrapper, path);
        }

        private struct FinalPathNameByHandleWrapper : IBufferFunc<StringBuffer, uint>
        {
            public SafeFileHandle FileHandle;
            public GetFinalPathNameByHandleFlags Flags;

            uint IBufferFunc<StringBuffer, uint>.Func(StringBuffer buffer)
            {
                return Imports.GetFinalPathNameByHandleW(FileHandle, buffer, buffer.CharCapacity, Flags);
            }
        }

        /// <summary>
        /// Gets a canonical version of the given handle's path.
        /// </summary>
        public static string GetFinalPathNameByHandle(
            SafeFileHandle fileHandle,
            GetFinalPathNameByHandleFlags flags = GetFinalPathNameByHandleFlags.FileNameNormalized | GetFinalPathNameByHandleFlags.VolumeNameDos)
        {
            var wrapper = new FinalPathNameByHandleWrapper { FileHandle = fileHandle, Flags = flags };
            return BufferHelper.ApiInvoke(ref wrapper);
        }

        /// <summary>
        /// Gets a canonical version of the given file's path.
        /// </summary>
        /// <param name="resolveLinks">True to get the destination of links rather than the link itself.</param>
        public static string GetFinalPathName(StringSpan path, GetFinalPathNameByHandleFlags finalPathFlags, bool resolveLinks)
        {
            if (path.IsEmpty) return null;

            // BackupSemantics is needed to get directory handles
            FileFlags flags = FileFlags.BackupSemantics;
            if (!resolveLinks) flags |= FileFlags.OpenReparsePoint;

            using (SafeFileHandle fileHandle = CreateFile(path, CreationDisposition.OpenExisting, 0, ShareModes.ReadWrite,
                FileAttributes.None, flags))
            {
                return GetFinalPathNameByHandle(fileHandle, finalPathFlags);
            }
        }

        private struct LongPathNameWrapper : IBufferFunc<StringBuffer, uint>
        {
            public string Path;

            uint IBufferFunc<StringBuffer, uint>.Func(StringBuffer buffer)
            {
                return Imports.GetLongPathNameW(Path, buffer, buffer.CharCapacity);
            }
        }

        /// <summary>
        /// Get the long (non 8.3) path version of the given path.
        /// </summary>
        public static string GetLongPathName(string path)
        {
            var wrapper = new LongPathNameWrapper { Path = path };
            return BufferHelper.ApiInvoke(ref wrapper, path);
        }

        /// <summary>
        /// Get a temporary file name. Creates a 0 length file.
        /// </summary>
        /// <param name="path">The directory for the file.</param>
        /// <param name="prefix">Three character prefix for the filename.</param>
        public static string GetTempFileName(string path, string prefix)
        {
            return BufferHelper.BufferInvoke((StringBuffer buffer) =>
            {
                buffer.EnsureCharCapacity(Paths.MaxPath);
                uint result = Imports.GetTempFileNameW(
                    lpPathName: path,
                    lpPrefixString: prefix,
                    uUnique: 0,
                    lpTempFileName: buffer);

                if (result == 0) throw Errors.Error.GetExceptionForLastError(path);

                buffer.SetLengthToFirstNull();
                return buffer.ToString();
            });
        }

        /// <summary>
        /// Delete the given file.
        /// </summary>
        public static void DeleteFile(string path)
        {
            if (!Imports.DeleteFileW(path))
                throw Errors.Error.GetExceptionForLastError(path);
        }

        /// <summary>
        /// Wrapper that allows getting a file stream using System.IO defines.
        /// </summary>
        public static System.IO.Stream CreateFileStream(
            StringSpan path,
            System.IO.FileAccess fileAccess,
            System.IO.FileShare fileShare,
            System.IO.FileMode fileMode,
            System.IO.FileAttributes fileAttributes = 0,
            FileFlags fileFlags = FileFlags.None,
            SecurityQosFlags securityFlags = SecurityQosFlags.None)
        {
            return CreateFileStream(
                path: path,
                desiredAccess: Conversion.FileAccessToDesiredAccess(fileAccess),
                shareMode: Conversion.FileShareToShareMode(fileShare),
                creationDisposition: Conversion.FileModeToCreationDisposition(fileMode),
                fileAttributes: (FileAttributes)fileAttributes,
                fileFlags: fileFlags,
                securityQosFlags: securityFlags);
        }

        /// <summary>
        /// Get a stream for the specified file.
        /// </summary>
        public static System.IO.Stream CreateFileStream(
            StringSpan path,
            DesiredAccess desiredAccess,
            ShareModes shareMode,
            CreationDisposition creationDisposition,
            FileAttributes fileAttributes = FileAttributes.None,
            FileFlags fileFlags = FileFlags.None,
            SecurityQosFlags securityQosFlags = SecurityQosFlags.None)
        {
            var fileHandle = CreateFile(path, creationDisposition, desiredAccess, shareMode, fileAttributes, fileFlags, securityQosFlags);

            // FileStream will own the lifetime of the handle
            return new System.IO.FileStream(
                handle: fileHandle,
                access: Conversion.DesiredAccessToFileAccess(desiredAccess),
                bufferSize: 4096,
                isAsync: (fileFlags & FileFlags.Overlapped) != 0);
        }

        private delegate SafeFileHandle CreateFileDelegate(
            StringSpan path,
            DesiredAccess desiredAccess,
            ShareModes shareMode,
            CreationDisposition creationDisposition,
            FileAttributes fileAttributes,
            FileFlags fileFlags,
            SecurityQosFlags securityQosFlags);

        private static CreateFileDelegate s_createFileDelegate;

        /// <summary>
        /// Wrapper that allows using System.IO defines where available. Calls CreateFile2 if available.
        /// </summary>
        public static SafeFileHandle CreateFileSystemIo(
            StringSpan path,
            System.IO.FileAccess fileAccess,
            System.IO.FileShare fileShare,
            System.IO.FileMode fileMode,
            System.IO.FileAttributes fileAttributes = 0,
            FileFlags fileFlags = FileFlags.None,
            SecurityQosFlags securityFlags = SecurityQosFlags.None)
        {
            return CreateFile(
                path: path,
                desiredAccess: Conversion.FileAccessToDesiredAccess(fileAccess),
                shareMode: Conversion.FileShareToShareMode(fileShare),
                creationDisposition: Conversion.FileModeToCreationDisposition(fileMode),
                fileAttributes: (FileAttributes)fileAttributes,
                fileFlags: fileFlags,
                securityQosFlags: securityFlags);
        }

        /// <summary>
        /// CreateFile wrapper that attempts to use CreateFile2 if running as Windows Store app.
        /// </summary>
        public static SafeFileHandle CreateFile(
            StringSpan path,
            CreationDisposition creationDisposition,
            DesiredAccess desiredAccess = DesiredAccess.GenericReadWrite,
            ShareModes shareMode = ShareModes.ReadWrite,
            FileAttributes fileAttributes = FileAttributes.None,
            FileFlags fileFlags = FileFlags.None,
            SecurityQosFlags securityQosFlags = SecurityQosFlags.None)
        {
            // Prefer CreateFile2, falling back to CreateFileEx if we can
            if (s_createFileDelegate == null)
            {
                s_createFileDelegate = CreateFile2;
                try
                {
                    return s_createFileDelegate(path, desiredAccess, shareMode, creationDisposition, fileAttributes, fileFlags, securityQosFlags);
                }
                catch (EntryPointNotFoundException)
                {
                    s_createFileDelegate = Delegates.CreateDelegate<CreateFileDelegate>(
                        "WInterop.Storage.Desktop.NativeMethods, " + Delegates.DesktopLibrary,
                        "CreateFileW");
                }
            }

            return s_createFileDelegate(path, desiredAccess, shareMode, creationDisposition, fileAttributes, fileFlags, securityQosFlags);
        }

        /// <summary>
        /// CreateFile2 wrapper. Only available on Windows 8 and above.
        /// </summary>
        public static unsafe SafeFileHandle CreateFile2(
            StringSpan path,
            DesiredAccess desiredAccess,
            ShareModes shareMode,
            CreationDisposition creationDisposition,
            FileAttributes fileAttributes = FileAttributes.None,
            FileFlags fileFlags = FileFlags.None,
            SecurityQosFlags securityQosFlags = SecurityQosFlags.None)
        {
            CREATEFILE2_EXTENDED_PARAMETERS extended = new CREATEFILE2_EXTENDED_PARAMETERS()
            {
                dwSize = (uint)sizeof(CREATEFILE2_EXTENDED_PARAMETERS),
                dwFileAttributes = fileAttributes,
                dwFileFlags = fileFlags,
                dwSecurityQosFlags = securityQosFlags
            };

            SafeFileHandle handle = Imports.CreateFile2(
                lpFileName: ref MemoryMarshal.GetReference(path.GetSpanWithoutTerminator()),
                dwDesiredAccess: desiredAccess,
                dwShareMode: shareMode,
                dwCreationDisposition: creationDisposition,
                pCreateExParams: ref extended);

            if (handle.IsInvalid)
                throw Error.GetExceptionForLastError(path.ToString());

            return handle;
        }

        private delegate void CopyFileDelegate(
            string source,
            string destination,
            bool overwrite);

        private static CopyFileDelegate s_copyFileDelegate;

        /// <summary>
        /// CopyFile wrapper that attempts to use CopyFile2 if running as Windows Store app.
        /// </summary>
        public static void CopyFile(string source, string destination, bool overwrite = false)
        {
            // Prefer CreateFile2, falling back to CreateFileEx if we can
            if (s_copyFileDelegate == null)
            {
                s_copyFileDelegate = CopyFile2;
                try
                {
                    s_copyFileDelegate(source, destination, overwrite);
                    return;
                }
                catch (EntryPointNotFoundException)
                {
                    s_copyFileDelegate = Delegates.CreateDelegate<CopyFileDelegate>(
                        "WInterop.Storage.Desktop.NativeMethods, " + Delegates.DesktopLibrary,
                        "CopyFileEx");
                }
            }

            s_copyFileDelegate(source, destination, overwrite);
        }

        /// <summary>
        /// CopyFile2 wrapper. Only available on Windows8 and above.
        /// </summary>
        public unsafe static void CopyFile2(string source, string destination, bool overwrite = false)
        {
            Boolean32 cancel = false;
            COPYFILE2_EXTENDED_PARAMETERS parameters = new COPYFILE2_EXTENDED_PARAMETERS()
            {
                dwSize = (uint)sizeof(COPYFILE2_EXTENDED_PARAMETERS),
                pfCancel = &cancel,
                dwCopyFlags = overwrite ? 0 : CopyFileFlags.FailIfExists
            };

            Imports.CopyFile2(source, destination, ref parameters).ThrowIfFailed();
        }

        /// <summary>
        /// Creates a wrapper for finding files.
        /// </summary>
        /// <param name="directory">The directory to search in.</param>
        /// <param name="recursive">True to find files recursively.</param>
        /// <param name="nameFilter">
        /// The filename filter. Can contain wildcards, full details can be found at
        /// <a href="https://msdn.microsoft.com/en-us/library/ff469270.aspx">[MS-FSA] 2.1.4.4 Algorithm for Determining if a FileName Is in an Expression</a>.
        /// If custom <paramref name="findFilter"/> is specified this parameter is ignored.
        /// </param>
        /// <param name="findFilter">Custom filter, if default behavior isn't desired. (Which is no "." or "..", and use the filter string.)</param>
        /// <param name="findTransform">Custom transform, if the default transform isn't desired.</param>
        public static FindOperation<T> CreateFindOperation<T>(
            string directory,
            string nameFilter = "*",
            bool recursive = false,
            IFindTransform<T> findTransform = null,
            IFindFilter findFilter = null)
        {
            return new FindOperation<T>(directory, nameFilter, recursive, findTransform, findFilter);
        }

        /// <summary>
        /// Gets file attributes for the given path.
        /// </summary>
        public static FileAttributes GetFileAttributes(string path)
        {
            FileAttributes attributes = Imports.GetFileAttributesW(path);
            if (attributes == FileAttributes.Invalid)
                throw Error.GetExceptionForLastError(path);

            return attributes;
        }

        /// <summary>
        /// Gets the file attributes for the given path.
        /// </summary>
        public static Win32FileAttributeData GetFileAttributesExtended(string path)
        {
            if (!Imports.GetFileAttributesExW(path, GetFileExtendedInformationLevels.Standard, out Win32FileAttributeData data))
                throw Error.GetExceptionForLastError(path);

            return data;
        }

        /// <summary>
        /// Simple wrapper to check if a given path exists.
        /// </summary>
        /// <exception cref="UnauthorizedAccessException">Thrown if there aren't rights to get attributes on the given path.</exception>
        public static bool PathExists(string path)
        {
            return TryGetFileInfo(path).HasValue;
        }

        /// <summary>
        /// Simple wrapper to check if a given path exists and is a file.
        /// </summary>
        /// <exception cref="UnauthorizedAccessException">Thrown if there aren't rights to get attributes on the given path.</exception>
        public static bool FileExists(string path)
        {
            var data = TryGetFileInfo(path);
            return data.HasValue && (data.Value.FileAttributes & FileAttributes.Directory) != FileAttributes.Directory;
        }

        /// <summary>
        /// Simple wrapper to check if a given path exists and is a directory.
        /// </summary>
        /// <exception cref="UnauthorizedAccessException">Thrown if there aren't rights to get attributes on the given path.</exception>
        public static bool DirectoryExists(string path)
        {
            var data = TryGetFileInfo(path);
            return data.HasValue && (data.Value.FileAttributes & FileAttributes.Directory) == FileAttributes.Directory;
        }

        /// <summary>
        /// Tries to get file info, returns null if the given path doesn't exist.
        /// </summary>
        /// <exception cref="UnauthorizedAccessException">Thrown if there aren't rights to get attributes on the given path.</exception>
        public static Win32FileAttributeData? TryGetFileInfo(string path)
        {
            if (!Imports.GetFileAttributesExW(path, GetFileExtendedInformationLevels.Standard, out Win32FileAttributeData data))
            {
                WindowsError error = Error.GetLastError();
                switch (error)
                {
                    case WindowsError.ERROR_ACCESS_DENIED:
                    case WindowsError.ERROR_NETWORK_ACCESS_DENIED:
                        throw error.GetException(path);
                    case WindowsError.ERROR_PATH_NOT_FOUND:
                    default:
                        return null;
                }
            }

            return data;
        }

        /// <summary>
        /// Sets the file attributes for the given path.
        /// </summary>
        public static void SetFileAttributes(string path, FileAttributes attributes)
        {
            if (!Imports.SetFileAttributesW(path, attributes))
                throw Error.GetExceptionForLastError(path);
        }

        /// <summary>
        /// Flush file buffers.
        /// </summary>
        public static void FlushFileBuffers(SafeFileHandle fileHandle)
        {
            if (!Imports.FlushFileBuffers(fileHandle))
                throw Error.GetExceptionForLastError();
        }

        /// <summary>
        /// Gets the file name from the given handle. This uses GetFileInformationByHandleEx, which does not give back the volume
        /// name for the path- but is available from Windows Store apps.
        /// </summary>
        /// <remarks>
        /// The exact data that is returned is somewhat complicated and is described in the documentation for ZwQueryInformationFile.
        /// </remarks>
        public unsafe static string GetFileNameLegacy(SafeFileHandle fileHandle)
        {
            return BufferHelper.BufferInvoke((HeapBuffer buffer) =>
            {
                unsafe
                {
                    while (!Imports.GetFileInformationByHandleEx(
                        fileHandle,
                        FileInfoClass.FileNameInfo,
                        buffer.VoidPointer,
                        checked((uint)buffer.ByteCapacity)))
                    {
                        Error.ThrowIfLastErrorNot(WindowsError.ERROR_MORE_DATA);
                        buffer.EnsureByteCapacity(buffer.ByteCapacity * 2);
                    }
                }

                return (((FILE_NAME_INFORMATION*)buffer.VoidPointer)->FileName).CreateString();
            });
        }

        /// <summary>
        /// Get standard file info from the given file handle.
        /// </summary>
        public static FileStandardInformation GetFileStandardInformation(SafeFileHandle fileHandle)
        {
            FileStandardInformation info;
            unsafe
            {
                if (!Imports.GetFileInformationByHandleEx(
                    fileHandle,
                    FileInfoClass.FileStandardInfo,
                    &info,
                    (uint)sizeof(FileStandardInformation)))
                    throw Error.GetExceptionForLastError();
            }

            return info;
        }

        /// <summary>
        /// Get basic file info from the given file handle.
        /// </summary>
        public static FileBasicInformation GetFileBasicInformation(SafeFileHandle fileHandle)
        {
            FileBasicInformation info;
            unsafe
            {
                if (!Imports.GetFileInformationByHandleEx(
                    fileHandle,
                    FileInfoClass.FileBasicInfo,
                    &info,
                    (uint)sizeof(FileBasicInformation)))
                    throw Error.GetExceptionForLastError();
            }
            return info;
        }

        /// <summary>
        /// Get the list of data streams for the given handle.
        /// </summary>
        public unsafe static IEnumerable<StreamInformation> GetStreamInformation(SafeFileHandle fileHandle)
        {
            return BufferHelper.BufferInvoke((HeapBuffer buffer) =>
            {
                unsafe
                {
                    while (!Imports.GetFileInformationByHandleEx(fileHandle, FileInfoClass.FileStreamInfo,
                        buffer.VoidPointer, checked((uint)buffer.ByteCapacity)))
                    {
                        WindowsError error = Error.GetLastError();
                        switch (error)
                        {
                            case WindowsError.ERROR_HANDLE_EOF:
                                // No streams
                                return Enumerable.Empty<StreamInformation>();
                            case WindowsError.ERROR_MORE_DATA:
                                buffer.EnsureByteCapacity(buffer.ByteCapacity * 2);
                                break;
                            default:
                                throw error.GetException();
                        }
                    }
                }

                return StreamInformation.Create((FILE_STREAM_INFORMATION*)buffer.VoidPointer);
            });
        }

        /// <summary>
        /// Synchronous wrapper for ReadFile.
        /// </summary>
        /// <param name="fileHandle">Handle to the file to read.</param>
        /// <param name="buffer">Buffer to read bytes into.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="fileHandle"/> is null.</exception>
        /// <returns>The number of bytes read.</returns>
        public unsafe static uint ReadFile(SafeFileHandle fileHandle, Span<byte> buffer, ulong? fileOffset = null)
        {
            if (fileHandle == null) throw new ArgumentNullException("fileHandle");
            if (fileHandle.IsClosed | fileHandle.IsInvalid) throw new ArgumentException("fileHandle");

            uint numberOfBytesRead;

            if (fileOffset.HasValue)
            {
                OVERLAPPED overlapped = new OVERLAPPED { Offset = fileOffset.Value };
                if (!Imports.ReadFile(fileHandle, ref MemoryMarshal.GetReference(buffer), (uint)buffer.Length, out numberOfBytesRead, &overlapped))
                    throw Error.GetExceptionForLastError();
            }
            else
            {
                if (!Imports.ReadFile(fileHandle, ref MemoryMarshal.GetReference(buffer), (uint)buffer.Length, out numberOfBytesRead, null))
                    throw Error.GetExceptionForLastError();
            }

            return numberOfBytesRead;
        }

        /// <summary>
        /// Synchronous wrapper for WriteFile.
        /// </summary>
        /// <param name="fileHandle">Handle to the file to write.</param>
        /// <param name="data">Data to write.</param>
        /// <param name="fileOffset">Offset into the file, or null for current.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="fileHandle"/> is null.</exception>
        /// <returns>The number of bytes written.</returns>
        public unsafe static uint WriteFile(SafeFileHandle fileHandle, Span<byte> data, ulong? fileOffset = null)
        {
            if (fileHandle == null) throw new ArgumentNullException("fileHandle");
            if (fileHandle.IsClosed | fileHandle.IsInvalid) throw new ArgumentException("fileHandle");

            uint numberOfBytesWritten;

            if (fileOffset.HasValue)
            {
                OVERLAPPED overlapped = new OVERLAPPED { Offset = fileOffset.Value };
                if (!Imports.WriteFile(fileHandle, ref MemoryMarshal.GetReference(data), (uint)data.Length, out numberOfBytesWritten, &overlapped))
                    throw Error.GetExceptionForLastError();
            }
            else
            {
                if (!Imports.WriteFile(fileHandle, ref MemoryMarshal.GetReference(data), (uint)data.Length, out numberOfBytesWritten, null))
                    throw Error.GetExceptionForLastError();
            }

            return numberOfBytesWritten;
        }

        /// <summary>
        /// Set the file pointer position for the given file.
        /// </summary>
        /// <param name="distance">Offset.</param>
        /// <param name="moveMethod">Start position.</param>
        /// <returns>The new pointer position.</returns>
        public static long SetFilePointer(SafeFileHandle fileHandle, long distance, MoveMethod moveMethod)
        {
            if (!Imports.SetFilePointerEx(fileHandle, distance, out long position, moveMethod))
                throw Error.GetExceptionForLastError();

            return position;
        }

        /// <summary>
        /// Get the position of the pointer for the given file.
        /// </summary>
        public static long GetFilePointer(SafeFileHandle fileHandle)
        {
            return SetFilePointer(fileHandle, 0, MoveMethod.Current);
        }

        /// <summary>
        /// Get the size of the given file.
        /// </summary>
        public static long GetFileSize(SafeFileHandle fileHandle)
        {
            if (!Imports.GetFileSizeEx(fileHandle, out long size))
                throw Error.GetExceptionForLastError();

            return size;
        }

        /// <summary>
        /// Get the type of the given file handle.
        /// </summary>
        public static FileType GetFileType(SafeFileHandle fileHandle)
        {
            FileType fileType = Imports.GetFileType(fileHandle);
            if (fileType == FileType.Unknown)
                Error.ThrowIfLastErrorNot(WindowsError.NO_ERROR);

            return fileType;
        }

        /// <summary>
        /// Gets the filenames in the specified directory. Includes "." and "..".
        /// </summary>
        public unsafe static IEnumerable<string> GetDirectoryFilenames(SafeFileHandle directoryHandle)
        {
            List<string> filenames = new List<string>();
            GetFullDirectoryInfoHelper(directoryHandle, buffer =>
            {
                FILE_FULL_DIR_INFORMATION* info = (FILE_FULL_DIR_INFORMATION*)buffer.BytePointer;
                do
                {
                    filenames.Add(info->FileName.CreateString());
                    info = FILE_FULL_DIR_INFORMATION.GetNextInfo(info);
                } while (info != null);
            });
            return filenames;
        }

        /// <summary>
        /// Gets all of the info for files within the given directory handle.
        /// </summary>
        public unsafe static IEnumerable<FullFileInformation> GetDirectoryInformation(SafeFileHandle directoryHandle)
        {
            List<FullFileInformation> infos = new List<FullFileInformation>();
            GetFullDirectoryInfoHelper(directoryHandle, buffer =>
            {
                FILE_FULL_DIR_INFORMATION* info = (FILE_FULL_DIR_INFORMATION*)buffer.BytePointer;
                do
                {
                    infos.Add(new FullFileInformation(info));
                    info = FILE_FULL_DIR_INFORMATION.GetNextInfo(info);
                } while (info != null);
            });
            return infos;
        }

        private unsafe static void GetFullDirectoryInfoHelper(SafeFileHandle directoryHandle, Action<HeapBuffer> action)
        {
            BufferHelper.BufferInvoke((HeapBuffer buffer) =>
            {
                // Make sure we have at least enough for the normal "." and ".."
                buffer.EnsureByteCapacity((ulong)sizeof(FILE_FULL_DIR_INFORMATION) * 2);

                do
                {
                    while (!Imports.GetFileInformationByHandleEx(
                        directoryHandle,
                        FileInfoClass.FileFullDirectoryInfo,
                        buffer.VoidPointer,
                        (uint)buffer.ByteCapacity))
                    {
                        var error = Error.GetLastError();
                        switch (error)
                        {
                            case WindowsError.ERROR_BAD_LENGTH:
                                // Not enough space for the struct data
                                Debug.Fail("Should have properly set a minimum buffer");
                                goto case WindowsError.ERROR_MORE_DATA;
                            case WindowsError.ERROR_MORE_DATA:
                                // Buffer isn't big enough for a filename
                                buffer.EnsureByteCapacity(buffer.ByteCapacity + 512);
                                break;
                            case WindowsError.ERROR_NO_MORE_FILES:
                                // Nothing left to get
                                return;
                            default:
                                throw error.GetException();
                        }
                    }

                    action(buffer);
                } while (true);
            });
        }

        /// <summary>
        /// Returns true if the given tag is owned by Microsoft.
        /// </summary>
        public static bool IsReparseTagMicrosoft(ReparseTag reparseTag)
        {
            // https://msdn.microsoft.com/en-us/library/windows/hardware/ff549452.aspx
            return ((uint)reparseTag & 0x80000000) != 0;
        }

        /// <summary>
        /// Returns true if the given tag is a name surrogate.
        /// </summary>
        public static bool IsReparseTagNameSurrogate(ReparseTag reparseTag)
        {
            // https://msdn.microsoft.com/en-us/library/windows/hardware/ff549462.aspx
            return ((uint)reparseTag & 0x20000000) != 0;
        }

        /// <summary>
        /// Returns true if the reparse point can have children.
        /// </summary>
        public static bool IsReparseTagDirectory(ReparseTag reparseTag)
        {
            return ((uint)reparseTag & 0x10000000) != 0;
        }

        /// <summary>
        /// Get the owner SID for the given handle.
        /// </summary>
        public unsafe static SID GetOwner(this SafeFileHandle handle)
            => Security.Security.GetOwner(handle, ObjectType.File);

        /// <summary>
        /// Get the primary group SID for the given handle.
        /// </summary>
        public unsafe static SID GetPrimaryGroup(this SafeFileHandle handle)
            => Security.Security.GetPrimaryGroup(handle, ObjectType.File);

        /// <summary>
        /// Get the discretionary access control list for the given handle.
        /// </summary>
        public unsafe static SecurityDescriptor GetAccessControlList(this SafeFileHandle handle)
            => Security.Security.GetAccessControlList(handle, ObjectType.File);

        public unsafe static void ChangeAccess(
            this SafeFileHandle handle,
            in SID sid,
            FileAccessRights rights,
            AccessMode access,
            Inheritance inheritance = Inheritance.NoInheritance)
        {
            using (SecurityDescriptor sd = GetAccessControlList(handle))
            {
                sd.SetAclAccess(in sid, rights, access, inheritance);
            }
        }

        /// <summary>
        /// Remove the given directory.
        /// </summary>
        public static void RemoveDirectory(string path)
        {
            if (!Imports.RemoveDirectoryW(path))
                throw Error.GetExceptionForLastError(path);
        }

        /// <summary>
        /// Create the given directory.
        /// </summary>
        public static void CreateDirectory(string path)
        {
            if (!Imports.CreateDirectoryW(path, IntPtr.Zero))
                throw Error.GetExceptionForLastError(path);
        }

        /// <summary>
        /// Simple wrapper to allow creating a file handle for an existing directory.
        /// </summary>
        public static SafeFileHandle CreateDirectoryHandle(StringSpan directoryPath, DesiredAccess desiredAccess = DesiredAccess.ListDirectory)
        {
            return CreateFile(
                directoryPath,
                CreationDisposition.OpenExisting,
                desiredAccess,
                ShareModes.ReadWrite | ShareModes.Delete,
                FileAttributes.None,
                FileFlags.BackupSemantics);
        }

        private struct GetCurrentDirectoryWrapper : IBufferFunc<StringBuffer, uint>
        {
            uint IBufferFunc<StringBuffer, uint>.Func(StringBuffer buffer)
            {
                return Imports.GetCurrentDirectoryW(buffer.CharCapacity, buffer);
            }
        }

        /// <summary>
        /// Get the current directory.
        /// </summary>
        public static string GetCurrentDirectory()
        {
            var wrapper = new GetCurrentDirectoryWrapper();
            return BufferHelper.ApiInvoke(ref wrapper);
        }

        /// <summary>
        /// Set the current directory.
        /// </summary>
        public static void SetCurrentDirectory(StringSpan path)
        {
            if (!Imports.SetCurrentDirectoryW(ref MemoryMarshal.GetReference(path.GetSpanWithoutTerminator())))
                throw Error.GetExceptionForLastError(path.ToString());
        }

        /// <summary>
        /// Get the drive type for the given root path.
        /// </summary>
        public static DriveType GetDriveType(string rootPath)
        {
            if (rootPath != null) rootPath = Paths.AddTrailingSeparator(rootPath);
            return Imports.GetDriveTypeW(rootPath);
        }

        /// <summary>
        /// Gets volume information for the given volume root path.
        /// </summary>
        public static VolumeInformation GetVolumeInformation(string rootPath)
        {
            rootPath = Paths.AddTrailingSeparator(rootPath);

            using (var volumeName = new StringBuffer(initialCharCapacity: Paths.MaxPath + 1))
            using (var fileSystemName = new StringBuffer(initialCharCapacity: Paths.MaxPath + 1))
            {
                // Documentation claims that removable (floppy/optical) drives will prompt for media when calling this API and say to
                // set the error mode to prevent it. I can't replicate this behavior or find any documentation on when it might have
                // changed. I'm guessing this changed in Windows 7 when they added support for setting the thread's error mode (as
                // opposed to the entire process).
                if (!Imports.GetVolumeInformationW(
                    rootPath,
                    volumeName,
                    volumeName.CharCapacity,
                    out uint serialNumber,
                    out uint maxComponentLength,
                    out FileSystemFeature flags,
                    fileSystemName,
                    fileSystemName.CharCapacity))
                    throw Error.GetExceptionForLastError(rootPath);

                volumeName.SetLengthToFirstNull();
                fileSystemName.SetLengthToFirstNull();

                return new VolumeInformation
                {
                    RootPathName = rootPath,
                    VolumeName = volumeName.ToString(),
                    VolumeSerialNumber = serialNumber,
                    MaximumComponentLength = maxComponentLength,
                    FileSystemFlags = flags,
                    FileSystemName = fileSystemName.ToString()
                };
            }
        }

        public static DiskFreeSpace GetDiskFreeSpace(string directory)
        {
            DiskFreeSpace freeSpace;

            unsafe
            {
                if (!Imports.GetDiskFreeSpaceW(
                    lpRootPathName: directory,
                    lpSectorsPerCluster: &freeSpace.SectorsPerCluster,
                    lpBytesPerSector: &freeSpace.BytesPerSector,
                    lpNumberOfFreeClusters: &freeSpace.NumberOfFreeClusters,
                    lpTotalNumberOfClusters: &freeSpace.TotalNumberOfClusters))
                {
                    throw Error.GetExceptionForLastError(directory);
                }
            }

            return freeSpace;
        }

        public static ExtendedDiskFreeSpace GetDiskFreeSpaceExtended(string directory)
        {
            ExtendedDiskFreeSpace freeSpace;

            unsafe
            {
                if (!Imports.GetDiskFreeSpaceExW(
                    lpDirectoryName: directory,
                    lpFreeBytesAvailable: &freeSpace.FreeBytesAvailable,
                    lpTotalNumberOfBytes: &freeSpace.TotalNumberOfBytes,
                    lpTotalNumberOfFreeBytes: &freeSpace.TotalNumberOfFreeBytes))
                {
                    throw Error.GetExceptionForLastError(directory);
                }
            }

            return freeSpace;
        }

        /// <summary>
        /// Helper for identifying the special invalid attributes.
        /// </summary>
        public static bool AreInvalid(this FileAttributes attributes)
        {
            return attributes == FileAttributes.Invalid;
        }

        /// <summary>
        /// Gets the flags for available drive letters.
        /// </summary>
        public static LogicalDrives GetLogicalDrives()
        {
            LogicalDrives drives = Imports.GetLogicalDrives();
            if (drives == 0)
                throw Error.GetExceptionForLastError();
            return drives;
        }

        /// <summary>
        /// Convert the given drive letter into a logical drive bit flag.
        /// Returns default if impossible.
        /// </summary>
        public static LogicalDrives GetLogicalDrive(char letter)
        {
            letter = char.ToUpperInvariant(letter);
            if (letter < 'A' || letter > 'Z')
                return default;

            return (LogicalDrives)(1 << (letter - 'A'));
        }

        /// <summary>
        /// Return the next available drive letter after the given drive letter.
        /// Returns default if no additional valid letters can be found.
        /// </summary>
        public static char GetNextAvailableDrive(char after)
        {
            after = char.ToUpperInvariant(after);
            if (after < 'A' || after > 'Y')
                return default;

            uint drives = (uint)GetLogicalDrives();
            for (int i = after - 'A' + 1; i < 26; i++)
            {
                if ((i & drives) > 0)
                    return (char)('A' + i);
            }

            return default;
        }
    }
}
