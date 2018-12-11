﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

using System.IO;

namespace FASTER.core
{
    public interface IKey<T>
    {
        long GetHashCode64();
        bool Equals(ref T k2);
    }
}