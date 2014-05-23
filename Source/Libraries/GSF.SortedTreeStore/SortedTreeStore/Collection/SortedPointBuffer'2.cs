﻿//******************************************************************************************************
//  SortedPointBuffer`2.cs - Gbtc
//
//  Copyright © 2014, Grid Protection Alliance.  All Rights Reserved.
//
//  Licensed to the Grid Protection Alliance (GPA) under one or more contributor license agreements. See
//  the NOTICE file distributed with this work for additional information regarding copyright ownership.
//  The GPA licenses this file to you under the Eclipse Public License -v 1.0 (the "License"); you may
//  not use this file except in compliance with the License. You may obtain a copy of the License at:
//
//      http://www.opensource.org/licenses/eclipse-1.0.php
//
//  Unless agreed to in writing, the subject software distributed under the License is distributed on an
//  "AS-IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. Refer to the
//  License for the specific language governing permissions and limitations.
//
//  Code Modification History:
//  ----------------------------------------------------------------------------------------------------
//  2/5/2014 - Steven E. Chisholm
//       Generated original version of source code. 
//     
//******************************************************************************************************

using System;
using GSF.SortedTreeStore.Tree;

namespace GSF.SortedTreeStore.Collection
{
    /// <summary>
    /// A temporary point buffer that is designed to write unsorted data to it, 
    /// the read the data back out sorted. 
    /// </summary>
    /// <typeparam name="TKey">The key type to use</typeparam>
    /// <typeparam name="TValue">The value type to use</typeparam>
    public class SortedPointBuffer<TKey, TValue>
        : TreeStream<TKey, TValue>
        where TKey : SortedTreeTypeBase<TKey>, new()
        where TValue : SortedTreeTypeBase<TValue>, new()
    {
        /// <summary>
        /// exposes methods for sorting the keys.
        /// </summary>
        private TKey m_keyMethods;
        /// <summary>
        /// Contains indexes of sorted data.
        /// </summary>
        private int[] m_sortingBlocks1;
        /// <summary>
        /// Contains indexex of sorted data.
        /// </summary>
        /// <remarks>
        /// Two blocks are needed to do a merge sort since 
        /// this class uses indexes instead of actually moving
        /// the raw values.
        /// </remarks>
        private int[] m_sortingBlocks2;

        /// <summary>
        /// A block of data for storing the keys.
        /// </summary>
        private byte[] m_keyData;
        /// <summary>
        /// A block of data for storing the values.
        /// </summary>
        private byte[] m_valueData;

        /// <summary>
        /// The maximum number of items that can be stored in this buffer.
        /// </summary>
        private int m_capacity;

        /// <summary>
        /// The index of the next point to dequeue.
        /// </summary>
        private int m_dequeueIndex;
        /// <summary>
        /// The index of the next point to write.
        /// </summary>
        private int m_enqueueIndex;

        /// <summary>
        /// The number of bytes required to store a key
        /// </summary>
        private int m_keySize;
        /// <summary>
        /// The number of bytes required to store a value
        /// </summary>
        private int m_valueSize;

        /// <summary>
        /// Gets if the stream is currently reading. 
        /// The stream was not designed to be read from and written to at the same time. So the mode must be changed.
        /// </summary>
        private bool m_isReadingMode;

        /// <summary>
        /// Creates a <see cref="SortedPointBuffer{TKey,TValue}"/> that can hold only exactly the specified <see cref="capacity"/>.
        /// </summary>
        /// <param name="capacity">The maximum number of items that can be stored in this class</param>
        public SortedPointBuffer(int capacity)
        {
            m_capacity = capacity;
            m_keyMethods = new TKey();

            m_keySize = m_keyMethods.Size;
            m_valueSize = new TValue().Size;

            m_keyData = new byte[capacity * m_keySize];
            m_valueData = new byte[capacity * m_valueSize];

            m_sortingBlocks1 = new int[capacity];
            m_sortingBlocks2 = new int[capacity];

            m_isReadingMode = false;
        }

        /// <summary>
        /// Gets the current number of items in the <see cref="SortedPointBuffer{TKey,TValue}"/>
        /// </summary>
        public int Count
        {
            get
            {
                return (m_enqueueIndex - m_dequeueIndex);
            }
        }

        /// <summary>
        /// Gets if this class does not contain any items
        /// </summary>
        public bool IsEmpty
        {
            get
            {
                return m_dequeueIndex == m_enqueueIndex;
            }
        }

        /// <summary>
        /// Gets if no more items can be added to this list.
        /// </summary>
        public bool IsFull
        {
            get
            {
                return m_capacity == m_enqueueIndex;
            }
        }

        /// <summary>
        /// Gets/Sets the current mode of the point buffer.
        /// </summary>
        /// <remarks>
        /// This class is not designed to be read from and written to at the same time.
        /// This is because sorting must occur right before reading from this stream.
        /// </remarks>
        public bool IsReadingMode
        {
            get
            {
                return m_isReadingMode;
            }
            set
            {
                if (m_isReadingMode != value)
                {
                    m_isReadingMode = value;
                    if (m_isReadingMode)
                    {
                        Sort();
                    }
                    else
                    {
                        Clear();
                    }
                }

            }
        }

        /// <summary>
        /// Clears all of the items in this list.
        /// </summary>
        private void Clear()
        {
            m_dequeueIndex = 0;
            m_enqueueIndex = 0;
            SetEos(false);
        }

        /// <summary>
        /// Attempts to enqueue the provided item to the list.
        /// </summary>
        /// <param name="key">the key to add</param>
        /// <param name="value">the value to add</param>
        /// <returns>true if the item was successfully enqueued. False if the queue is full.</returns>
        unsafe public bool TryEnqueue(TKey key, TValue value)
        {
            if (m_isReadingMode)
                throw new InvalidOperationException("Cannot enqueue to a list that is in ReadMode");
            if (IsFull)
                return false;
            fixed (byte* lpk = m_keyData, lpv = m_valueData)
            {
                key.Write(lpk + m_enqueueIndex * m_keySize);
                value.Write(lpv + m_enqueueIndex * m_valueSize);
                m_enqueueIndex++;
            }
            return true;
        }

        /// <summary>
        /// Advances the stream to the next value. 
        /// If before the beginning of the stream, advances to the first value
        /// </summary>
        /// <returns>True if the advance was successful. False if the end of the stream was reached.</returns>
        unsafe protected override bool ReadNext(TKey key, TValue value)
        {
            if (!m_isReadingMode)
                throw new InvalidOperationException("Cannot read from a list that is not in ReadMode");
            if (IsEmpty)
                return false;

            //Since this class is fixed in size. Bounds checks are not necessary as they will always be valid.
            fixed (byte* lpk = m_keyData, lpv = m_valueData)
            {
                key.Read(lpk + m_sortingBlocks1[m_dequeueIndex] * m_keySize);
                value.Read(lpv + m_sortingBlocks1[m_dequeueIndex] * m_valueSize);
            }

            m_dequeueIndex++;
            return true;
        }

        /// <summary>
        /// Overrides the default behavior that disposes the stream when the end of the stream has been encountered.
        /// </summary>
        protected override void EndOfStreamReached()
        {
            SetEos(true);
        }

        /// <summary>
        /// Reads the specified item from the sorted list.
        /// </summary>
        /// <param name="index">the index of the item to read. Note: Bounds checking is not done.</param>
        /// <param name="key">the key to write to</param>
        /// <param name="value">the value to write to</param>
        internal unsafe void ReadSorted(int index, TKey key, TValue value)
        {
            if (!m_isReadingMode)
                throw new InvalidOperationException("Cannot read from a list that is not in ReadMode");
            //Since this class is fixed in size. Bounds checks are not necessary as they will always be valid.
            fixed (byte* lpk = m_keyData, lpv = m_valueData)
            {
                key.Read(lpk + m_sortingBlocks1[index] * m_keySize);
                value.Read(lpv + m_sortingBlocks1[index] * m_valueSize);
            }
        }

        /// <summary>
        /// Does a sort of the data. using a merge sort like algorithm.
        /// </summary>
        private unsafe void Sort()
        {
            fixed (byte* lp = m_keyData)
            {
                //InitialSort
                int keySize = m_keySize;
                int count = Count;

                for (int x = 0; x < count; x += 2)
                {
                    //Can't sort the last entry if not
                    if (x + 1 == count)
                    {
                        m_sortingBlocks1[x] = x;
                    }
                    else if (m_keyMethods.IsLessThanOrEqualTo(lp + keySize * x, lp + keySize * (x + 1)))
                    {
                        m_sortingBlocks1[x] = x;
                        m_sortingBlocks1[x + 1] = (x + 1);
                    }
                    else
                    {
                        m_sortingBlocks1[x] = (x + 1);
                        m_sortingBlocks1[x + 1] = x;
                    }
                }

                bool shouldSwap = false;

                fixed (int* block1 = m_sortingBlocks1, block2 = m_sortingBlocks2)
                {
                    int stride = 2;
                    while (true)
                    {
                        if (stride >= count)
                            break;

                        shouldSwap = true;
                        SortLevel(block1, block2, lp, count, stride, keySize);
                        stride *= 2;

                        if (stride >= count)
                            break;

                        shouldSwap = false;
                        SortLevel(block2, block1, lp, count, stride, keySize);
                        stride *= 2;
                    }
                }

                if (shouldSwap)
                {
                    var b1 = m_sortingBlocks1;
                    m_sortingBlocks1 = m_sortingBlocks2;
                    m_sortingBlocks2 = b1;
                }
            }
        }


        /// <summary>
        /// Does a merge sort on the provided level.
        /// </summary>
        /// <param name="srcIndex">where the current indexes exist</param>
        /// <param name="dstIndex">where the final indexes should go</param>
        /// <param name="ptr">the data</param>
        /// <param name="count">the number of entries at this level</param>
        /// <param name="stride">the number of compares per level</param>
        /// <param name="keySize">the size of the key</param>
        unsafe void SortLevel(int* srcIndex, int* dstIndex, byte* ptr, int count, int stride, int keySize)
        {
            for (int xStart = 0; xStart < count; xStart += stride + stride)
            {
                int d = xStart;
                int dEnd = Math.Min(xStart + stride + stride, count);
                int i1 = xStart;
                int i1End = Math.Min(xStart + stride, count);
                int i2 = Math.Min(xStart + stride, count);
                int i2End = Math.Min(xStart + stride + stride, count);

                if (d != dEnd && i1 != i1End && i2 != i2End)
                {
                    //Check to see if already in order, then I can shortcut

                    if (m_keyMethods.IsLessThanOrEqualTo(ptr + srcIndex[i1End - 1] * keySize, ptr + srcIndex[i2] * keySize))
                    {
                        for (int i = d; i < dEnd; i++)
                        {
                            dstIndex[i] = srcIndex[i];
                        }
                        continue;
                    }
                }

                while (d < dEnd)
                {
                    if (i1 == i1End)
                    {
                        dstIndex[d] = srcIndex[i2];
                        d++;
                        i2++;
                    }
                    else if (i2 == i2End)
                    {
                        dstIndex[d] = srcIndex[i1];
                        d++;
                        i1++;
                    }
                    else if (m_keyMethods.IsLessThanOrEqualTo(ptr + srcIndex[i1] * keySize, ptr + srcIndex[i2] * keySize))
                    {
                        dstIndex[d] = srcIndex[i1];
                        d++;
                        i1++;
                    }
                    else
                    {
                        dstIndex[d] = srcIndex[i2];
                        d++;
                        i2++;
                    }
                }
            }
        }
    }
}
