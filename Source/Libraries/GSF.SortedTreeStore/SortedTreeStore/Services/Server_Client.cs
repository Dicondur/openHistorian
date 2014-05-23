﻿//******************************************************************************************************
//  ServerHost_ClientHost.cs - Gbtc
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
//  4/19/2014 - Steven E. Chisholm
//       Generated original version of source code. 
//       
//
//******************************************************************************************************

using System;
using System.Collections.Generic;
using System.Linq;
using GSF.SortedTreeStore.Services;

namespace GSF.SortedTreeStore.Services
{
    public partial class Server
    {
        /// <summary>
        /// A client wrapper around a <see cref="Server"/>. 
        /// </summary>
        public class Client
            : SortedTreeStore.Services.Client
        {
            private bool m_disposed;
            private object m_syncRoot;
            private Server m_server;
            private Dictionary<string, ClientDatabaseBase> m_connectedDatabases;

            /// <summary>
            /// Creates a <see cref="Client"/>
            /// </summary>
            /// <param name="server">the collection to wrap</param>
            public Client(Server server)
            {
                if (server == null)
                    throw new ArgumentNullException("server");
                m_syncRoot = new object();
                m_connectedDatabases = new Dictionary<string, ClientDatabaseBase>();
                m_server = server;
                server.RegisterClient(this);
            }

            /// <summary>
            /// Gets the database that matches <see cref="databaseName"/>
            /// </summary>
            /// <param name="databaseName">the case insensitive name of the databse</param>
            /// <returns></returns>
            public override ClientDatabaseBase GetDatabase(string databaseName)
            {
                if (m_disposed)
                    throw new ObjectDisposedException(GetType().FullName);
                databaseName = databaseName.ToUpper();

                ClientDatabaseBase database;
                lock (m_syncRoot)
                {
                    if (!m_connectedDatabases.TryGetValue(databaseName, out database))
                    {
                        database = m_server.GetDatabase(databaseName).CreateClientDatabase(this, Unregister);
                        m_connectedDatabases.Add(databaseName, database);
                    }
                }
                return database;
            }

            /// <summary>
            /// Determines if <see cref="databaseName"/> is contained in the database.
            /// </summary>
            /// <param name="databaseName">Name of database instance to access.</param>
            /// <returns></returns>
            public bool Contains(string databaseName)
            {
                if (m_disposed)
                    throw new ObjectDisposedException(GetType().FullName);
                return m_server.Contains(databaseName);
            }

            /// <summary>
            /// Gets basic information for every database connected to the server.
            /// </summary>
            /// <returns></returns>
            public List<DatabaseInfo> GetDatabaseInfo()
            {
                if (m_disposed)
                    throw new ObjectDisposedException(GetType().FullName);
                return m_server.GetDatabaseInfo();
            }

            /// <summary>
            /// Unregisters a client database.
            /// </summary>
            /// <param name="client">the client database to unregister</param>
            private void Unregister(ClientDatabaseBase client)
            {
                lock (m_syncRoot)
                {
                    m_connectedDatabases.Remove(client.Info.DatabaseName);
                }
            }

            /// <summary>
            /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
            /// </summary>
            /// <filterpriority>2</filterpriority>
            public void Dispose()
            {
                if (!m_disposed)
                {
                    lock (m_syncRoot)
                    {
                        foreach (var db in m_connectedDatabases.Values.ToArray())
                        {
                            db.Dispose();
                        }
                    }
                    m_server.UnRegisterClient(this);
                    m_server = null;
                    m_disposed = true;
                }
            }
        }
    }
}
