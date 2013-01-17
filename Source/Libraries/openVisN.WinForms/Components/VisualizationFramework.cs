﻿//******************************************************************************************************
//  VisualizationFramework.cs - Gbtc
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
//  12/15/2012 - Steven E. Chisholm
//       Generated original version of source code. 
//
//******************************************************************************************************

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using openVisN.Framework;

namespace openVisN.Components
{
    public partial class VisualizationFramework : Component
    {
        SubscriptionFramework m_framework;
        string m_server = string.Empty;
        int m_port = 0;
        bool m_useNetworkHistorian;
        bool m_enabled;
        List<ISubscriber> m_pendingSubscribers = new List<ISubscriber>();

        string[] m_localDirectories = new string[0];

        public VisualizationFramework()
        {
            InitializeComponent();

            m_framework = new SubscriptionFramework();
            Disposed += VisualizationFramework_Disposed;

        }

        void VisualizationFramework_Disposed(object sender, EventArgs e)
        {
            m_framework.Dispose();
        }

        public VisualizationFramework(IContainer container)
        {
            container.Add(this);

            InitializeComponent();

            m_framework = new SubscriptionFramework();
            Disposed += VisualizationFramework_Disposed;

        }

        public void Start()
        {
            m_framework.Start(Paths);
        }

        public void Stop()
        {

        }

        public SubscriptionFramework Framework
        {
            get
            {
                return m_framework;
            }
        }

        [
        Bindable(true),
        Browsable(true),
        Category("Communications"),
        Description("Determines whether the framework is connected to the historian"),
        DefaultValue(false)
        ]
        public bool Enabled
        {
            get
            {
                return m_enabled;
            }
            set
            {
                m_enabled = value;
            }

        }

        [
        Bindable(true),
        Browsable(true),
        Category("Communications"),
        Description("When using a local historian, these are the paths that it will use to connect."),
        DefaultValue("")
        ]
        public string[] Paths
        {
            get
            {
                return m_localDirectories;
            }
            set
            {
                m_localDirectories = value;
            }
        }

        [
        Bindable(true),
        Browsable(true),
        Category("Communications"),
        Description("Determines whether to connect remotely to a historian, or create a local one"),
        DefaultValue(true)
        ]
        public bool UseNetworkHistorian
        {
            get
            {
                return m_useNetworkHistorian;
            }
            set
            {
                m_useNetworkHistorian = value;
            }
        }

        [
        Browsable(true),
        Bindable(true),
        Description("Contains the server hostname or IP address that contains the historian."),
        Category("Communications"),
        DefaultValue("")
        ]
        public string Server
        {
            get
            {
                return m_server;
            }
            set
            {
                m_server = value;
            }
        }

        [
        Browsable(true),
        Bindable(true),
        Description("Contains the port to communicate to the historian on."),
        Category("Communications"),
        DefaultValue(0)
        ]
        public int Port
        {
            get
            {
                return m_port;
            }
            set
            {
                m_port = value;
            }
        }

   }
}
