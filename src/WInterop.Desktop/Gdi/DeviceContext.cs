﻿// ------------------------
//    WInterop Framework
// ------------------------

// Copyright (c) Jeremy W. Kuhne. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Drawing;
using WInterop.Gdi.Unsafe;
using WInterop.Windows;

namespace WInterop.Gdi
{
    /// <summary>
    /// DeviceContext handle (HDC)
    /// </summary>
    public readonly struct DeviceContext : IDisposable
    {
        public HDC Handle { get; }
        private readonly WindowHandle _window;
        private readonly CollectionType _type;

        public DeviceContext(HDC handle, bool ownsHandle = false)
        {
            _window = default;
            Handle = handle;
            _type = ownsHandle ? CollectionType.Delete : CollectionType.None;
        }

        public DeviceContext(HDC handle, WindowHandle windowHandle)
        {
            _window = windowHandle;
            Handle = handle;
            _type = CollectionType.Release;
        }

        public DeviceContext(HDC handle, WindowHandle windowHandle, in PAINTSTRUCT paint)
        {
            _window = windowHandle;
            _type = CollectionType.EndPaint;
            Handle = paint.hdc;
        }

        public void Dispose()
        {
            switch (_type)
            {
                case CollectionType.Delete:
                    if (!Imports.DeleteDC(Handle))
                    { } // Debug.Fail("Failed to delete DC");
                    break;
                case CollectionType.Release:
                    if (!Imports.ReleaseDC(_window, Handle))
                    { } // Debug.Fail("Failed to release DC");
                    break;
                case CollectionType.EndPaint:
                    PAINTSTRUCT ps = new PAINTSTRUCT(Handle);
                    if (!Imports.EndPaint(_window, in ps))
                    { } // Debug.Fail("Failed to end paint");
                    break;
                case CollectionType.None:
                    break;
                default:
                    throw new InvalidOperationException();
            }
        }

        public bool IsInvalid => Handle.IsInvalid;

        public static implicit operator HDC(DeviceContext context) => context.Handle;
        public static explicit operator DeviceContext(WParam wparam) => new DeviceContext(new HDC((IntPtr)wparam));

        private enum CollectionType
        {
            None,
            Delete,
            Release,
            EndPaint
        }

        public void TextOut(Point point, object p)
        {
            throw new NotImplementedException();
        }
    }
}
