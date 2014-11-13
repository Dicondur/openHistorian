﻿//******************************************************************************************************
//  HistorianClient.cs - Gbtc
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
//  11/8/2013 - Steven E. Chisholm
//       Generated original version of source code. 
//       
//
//******************************************************************************************************

using GSF.Snap.Services.Net;

namespace openHistorian.Net
{
    /// <summary>
    /// Connects to a socket based remoted historian database collection.
    /// </summary>
    public class HistorianClient :
        SnapNetworkClient
    {
        public HistorianClient(string serverNameOrIp, int port, bool integratedSecurity = false)
            : base(new SnapNetworkClientSettings()
            {
                NetworkPort = port,
                ServerNameOrIp = serverNameOrIp,
                UseIntegratedSecurity = integratedSecurity
            }, null, false)
        {

        }
    }
}