﻿// ------------------------
//    WInterop Framework
// ------------------------

// Copyright (c) Jeremy W. Kuhne. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Win32.SafeHandles;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using WInterop.Errors;
using WInterop.Support.Buffers;
using WInterop.Console.Unsafe;
using WInterop.Windows;

namespace WInterop.Console
{
    public static partial class Console
    {
        public static WindowHandle GetConsoleWindow() => Imports.GetConsoleWindow();

        public static void FreeConsole() => Error.ThrowLastErrorIfFalse(Imports.FreeConsole());

        public static bool TryFreeConsole() => Imports.FreeConsole();

        public static uint GetConsoleInputCodePage() => Imports.GetConsoleCP();

        public static uint GetConsoleOuputCodePage() => Imports.GetConsoleOutputCP();

        public static ConsoleInputMode GetConsoleInputMode(SafeFileHandle inputHandle)
        {
            Error.ThrowLastErrorIfFalse(Imports.GetConsoleMode(inputHandle, out uint mode));
            return (ConsoleInputMode)mode;
        }

        public static void SetConsoleInputMode(SafeFileHandle inputHandle, ConsoleInputMode mode)
            => Error.ThrowLastErrorIfFalse(Imports.SetConsoleMode(inputHandle, (uint)mode));

        public static ConsoleOuputMode GetConsoleOutputMode(SafeFileHandle outputHandle)
        {
            Error.ThrowLastErrorIfFalse(Imports.GetConsoleMode(outputHandle, out uint mode));
            return (ConsoleOuputMode)mode;
        }

        public static void SetConsoleOutputMode(SafeFileHandle outputHandle, ConsoleOuputMode mode)
            => Error.ThrowLastErrorIfFalse(Imports.SetConsoleMode(outputHandle, (uint)mode));

        /// <summary>
        /// Get the specified standard handle.
        /// </summary>
        public static SafeFileHandle GetStandardHandle(StandardHandleType type)
        {
            IntPtr handle = Imports.GetStdHandle(type);
            if (handle == (IntPtr)(-1))
                throw Error.GetExceptionForLastError();

            // If there is no associated standard handle, return null
            if (handle == IntPtr.Zero)
                return null;

            // The standard handles do not need to be released.
            return new SafeFileHandle(handle, ownsHandle: false);
        }

        /// <summary>
        /// Reads input from the console. Will wait for next input, exit the iterator to stop listening.
        /// </summary>
        public static IEnumerable<InputRecord> ReadConsoleInput(SafeFileHandle inputHandle)
        {
            InputRecord buffer = new InputRecord();
            while (Imports.ReadConsoleInputW(inputHandle, ref buffer, 1, out uint read))
            {
                yield return buffer;
            }
            throw Error.GetExceptionForLastError();
        }

        /// <summary>
        /// Peek at the console input records.
        /// </summary>
        /// <param name="count">The maximum number of records to investigate.</param>
        public static IEnumerable<InputRecord> PeekConsoleInput(SafeFileHandle inputHandle, int count)
        {
            var owner = OwnedMemoryPool.Rent<InputRecord>(count);
            if (!Imports.PeekConsoleInputW(inputHandle, ref MemoryMarshal.GetReference(owner.Memory.Span), (uint)count, out uint read))
            {
                owner.Dispose();
                throw Error.GetExceptionForLastError();
            }

            return new OwnedMemoryEnumerable<InputRecord>(owner, 0, (int)read);
        }

        /// <summary>
        /// Writes the specified <paramref name="text"/> to the given console output handle.
        /// </summary>
        public unsafe static uint WriteConsole(SafeFileHandle outputHandle, ReadOnlySpan<char> text)
        {
            fixed (char* c = &MemoryMarshal.GetReference(text))
            {
                if (!Imports.WriteConsoleW(outputHandle, c, (uint)text.Length, out uint charsWritten))
                    throw Error.GetExceptionForLastError();

                return charsWritten;
            }
        }
    }
}
