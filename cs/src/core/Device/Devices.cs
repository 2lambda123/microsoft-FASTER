﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

using System;
using System.IO;
using System.Linq.Expressions;
using System.Runtime.InteropServices;

namespace FASTER.core
{
    /// <summary>
    /// Factory to create FASTER objects
    /// </summary>
    public static class Devices
    {
        /// <summary>
        /// This value is supplied for capacity when the device does not have a specified limit.
        /// </summary>
        public const long CAPACITY_UNSPECIFIED = -1;
        private const string EMULATED_STORAGE_STRING = "UseDevelopmentStorage=true;";
        private const string TEST_CONTAINER = "test";

        /// <summary>
        /// Create a storage device for the log
        /// </summary>
        /// <param name="logPath">Path to file that will store the log (empty for null device)</param>
        /// <param name="preallocateFile">Whether we try to preallocate the file on creation</param>
        /// <param name="deleteOnClose">Delete files on close</param>
        /// <param name="capacity"></param>
        /// <returns>Device instance</returns>
        public static IDevice CreateLogDevice(string logPath, bool preallocateFile = true, bool deleteOnClose = false, long capacity = CAPACITY_UNSPECIFIED)
        {
            if (string.IsNullOrWhiteSpace(logPath))
                return new NullDevice();

            IDevice logDevice;

#if DOTNETCORE
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                logDevice = new ManagedLocalStorageDevice(logPath, preallocateFile, deleteOnClose, capacity);
            }
            else
#endif
            {
                logDevice = new LocalStorageDevice(logPath, preallocateFile, deleteOnClose, capacity: capacity);
            }
            return logDevice;
        }
    }
}
