﻿//******************************************************************************************************
//  BufferedFile.cs - Gbtc
//
//  Copyright © 2013, Grid Protection Alliance.  All Rights Reserved.
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
//  2/1/2013 - Steven E. Chisholm
//       Generated original version of source code. 
//       
//
//******************************************************************************************************

using System;
using System.IO;
using System.Linq;
using System.Threading;
using GSF;
using GSF.IO.Unmanaged;
using GSF.UnmanagedMemory;

namespace openHistorian.FileStructure.IO
{
    /// <summary>
    /// A buffered file stream utilizes the buffer pool to intellectually cache
    /// the contents of files.  
    /// </summary>
    /// <remarks>
    /// The cache algorithm is a least recently used algorithm.
    /// but will place more emphysis on object that are repeatidly accessed over 
    /// ones that are rarely accessed. This is accomplised by incrementing a counter
    /// every time a page is accessed and dividing by 2 every time a collection occurs from the buffer pool.
    /// </remarks>
    //ToDo: Consider allowing this class to scale horizontally like how the concurrent dictionary scales.
    //ToDo: this will reduce the concurrent contention on the class at the cost of more memory required.
    unsafe internal partial class BufferedFile : DiskMediumBase
    {
        GSF.IO.Unmanaged.MemoryStream m_writeBuffer;
        IBinaryStreamIoSession m_writeBufferIO;

        long m_endOfCommittedPosition;
        long m_endOfHeaderPosition;

        /// <summary>
        /// To synchronize all calls to this class.
        /// </summary>
        object m_syncRoot;

        /// <summary>
        /// To limit flushing to a single flush call
        /// </summary>
        object m_syncFlush;

        BufferPool m_pool;

        /// <summary>
        /// The file stream use by this class.
        /// </summary>
        FileStream m_baseStream;

        PageReplacementAlgorithm m_pageReplacementAlgorithm;

        bool m_disposed;
        bool m_ownsStream;

        IoQueue m_queue;

        public BufferedFile(FileStream stream, bool ownsStream, OpenMode openMode)
            : this(stream, Globals.BufferPool, 4096, ownsStream, openMode)
        {

        }

        /// <summary>
        /// Creates a file backed memory stream.
        /// </summary>
        /// <param name="stream">The file stream to back</param>
        /// <param name="pool"></param>
        /// <param name="dirtyPageSize"></param>
        public BufferedFile(FileStream stream, BufferPool pool, int dirtyPageSize, bool ownsStream, OpenMode openMode)
            : base(pool.PageSize, dirtyPageSize)
        {
            m_endOfHeaderPosition = dirtyPageSize * 10;
            m_writeBuffer = new GSF.IO.Unmanaged.MemoryStream(pool);
            m_writeBufferIO = m_writeBuffer.GetNextIoSession();
            m_ownsStream = ownsStream;
            m_pool = pool;

            m_queue = new IoQueue(stream, pool.PageSize, dirtyPageSize, this);

            m_syncRoot = new object();
            m_syncFlush = new object();

            m_pageReplacementAlgorithm = new PageReplacementAlgorithm(pool);
            m_baseStream = stream;
            pool.RequestCollection += BufferPool_RequestCollection;

            if (openMode == OpenMode.Create)
            {
                Initialize(FileHeaderBlock.CreateNew(dirtyPageSize));
                byte[] header = Header.GetBytes();
                stream.Position = 0;
                for (int x = 0; x < 10; x++)
                {
                    stream.Write(header, 0, header.Length);
                }
            }
            else
            {
                byte[] buffer = new byte[dirtyPageSize];
                stream.Position = 0;
                stream.Read(buffer, 0, dirtyPageSize);
                Initialize(FileHeaderBlock.Open(buffer));
            }
            m_endOfCommittedPosition = (Header.LastAllocatedBlock + 1) * (long)dirtyPageSize;
        }

        void GetBlock(PageLock pageLock, long position, bool isWriting, out IntPtr firstPointer, out long firstPosition, out int length, out bool supportsWriting)
        {
            pageLock.Clear();
            if (position >= m_endOfCommittedPosition)
            {
                m_writeBufferIO.GetBlock(position - m_endOfCommittedPosition, isWriting, out firstPointer, out firstPosition, out length, out supportsWriting);
                firstPosition += m_endOfCommittedPosition;
                return;
            }
            else if (position < m_endOfHeaderPosition)
            {
                throw new ArgumentOutOfRangeException("position", "Cannot use this function to modify the file header.");
            }
            else
            {
                if (isWriting)
                    throw new ArgumentException("Cannot write to committed data space", "isWriting");
                supportsWriting = false;
                length = DiskBlockSize;

                long relativePosition = (position - m_endOfHeaderPosition) & ~(long)m_pool.PageMask; //rounds to the beginning of the block to be looked up.
                firstPosition = relativePosition + m_endOfHeaderPosition;

                GetBlockFromCommittedSpace(pageLock, relativePosition, out firstPointer);

                //Make sure the block does not go beyond the end of the uncommitted space.
                if (firstPosition + length > m_endOfCommittedPosition)
                    length = (int)(m_endOfCommittedPosition - m_endOfHeaderPosition - relativePosition);

                return;
            }
        }

        void GetBlockFromCommittedSpace(PageLock pageLock, long relativePosition, out IntPtr firstPointer)
        {

            lock (m_syncRoot)
            {
                if (m_pageReplacementAlgorithm.TryGetSubPage(pageLock, relativePosition, out firstPointer))
                {
                    return;
                }
            }

            //If the address doesn't exist in the current list. Read it from the disk.
            int poolPageIndex;
            IntPtr poolAddress;
            m_pool.AllocatePage(out poolPageIndex, out poolAddress);

            m_queue.Read(relativePosition + m_endOfHeaderPosition, poolAddress);
            bool wasPageAdded;
            lock (m_syncRoot)
            {
                firstPointer = m_pageReplacementAlgorithm.AddOrGetPage(pageLock, relativePosition, poolAddress, poolPageIndex, out wasPageAdded);
            }
            if (!wasPageAdded)
                m_pool.ReleasePage(poolPageIndex);
        }

        protected override void FlushWithHeader(FileHeaderBlock header)
        {
            long oldEndOfCommittedBytes = m_endOfCommittedPosition;
            long endOfUncommitted = (header.LastAllocatedBlock + 1) * (long)FileStructureBlockSize;
            m_queue.Write(m_writeBufferIO, m_endOfCommittedPosition, endOfUncommitted - m_endOfCommittedPosition, true);
            m_endOfCommittedPosition = endOfUncommitted;
            var bytes = header.GetBytes();
            m_queue.WriteToDisk(0, bytes, FileStructureBlockSize);
            m_queue.WriteToDisk(FileStructureBlockSize, bytes, FileStructureBlockSize);
            m_queue.WriteToDisk(FileStructureBlockSize * ((header.SnapshotSequenceNumber & 7) + 2), bytes, FileStructureBlockSize);
            m_queue.FlushFileBuffers();

            //Finish filling up the split page in the buffer.
            lock (m_syncRoot)
            {
                IntPtr ptrDest;
                long relativePosition = oldEndOfCommittedBytes - m_endOfHeaderPosition;
                int startPos = (int)(relativePosition & (DiskBlockSize - 1));
                long startOfRelative = relativePosition - (relativePosition & (DiskBlockSize - 1)); //Mod
                int copyLength = DiskBlockSize - (int)(relativePosition - startOfRelative);
                if (copyLength == DiskBlockSize)
                    return;
                if (m_pageReplacementAlgorithm.TryGetSubPageNoLock(startOfRelative, out ptrDest))
                {
                    int length;
                    IntPtr ptrSrc;
                    m_writeBufferIO.ReadBlock(0, out ptrSrc, out length);
                    Footer.WriteChecksumResultsToFooter(ptrSrc, FileStructureBlockSize, copyLength);
                    Memory.Copy((byte*)ptrSrc, (byte*)ptrDest + startPos, copyLength);
                }
            }

        }

        public override void RollbackChanges()
        {
            //throw new NotImplementedException();
        }

        public override void Dispose()
        {
            if (!m_disposed)
            {
                try
                {
                    m_disposed = true;
                    //Unregistering from this event gaurentees that a collection will no longer
                    //be called since this class utilizes custom code to garentee this.
                    Globals.BufferPool.RequestCollection -= BufferPool_RequestCollection;

                    lock (m_syncRoot)
                    {
                        m_pageReplacementAlgorithm.Dispose();
                        if (m_ownsStream)
                            m_baseStream.Dispose();
                        m_writeBufferIO.Dispose();
                        m_writeBuffer.Dispose();
                    }
                }
                finally
                {
                    m_disposed = true;
                    m_pageReplacementAlgorithm = null;
                    m_writeBuffer = null;
                    m_writeBufferIO = null;
                    m_queue = null;
                }

            }
        }

        void BufferPool_RequestCollection(object sender, CollectionEventArgs e)
        {
            if (m_disposed)
                return;

            lock (m_syncRoot)
            {
                if (m_disposed)
                    return;
                m_pageReplacementAlgorithm.DoCollection(e);
            }

            if (e.CollectionMode == BufferPoolCollectionMode.Critical)
            {
                //ToDo: actually do something differently if collection level reaches critical
                lock (m_syncRoot)
                {
                    if (m_disposed)
                        return;
                    m_pageReplacementAlgorithm.DoCollection(e);
                }
            }
        }

        public override IBinaryStreamIoSession GetNextIoSession()
        {
            lock (m_syncRoot)
            {
                return new IoSession(this, m_pageReplacementAlgorithm.GetPageLock());
            }
        }

        public override long Length
        {
            get
            {
                return m_baseStream.Length;
            }
        }

        public override bool IsReadOnly
        {
            get
            {
                return !m_baseStream.CanWrite;
            }
        }

        public override bool IsDisposed
        {
            get
            {
                return m_disposed;
            }
        }


    }
}
