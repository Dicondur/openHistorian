﻿//******************************************************************************************************
//  HistorianCompressionDeltaScanner.cs - Gbtc
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
//  7/26/2013 - Steven E. Chisholm
//       Generated original version of source code. 
//     
//******************************************************************************************************

using System;
using GSF;
using GSF.IO;

namespace openHistorian.Collections.Generic.CustomCompression
{
    public unsafe class HistorianCompressionDeltaScanner
        : EncodedNodeScannerBase<HistorianKey, HistorianValue>
    {
        ulong m_prevTimestamp;
        ulong m_prevPointId;
        ulong m_prevEntryNumber;
        ulong m_prevValue1;
        ulong m_prevValue2;
        ulong m_prevValue3;

        public HistorianCompressionDeltaScanner(byte level, int blockSize, BinaryStreamBase stream, Func<HistorianKey, byte, uint> lookupKey, TreeKeyMethodsBase<HistorianKey> keyMethods, TreeValueMethodsBase<HistorianValue> valueMethods)
            : base(level, blockSize, stream, lookupKey, keyMethods, valueMethods, 2)
        {
        }

        protected override unsafe int DecodeRecord(byte* stream, HistorianKey key, HistorianValue value)
        {
            int position = 0;
            m_prevTimestamp ^= Compression.Read7BitUInt64(stream, ref position);
            m_prevPointId ^= Compression.Read7BitUInt64(stream, ref position);
            m_prevEntryNumber ^= Compression.Read7BitUInt64(stream, ref position);
            m_prevValue1 ^= Compression.Read7BitUInt64(stream, ref position);
            m_prevValue2 ^= Compression.Read7BitUInt64(stream, ref position);
            m_prevValue3 ^= Compression.Read7BitUInt64(stream, ref position);

            key.Timestamp = m_prevTimestamp;
            key.PointID = m_prevPointId;
            key.EntryNumber = m_prevEntryNumber;
            value.Value1 = m_prevValue1;
            value.Value2 = m_prevValue2;
            value.Value3 = m_prevValue3;
            return position;
        }

        protected override void ResetEncoder()
        {
            m_prevTimestamp = 0;
            m_prevPointId = 0;
            m_prevEntryNumber = 0;
            m_prevValue1 = 0;
            m_prevValue2 = 0;
            m_prevValue3 = 0;
        }

    }
}