﻿// ------------------------
//    WInterop Framework
// ------------------------

// Copyright (c) Jeremy W. Kuhne. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using WInterop.Errors;
using WInterop.Storage;
using WInterop.Support;

namespace Tests.Support
{
    public static class FileHelper
    {
        public static void WriteAllBytes(string path, byte[] data)
        {
            using (var stream = Storage.CreateFileStream(path,
                DesiredAccess.GenericWrite, ShareModes.ReadWrite, CreationDisposition.OpenAlways))
            {
                using (var writer = new System.IO.BinaryWriter(stream))
                {
                    writer.Write(data);
                }
            }
        }

        public static void WriteAllText(string path, string text)
        {
            using (var stream = Storage.CreateFileStream(path,
                DesiredAccess.GenericWrite, ShareModes.ReadWrite, CreationDisposition.OpenAlways))
            {
                using (var writer = new System.IO.StreamWriter(stream))
                {
                    writer.Write(text);
                }
            }
        }

        public static string ReadAllText(string path)
        {
            using (var stream = Storage.CreateFileStream(path,
                DesiredAccess.GenericRead, ShareModes.ReadWrite, CreationDisposition.OpenExisting))
            {
                using (var reader = new System.IO.StreamReader(stream))
                {
                    return reader.ReadToEnd();
                }
            }
        }

        public static string CreateDirectoryRecursive(string path)
        {
            if (!Storage.PathExists(path))
            {
                int lastSeparator = path.LastIndexOfAny(new char[] { Paths.DirectorySeparator, Paths.AltDirectorySeparator });
                CreateDirectoryRecursive(path.Substring(0, lastSeparator));
                Storage.CreateDirectory(path);
            }

            return path;
        }

        public static void DeleteDirectoryRecursive(string path)
        {
            var data = Storage.TryGetFileInfo(path);
            if (!data.HasValue)
            {
                // Nothing found
                WindowsError.ERROR_PATH_NOT_FOUND.Throw(path);
            }

            if ((data.Value.FileAttributes & FileAttributes.Directory) != FileAttributes.Directory)
            {
                // Not a directory, a file
                WindowsError.ERROR_FILE_EXISTS.Throw(path);
            }

            if ((data.Value.FileAttributes & FileAttributes.ReadOnly) == FileAttributes.ReadOnly)
            {
                // Make it writable
                Storage.SetFileAttributes(path, data.Value.FileAttributes & ~FileAttributes.ReadOnly);
            }

            // Reparse points don't need to be empty to be deleted. Deleting will simply disconnect the reparse point, which is what we want.
            if ((data.Value.FileAttributes & FileAttributes.ReparsePoint) != FileAttributes.ReparsePoint)
            {
                foreach (FindResult findResult in new FindOperation<FindResult>(path))
                {
                    if ((findResult.Attributes & FileAttributes.Directory) == FileAttributes.Directory)
                        DeleteDirectoryRecursive(Paths.Combine(path, findResult.FileName));
                    else
                        Storage.DeleteFile(Paths.Combine(path, findResult.FileName));
                }
            }

            // We've either emptied or we're a reparse point, delete the directory
            Storage.RemoveDirectory(path);
        }

        public static void EnsurePathDirectoryExists(string path)
        {
            CreateDirectoryRecursive(TrimLastSegment(path));
        }

        public static string TrimLastSegment(string path)
        {
            int length = path.Length;
            while (((length > 0)
                && (path[--length] != Paths.DirectorySeparator))
                && (path[length] != Paths.AltDirectorySeparator)) { }
            return path.Substring(0, length);
        }
    }
}
