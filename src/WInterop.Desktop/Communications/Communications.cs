﻿// ------------------------
//    WInterop Framework
// ------------------------

// Copyright (c) Jeremy W. Kuhne. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Win32.SafeHandles;
using System.Collections.Generic;
using System.Linq;
using WInterop.Communications.Unsafe;
using WInterop.Errors;
using WInterop.Registry;
using WInterop.Storage;
using WInterop.Windows;

namespace WInterop.Communications
{
    public static partial class Communications
    {
        public unsafe static DeviceControlBlock GetCommunicationsState(SafeFileHandle fileHandle)
        {
            DeviceControlBlock dcb = new DeviceControlBlock()
            {
                DCBlength = (uint)sizeof(DeviceControlBlock)
            };

            if (!Imports.GetCommState(fileHandle, ref dcb))
                throw Error.GetExceptionForLastError();

            return dcb;
        }

        public unsafe static void SetCommunicationsState(SafeFileHandle fileHandle, ref DeviceControlBlock dcb)
        {
            dcb.DCBlength = (uint)sizeof(DeviceControlBlock);

            if (!Imports.GetCommState(fileHandle, ref dcb))
                throw Error.GetExceptionForLastError();
        }

        public unsafe static DeviceControlBlock BuildDeviceControlBlock(string definition)
        {
            if (!Imports.BuildCommDCBW(definition, out DeviceControlBlock dcb))
                throw Error.GetExceptionForLastError();

            return dcb;
        }

        public static CommunicationsProperties GetCommunicationsProperties(SafeFileHandle fileHandle)
        {
            if (!Imports.GetCommProperties(fileHandle, out CommunicationsProperties properties))
                throw Error.GetExceptionForLastError();

            return properties;
        }

        public unsafe static CommunicationsConfig GetCommunicationsConfig(SafeFileHandle fileHandle)
        {
            CommunicationsConfig config = new CommunicationsConfig();
            uint size = (uint)sizeof(CommunicationsConfig);

            if (!Imports.GetCommConfig(fileHandle, ref config, ref size))
                throw Error.GetExceptionForLastError();

            return config;
        }

        /// <summary>
        /// Get the default config values for the given com port.
        /// </summary>
        /// <param name="port">Simple name only (COM1, not \\.\COM1)</param>
        public unsafe static CommunicationsConfig GetDefaultCommunicationsConfig(string port)
        {
            CommunicationsConfig config = new CommunicationsConfig();
            uint size = (uint)sizeof(CommunicationsConfig);

            if (!Imports.GetDefaultCommConfigW(port, ref config, ref size))
                throw Error.GetExceptionForLastError();

            return config;
        }

        /// <summary>
        /// Pops the COM port configuration dialog and returns the selected settings.
        /// </summary>
        /// <exception cref="OperationCanceledException">Thrown if the dialog is cancelled.</exception>
        public unsafe static CommunicationsConfig CommunicationsConfigDialog(string port, WindowHandle parent)
        {
            CommunicationsConfig config = GetDefaultCommunicationsConfig(port);

            if (!Imports.CommConfigDialogW(port, parent, ref config))
                throw Error.GetExceptionForLastError();

            return config;
        }

        /// <summary>
        /// Simple helper for CreateFile call that sets the expected values when opening a COM port.
        /// </summary>
        public static SafeFileHandle CreateComPortFileHandle(
            string path,
            FileAttributes fileAttributes = FileAttributes.None,
            FileFlags fileFlags = FileFlags.None)
        {
            return Storage.Storage.CreateFile(
                path,
                CreationDisposition.OpenExisting,
                DesiredAccess.GenericReadWrite,
                0,
                fileAttributes,
                fileFlags);
        }

        public static IEnumerable<string> GetAvailableComPorts()
        {
            using (var key = Registry.Registry.OpenKey(
                RegistryKeyHandle.HKEY_LOCAL_MACHINE, @"HARDWARE\DEVICEMAP\SERIALCOMM"))
            {
                return Registry.Registry.GetValueDataDirect(key, RegistryValueType.String).OfType<string>().ToArray();
            }
        }
    }
}
