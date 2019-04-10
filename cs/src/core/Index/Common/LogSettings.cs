﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.


using System;

namespace FASTER.core
{
    /// <summary>
    /// Configuration settings for serializing objects
    /// </summary>
    /// <typeparam name="Key"></typeparam>
    /// <typeparam name="Value"></typeparam>
    public class SerializerSettings<Key, Value>
    {
        /// <summary>
        /// Key serializer
        /// </summary>
        public Func<IObjectSerializer<Key>> keySerializer;

        /// <summary>
        /// Value serializer
        /// </summary>
        public Func<IObjectSerializer<Value>> valueSerializer;
    }

    /// <summary>
    /// Interface for variable length in-place objects
    /// modeled as structs, in FASTER
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public interface IVarLenStruct<T>
    {
        /// <summary>
        /// Actual length of object
        /// </summary>
        /// <param name="t"></param>
        /// <returns></returns>
        int GetLength(ref T t);

        /// <summary>
        /// Average length of objects
        /// </summary>
        /// <returns></returns>
        int GetAverageLength();

        /// <summary>
        /// Initial length, when populating for RMW from given input
        /// </summary>
        /// <typeparam name="Input"></typeparam>
        /// <param name="input"></param>
        /// <returns></returns>
        int GetInitialLength<Input>(ref Input input);
    }


    internal struct FixedLengthStruct<T> : IVarLenStruct<T>
    {
        private static readonly int size = Utility.GetSize(default(T));

        public int GetAverageLength()
        {
            return size;
        }

        public int GetInitialLength<Input>(ref Input input)
        {
            return size;
        }

        public int GetLength(ref T t)
        {
            return size;
        }
    }

    /// <summary>
    /// Settings for variable length keys and values
    /// </summary>
    /// <typeparam name="Key"></typeparam>
    /// <typeparam name="Value"></typeparam>
    public class VariableLengthStructSettings<Key, Value>
    {
        /// <summary>
        /// Key length
        /// </summary>
        public IVarLenStruct<Key> keyLength;

        /// <summary>
        /// Value length
        /// </summary>
        public IVarLenStruct<Value> valueLength;
    }


    /// <summary>
    /// Configuration settings for hybrid log
    /// </summary>
    public class LogSettings
    {
        /// <summary>
        /// Device used for main hybrid log
        /// </summary>
        public IDevice LogDevice = new NullDevice();

        /// <summary>
        /// Device used for serialized heap objects in hybrid log
        /// </summary>
        public IDevice ObjectLogDevice = new NullDevice();

        /// <summary>
        /// Size of a segment (group of pages), in bits
        /// </summary>
        public int PageSizeBits = 25;

        /// <summary>
        /// Size of a segment (group of pages), in bits
        /// </summary>
        public int SegmentSizeBits = 30;

        /// <summary>
        /// Total size of in-memory part of log, in bits
        /// </summary>
        public int MemorySizeBits = 34;

        /// <summary>
        /// Fraction of log marked as mutable (in-place updates)
        /// </summary>
        public double MutableFraction = 0.9;

        /// <summary>
        /// Copy reads to tail of log
        /// </summary>
        public bool CopyReadsToTail = false;

        /// <summary>
        /// Settings for optional read cache
        /// Overrides the "copy reads to tail" setting
        /// </summary>
        public ReadCacheSettings ReadCacheSettings = null;
    }

    /// <summary>
    /// Configuration settings for hybrid log
    /// </summary>
    public class ReadCacheSettings
    {
        /// <summary>
        /// Size of a segment (group of pages), in bits
        /// </summary>
        public int PageSizeBits = 25;

        /// <summary>
        /// Total size of in-memory part of log, in bits
        /// </summary>
        public int MemorySizeBits = 34;

        /// <summary>
        /// Fraction of log used for second chance copy to tail
        /// </summary>
        public double SecondChanceFraction = 0.9;
    }
}
