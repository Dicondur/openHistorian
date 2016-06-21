﻿//******************************************************************************************************
//  ComparisonUtility.cs - Gbtc
//
//  Copyright © 2016, Grid Protection Alliance.  All Rights Reserved.
//
//  Licensed to the Grid Protection Alliance (GPA) under one or more contributor license agreements. See
//  the NOTICE file distributed with this work for additional information regarding copyright ownership.
//  The GPA licenses this file to you under the MIT License (MIT), the "License"; you may
//  not use this file except in compliance with the License. You may obtain a copy of the License at:
//
//      http://opensource.org/licenses/MIT
//
//  Unless agreed to in writing, the subject software distributed under the License is distributed on an
//  "AS-IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. Refer to the
//  License for the specific language governing permissions and limitations.
//
//  Code Modification History:
//  ----------------------------------------------------------------------------------------------------
//  06/15/2016 - J. Ritchie Carroll
//       Generated original version of source code.
//
//******************************************************************************************************

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using GSF;
using GSF.Collections;
using GSF.Configuration;
using GSF.IO;
using GSF.Windows.Forms;

namespace ComparisonUtility
{
    public partial class ComparisonUtility : Form
    {
        private bool m_formClosing;

        public ComparisonUtility()
        {
            InitializeComponent();
        }

        private void ComparisonUtility_Load(object sender, EventArgs e)
        {
            CategorizedSettingsElementCollection settings = ConfigurationFile.Current.Settings["systemSettings"];

            settings.Add("sourceHostAddress", textBoxSourceHistorianHostAddress.Text, "Host address of source historian", false, SettingScope.User);
            settings.Add("sourceDataPort", maskedTextBoxSourceHistorianDataPort.Text, "Data port of source historian", false, SettingScope.User);
            settings.Add("sourceMetaDataPort", maskedTextBoxSourceHistorianMetaDataPort.Text, "Meta-data port of source historian", false, SettingScope.User);
            settings.Add("sourceInstanceName", textBoxSourceHistorianInstanceName.Text, "Instance name of source historian", false, SettingScope.User);
            settings.Add("destinationHostAddress", textBoxDestinationHistorianHostAddress.Text, "Host address of destination historian", false, SettingScope.User);
            settings.Add("destinationDataPort", maskedTextBoxDestinationHistorianDataPort.Text, "Data port of destination historian", false, SettingScope.User);
            settings.Add("destinationMetaDataPort", maskedTextBoxDestinationHistorianMetaDataPort.Text, "Meta-data port of destination historian", false, SettingScope.User);
            settings.Add("destinationInstanceName", textBoxDestinationHistorianInstanceName.Text, "Instance name of destination historian", false, SettingScope.User);
            settings.Add("frameRate", maskedTextBoxFrameRate.Text, "Frame rate, in frames per second, used to estimate total data for timespan", false, SettingScope.User);
            settings.Add("metaDataTimeout", maskedTextBoxMetaDataTimeout.Text, "Meta-data retriever timeout", false, SettingScope.User);
            settings.Add("startTime", dateTimePickerSourceTime.Text, "Start of time range", false, SettingScope.User);
            settings.Add("endTime", dateTimePickerEndTime.Text, "End of time range", false, SettingScope.User);
            settings.Add("messageInterval", maskedTextBoxMessageInterval.Text, "Message display interval", false, SettingScope.User);
            settings.Add("enableLogging", checkBoxEnableLogging.Checked.ToString(), "Flag to enable detailed logging", false, SettingScope.User);

            textBoxSourceHistorianHostAddress.Text = settings["sourceHostAddress"].Value;
            maskedTextBoxSourceHistorianDataPort.Text = settings["sourceDataPort"].Value;
            maskedTextBoxSourceHistorianMetaDataPort.Text = settings["sourceMetaDataPort"].Value;
            textBoxSourceHistorianInstanceName.Text = settings["sourceInstanceName"].Value;
            textBoxDestinationHistorianHostAddress.Text = settings["destinationHostAddress"].Value;
            maskedTextBoxDestinationHistorianDataPort.Text = settings["destinationDataPort"].Value;
            maskedTextBoxDestinationHistorianMetaDataPort.Text = settings["destinationMetaDataPort"].Value;
            textBoxDestinationHistorianInstanceName.Text = settings["destinationInstanceName"].Value;
            maskedTextBoxFrameRate.Text = settings["frameRate"].Value;
            maskedTextBoxMetaDataTimeout.Text = settings["metaDataTimeout"].Value;
            dateTimePickerSourceTime.Text = settings["startTime"].Value;
            dateTimePickerEndTime.Text = settings["endTime"].Value;
            maskedTextBoxMessageInterval.Text = settings["messageInterval"].Value;
            checkBoxEnableLogging.Checked = settings["enableLogging"].ValueAs(checkBoxEnableLogging.Checked);

            this.RestoreLocation();
        }

        private void ComparisonUtility_FormClosing(object sender, FormClosingEventArgs e)
        {
            CategorizedSettingsElementCollection settings = ConfigurationFile.Current.Settings["systemSettings"];

            m_formClosing = true;

            this.SaveLocation();

            settings["sourceHostAddress"].Value = textBoxSourceHistorianHostAddress.Text;
            settings["sourceDataPort"].Value = maskedTextBoxSourceHistorianDataPort.Text;
            settings["sourceMetaDataPort"].Value = maskedTextBoxSourceHistorianMetaDataPort.Text;
            settings["sourceInstanceName"].Value = textBoxSourceHistorianInstanceName.Text;
            settings["destinationHostAddress"].Value = textBoxDestinationHistorianHostAddress.Text;
            settings["destinationDataPort"].Value = maskedTextBoxDestinationHistorianDataPort.Text;
            settings["destinationMetaDataPort"].Value = maskedTextBoxDestinationHistorianMetaDataPort.Text;
            settings["destinationInstanceName"].Value = textBoxDestinationHistorianInstanceName.Text;
            settings["frameRate"].Value = maskedTextBoxFrameRate.Text;
            settings["metaDataTimeout"].Value = maskedTextBoxMetaDataTimeout.Text;
            settings["startTime"].Value = dateTimePickerSourceTime.Text;
            settings["endTime"].Value = dateTimePickerEndTime.Text;
            settings["messageInterval"].Value = maskedTextBoxMessageInterval.Text;
            settings["enableLogging"].Value = checkBoxEnableLogging.Checked.ToString();

            ConfigurationFile.Current.Save();
        }

        private void buttonGo_Click(object sender, EventArgs e)
        {
            string logFileName = null;

            if (checkBoxEnableLogging.Checked)
            {
                using (FileDialog fileDialog = new SaveFileDialog())
                {
                    fileDialog.Title = "Select Comparison Log File";
                    fileDialog.DefaultExt = "txt";
                    fileDialog.Filter = @"TXT files|*.txt|All files|*.*";

                    if (fileDialog.ShowDialog() != DialogResult.OK)
                        return;

                    logFileName = fileDialog.FileName;
                }
            }

            buttonGo.Enabled = false;
            ClearUpdateMessages();
            UpdateProgressBar(0);
            SetProgressMaximum(100);

            Dictionary<string, string> parameters = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            parameters["sourceHostAddress"] = textBoxSourceHistorianHostAddress.Text;
            parameters["sourceDataPort"] = maskedTextBoxSourceHistorianDataPort.Text;
            parameters["sourceMetaDataPort"] = maskedTextBoxSourceHistorianMetaDataPort.Text;
            parameters["sourceInstanceName"] = textBoxSourceHistorianInstanceName.Text;
            parameters["destinationHostAddress"] = textBoxDestinationHistorianHostAddress.Text;
            parameters["destinationDataPort"] = maskedTextBoxDestinationHistorianDataPort.Text;
            parameters["destinationMetaDataPort"] = maskedTextBoxDestinationHistorianMetaDataPort.Text;
            parameters["destinationInstanceName"] = textBoxDestinationHistorianInstanceName.Text;
            parameters["frameRate"] = maskedTextBoxFrameRate.Text;
            parameters["metaDataTimeout"] = maskedTextBoxMetaDataTimeout.Text;
            parameters["startTime"] = dateTimePickerSourceTime.Text;
            parameters["endTime"] = dateTimePickerEndTime.Text;
            parameters["messageInterval"] = maskedTextBoxMessageInterval.Text;
            parameters["enableLogging"] = checkBoxEnableLogging.Checked.ToString();
            parameters["logFileName"] = logFileName;

            Thread operation = new Thread(CompareArchives);
            operation.IsBackground = true;
            operation.Start(parameters);
        }

        private void CompareArchives(object state)
        {
            try
            {
                Ticks operationStartTime = DateTime.UtcNow.Ticks;
                Dictionary<string, string> parameters = state as Dictionary<string, string>;

                if ((object)parameters == null)
                    throw new ArgumentNullException(nameof(state), "Could not interpret thread state as parameters dictionary");

                string sourceHostAddress = parameters["sourceHostAddress"];
                int sourceDataPort = int.Parse(parameters["sourceDataPort"]);
                int sourceMetadataPort = int.Parse(parameters["sourceMetaDataPort"]);
                string sourceInstanceName = parameters["sourceInstanceName"];
                string destinationHostAddress = parameters["destinationHostAddress"];
                int destinationDataPort = int.Parse(parameters["destinationDataPort"]);
                int destinationMetaDataPort = int.Parse(parameters["destinationMetaDataPort"]);
                string destinationInstanceName = parameters["destinationInstanceName"];
                int frameRate = int.Parse(parameters["frameRate"]);
                int metaDataTimeout = int.Parse(parameters["metaDataTimeout"]) * 1000;
                ulong startTime = (ulong)DateTime.Parse(parameters["startTime"]).Ticks;
                ulong endTime = (ulong)DateTime.Parse(parameters["endTime"]).Ticks;
                int messageInterval = int.Parse(parameters["messageInterval"]);
                bool enableLogging = parameters["enableLogging"].ParseBoolean();
                string logFileName = parameters["logFileName"];

                ShowUpdateMessage("Loading source connection metadata...");
                List<Metadata> sourceMetadata = Metadata.Query(sourceHostAddress, sourceMetadataPort, metaDataTimeout);

                ShowUpdateMessage("Loading destination connection metadata...");
                List<Metadata> destinationMetadata = Metadata.Query(destinationHostAddress, destinationMetaDataPort, metaDataTimeout);

                Ticks totalTime = DateTime.UtcNow.Ticks - operationStartTime;
                ShowUpdateMessage("*** Metadata Load Complete ***");
                ShowUpdateMessage($"Total metadata load time {totalTime.ToElapsedTimeString(3)}...");

                ShowUpdateMessage("Analyzing metadata...");

                operationStartTime = DateTime.UtcNow.Ticks;

                Dictionary<ulong, ulong> sourcePointMappings = new Dictionary<ulong, ulong>();
                Dictionary<ulong, ulong> destinationPointMappings = new Dictionary<ulong, ulong>();
                Dictionary<ulong, string> pointDevices = new Dictionary<ulong, string>();

                StreamWriter writer = null;
                string logFileNameTemplate = $"{FilePath.GetDirectoryName(logFileName)}{FilePath.GetFileNameWithoutExtension(logFileName)}-{{0}}{FilePath.GetExtension(logFileName)}";

                if (enableLogging)
                    writer = new StreamWriter(FilePath.GetAbsolutePath(string.Format(logFileNameTemplate, "metadata")));

                AnalyzeMetadata(writer, startTime, endTime, sourceMetadata, destinationMetadata, sourcePointMappings, destinationPointMappings, pointDevices);

                TimeSpan range = new TimeSpan((long)(endTime- startTime));
                double timespan = range.TotalSeconds;
                long comparedPoints = 0;
                long validPoints = 0;
                long invalidPoints = 0;
                long missingSourcePoints = 0;
                long missingDestinationPoints = 0;
                long duplicateSourcePoints = 0;
                long duplicateDestinationPoints = 0;
                long processedPoints = 0;
                long displayMessageCount = messageInterval;

                const int MissingSourceValue = 0;
                const int MissingDestinationValue = 1;
                const int ValidValue = 2;
                const int InvalidValue = 3;
                const int ComparedValue = 4;
                const int DuplicateSourceValue = 5;
                const int DuplicateDestinationValue = 6;

                const int SourcePoint = 0;
                const int DestinationPoint = 1;

                Dictionary<ulong, Dictionary<int, long[]>> hourlySummaries = new Dictionary<ulong, Dictionary<int, long[]>>();  // PointID[HourIndex[ValueCount[7]]]
                Dictionary<ulong, DataPoint[]> dataBlock = new Dictionary<ulong, DataPoint[]>();                                // PointID[DataPoint[2]]
                DataPoint sourcePoint = new DataPoint();
                DataPoint destinationPoint = new DataPoint();
                DataPoint referencePoint = new DataPoint();
                Ticks readStartTime = DateTime.UtcNow.Ticks;

                Func<ulong, int> getHourIndex = timestamp => (int)Math.Truncate(new TimeSpan((long)(timestamp - startTime)).TotalHours);
                Func<ulong, Dictionary<int, long[]>> getHourlySummary = pointID => hourlySummaries.GetOrAdd(pointID, id => new Dictionary<int, long[]>());

                Action<DataPoint, int> incrementValueCount = (dataPoint, valueIndex) =>
                {
                    Dictionary<int, long[]> summary = getHourlySummary(dataPoint.PointID);
                    int hourIndex = getHourIndex(dataPoint.Timestamp);
                    long[] counts = summary.GetOrAdd(hourIndex, index => new long[7]);
                    counts[valueIndex] = counts[valueIndex] + 1;
                };

                ProcessQueue<Tuple<DataPoint, int>>.ProcessItemsFunctionSignature processActions = items =>
                {
                    foreach (Tuple<DataPoint, int> item in items)
                        incrementValueCount(item.Item1, item.Item2);
                };

                ProcessQueue<Tuple<DataPoint, int>> counterIncrements = ProcessQueue<Tuple<DataPoint, int>>.CreateRealTimeQueue(processActions);

                Action<DataPoint> logMissingSourceValue = dataPoint => counterIncrements.Add(new Tuple<DataPoint, int>(dataPoint.Clone(), MissingSourceValue));
                Action<DataPoint> logMissingDestinationValue = dataPoint => counterIncrements.Add(new Tuple<DataPoint, int>(dataPoint.Clone(), MissingDestinationValue));
                Action<DataPoint> logValidValue = dataPoint => counterIncrements.Add(new Tuple<DataPoint, int>(dataPoint.Clone(), ValidValue));
                Action<DataPoint> logInvalidValue = dataPoint => counterIncrements.Add(new Tuple<DataPoint, int>(dataPoint.Clone(), InvalidValue));
                Action<DataPoint> logComparedValue = dataPoint => counterIncrements.Add(new Tuple<DataPoint, int>(dataPoint.Clone(), ComparedValue));
                Action<DataPoint> logDuplicateSourceValue = dataPoint => counterIncrements.Add(new Tuple<DataPoint, int>(dataPoint.Clone(), DuplicateSourceValue));
                Action<DataPoint> logDuplicateDestinationValue = dataPoint => counterIncrements.Add(new Tuple<DataPoint, int>(dataPoint.Clone(), DuplicateDestinationValue));

                Func<DataPoint, DataPoint> getReferencePoint = dataPoint =>
                {
                    referencePoint.PointID = sourcePointMappings[dataPoint.PointID];
                    referencePoint.Timestamp = dataPoint.Timestamp;
                    return referencePoint;
                };

                ShowUpdateMessage("Comparing archives...");

                // Start counter incrementation queue
                counterIncrements.Start();

                using (SnapDBClient sourceClient = new SnapDBClient(sourceHostAddress, sourceDataPort, sourceInstanceName, startTime, endTime, frameRate, sourcePointMappings.Values))
                using (SnapDBClient destinationClient = new SnapDBClient(destinationHostAddress, destinationDataPort, destinationInstanceName, startTime, endTime, frameRate, destinationPointMappings.Values))
                {
                    // Scan to first record in source
                    if (!sourceClient.ReadNext(sourcePoint))
                        throw new InvalidOperationException("No data for specified time range in source connection!");

                    // Scan to first record in destination
                    if (!destinationClient.ReadNext(destinationPoint))
                        throw new InvalidOperationException("No data for specified time range in destination connection!");

                    while (true)
                    {
                        // Compare timestamps of current records
                        int timeComparison = DataPoint.CompareTimestamps(sourcePoint.Timestamp, destinationPoint.Timestamp, frameRate);
                        bool readSuccess = true;

                        // If timestamps do not match, synchronize starting times of source and destination datasets
                        while (readSuccess && timeComparison != 0)
                        {
                            if (timeComparison < 0)
                            {
                                // Destination has no data where source begins, scan source forward to match destination start time
                                do
                                {
                                    missingDestinationPoints++;

                                    if (enableLogging)
                                        logMissingDestinationValue(sourcePoint);

                                    if (!sourceClient.ReadNext(sourcePoint))
                                    {
                                        readSuccess = false;
                                        break;
                                    }
                                        
                                    timeComparison = DataPoint.CompareTimestamps(sourcePoint.Timestamp, destinationPoint.Timestamp, frameRate);
                                }
                                while (timeComparison < 0);
                            }
                            else // timeComparison > 0
                            {
                                // Source has no data where destination begins, scan destination forward to match source start time
                                do
                                {
                                    missingSourcePoints++;

                                    if (enableLogging)
                                        logMissingSourceValue(getReferencePoint(destinationPoint));

                                    if (!destinationClient.ReadNext(destinationPoint))
                                    {
                                        readSuccess = false;
                                        break;
                                    }

                                    timeComparison = DataPoint.CompareTimestamps(sourcePoint.Timestamp, destinationPoint.Timestamp, frameRate);
                                }
                                while (timeComparison > 0);
                            }
                        }

                        // Finished with data read
                        if (!readSuccess)
                        {
                            ShowUpdateMessage("*** End of data read encountered ***");
                            break;
                        }

                        // Read all time adjusted points for the current timestamp into a single block
                        ulong currentTimestamp = DataPoint.RoundTimestamp(sourcePoint.Timestamp, frameRate);                        
                        dataBlock.Clear();

                        // Load source data for current timestamp
                        do
                        {
                            if (!sourceClient.ReadNext(sourcePoint))
                            {
                                readSuccess = false;
                                break;
                            }

                            timeComparison = DataPoint.CompareTimestamps(sourcePoint.Timestamp, currentTimestamp, frameRate);

                            if (timeComparison == 0)
                            {
                                DataPoint[] points = dataBlock.GetOrAdd(sourcePoint.PointID, id => new DataPoint[2]);

                                if ((object)points[SourcePoint] != null)
                                {
                                    duplicateSourcePoints++;

                                    if (enableLogging)
                                        logDuplicateSourceValue(points[SourcePoint]);
                                }

                                points[SourcePoint] = sourcePoint.Clone();
                            }
                        }
                        while (timeComparison == 0);

                        // Finished with data read
                        if (!readSuccess)
                        {
                            ShowUpdateMessage("*** End of data read encountered ***");
                            break;
                        }

                        // Load destination data for current timestamp
                        do
                        {
                            if (!destinationClient.ReadNext(destinationPoint))
                            {
                                readSuccess = false;
                                break;
                            }

                            timeComparison = DataPoint.CompareTimestamps(destinationPoint.Timestamp, currentTimestamp, frameRate);

                            if (timeComparison == 0)
                            {
                                DataPoint[] points = dataBlock.GetOrAdd(sourcePointMappings[destinationPoint.PointID], id => new DataPoint[2]);

                                if ((object)points[DestinationPoint] != null)
                                {
                                    duplicateDestinationPoints++;

                                    if (enableLogging)
                                        logDuplicateDestinationValue(getReferencePoint(points[DestinationPoint]));
                                }

                                points[DestinationPoint] = destinationPoint.Clone();
                            }
                        }
                        while (timeComparison == 0);

                        // Finished with data read - destination is short of source read
                        if (!readSuccess)
                        {
                            ShowUpdateMessage("*** End of data read encountered: destination read was short of data available in source ***");
                            break;
                        }

                        // Analyze data block
                        foreach (DataPoint[] points in dataBlock.Values)
                        {
                            if ((object)points[SourcePoint] == null || (object)points[DestinationPoint] == null)
                            {
                                if ((object)points[SourcePoint] == null)
                                {
                                    missingSourcePoints++;

                                    if (enableLogging)
                                        logMissingSourceValue(getReferencePoint(points[DestinationPoint]));
                                }
                                else
                                {
                                    missingDestinationPoints++;

                                    if (enableLogging)
                                        logMissingDestinationValue(points[SourcePoint]);
                                }
                            }
                            else
                            {
                                if (points[SourcePoint].Value == points[DestinationPoint].Value)
                                {
                                    if (points[SourcePoint].Flags == points[DestinationPoint].Flags)
                                    {
                                        validPoints++;

                                        if (enableLogging)
                                            logValidValue(points[SourcePoint]);
                                    }
                                    else
                                    {
                                        invalidPoints++;

                                        if (enableLogging)
                                            logInvalidValue(points[SourcePoint]);
                                    }
                                }
                                else
                                {
                                    invalidPoints++;

                                    if (enableLogging)
                                        logInvalidValue(points[SourcePoint]);
                                }

                                comparedPoints++;

                                if (enableLogging)
                                    logComparedValue(points[SourcePoint]);
                            }

                            if (processedPoints++ == displayMessageCount)
                            {
                                if (processedPoints % (5 * messageInterval) == 0)
                                    ShowUpdateMessage($"{Environment.NewLine}*** Processed {processedPoints:N0} points so far averaging {processedPoints / (DateTime.UtcNow.Ticks - readStartTime).ToSeconds():N0} points per second ***{Environment.NewLine}");
                                else
                                    ShowUpdateMessage($"{Environment.NewLine}Found {validPoints:N0} valid, {invalidPoints:N0} invalid and {missingSourcePoints + missingDestinationPoints:N0} missing points during compare so far...{Environment.NewLine}");

                                displayMessageCount += messageInterval;

                                UpdateProgressBar((int)((1.0D - new Ticks((long)(endTime - sourcePoint.Timestamp)).ToSeconds() / timespan) * 100.0D));
                            }
                        }
                    }

                    if (m_formClosing)
                    {
                        ShowUpdateMessage("Comparison canceled.");
                        UpdateProgressBar(0);
                    }
                    else
                    {
                        totalTime = DateTime.UtcNow.Ticks - operationStartTime;
                        ShowUpdateMessage("*** Compare Complete ***");
                        ShowUpdateMessage($"Total compare time {totalTime.ToElapsedTimeString(3)} at {comparedPoints / totalTime.ToSeconds():N0} points per second.");
                        UpdateProgressBar(100);

                        ShowUpdateMessage("Completing count processing...");
                        counterIncrements.Flush();
                        ShowUpdateMessage("*** Count Processing Complete ***");

                        long expectedPoints = (long)(timespan * frameRate * sourceMetadata.Count);
                        long receivedPoints = comparedPoints + missingSourcePoints;

                        ShowUpdateMessage(
                            $"{Environment.NewLine}" +
                            $"           Meta-data points: {sourceMetadata.Count}{Environment.NewLine}" +
                            $"          Time-span covered: {timespan:N0} seconds: {Ticks.FromSeconds(timespan).ToElapsedTimeString(2)}{Environment.NewLine}" +
                            $"           Processed points: {processedPoints:N0}{Environment.NewLine}" +
                            $"            Compared points: {comparedPoints:N0}{Environment.NewLine}" +
                            $"               Valid points: {validPoints:N0}{Environment.NewLine}" +
                            $"             Invalid points: {invalidPoints:N0}{Environment.NewLine}" +
                            $"      Missing source points: {missingSourcePoints:N0}{Environment.NewLine}" +
                            $" Missing destination points: {missingDestinationPoints:N0}{Environment.NewLine}" +
                            $"          Source duplicates: {duplicateSourcePoints:N0}{Environment.NewLine}" +
                            $"     Destination duplicates: {duplicateDestinationPoints:N0}{Environment.NewLine}" +
                            $"{Environment.NewLine}" +
                            $"     Expected source points: {expectedPoints:N0}{Environment.NewLine}" +
                            $"     Received source points: {receivedPoints:N0}{Environment.NewLine}" +
                            $"     Base source point loss: {expectedPoints - receivedPoints:N0}: {100.0D - Math.Round(receivedPoints / (double)expectedPoints.NotZero(1) * 100000.0D) / 1000.0D:N3}%{Environment.NewLine}" +
                            $"              Data accuracy: {Math.Round(validPoints / (double)comparedPoints * 100000.0D) / 1000.0D:N3}%{Environment.NewLine}" +
                            $"{Environment.NewLine}" +
                            $">> {Math.Round(missingSourcePoints / (double)(comparedPoints + missingSourcePoints).NotZero(1) * 100000.0D) / 1000.0D:N3}% missing from source that exists in destination{Environment.NewLine}" +
                            $">> {Math.Round(missingDestinationPoints / (double)(comparedPoints + missingDestinationPoints).NotZero(1) * 100000.0D) / 1000.0D:N3}% missing from destination that exists in source{Environment.NewLine}");
                    }

                    if (enableLogging)
                    {
                        ShowUpdateMessage("Writing log files...");

                        int totalHours = getHourIndex(endTime) + 1;
                        Dictionary<string, Dictionary<int, long[]>> deviceHourlySummaries = new Dictionary<string, Dictionary<int, long[]>>();

                        Func<ulong, string> getPointTag = pointID => sourceMetadata.FirstOrDefault(metadata => metadata.PointID == pointID)?.PointTag ?? "Not Found";

                        using (writer = new StreamWriter(FilePath.GetAbsolutePath(string.Format(logFileNameTemplate, "detail"))))
                        {
                            writer.Write("Point ID, Point Tag");

                            for (int i = 0; i < totalHours; i++)
                                writer.Write($", Missing Source Points for Hour {i}, Missing Destination Points for Hour {i}, Valid Points for Hour {i}, Invalid Points for Hour {i}, Compared Points for Hour {i}, Duplicate Source Points for Hour {i}, Duplicate Destination Points for Hour {i}");

                            writer.WriteLine();

                            foreach (KeyValuePair<ulong, Dictionary<int, long[]>> item in hourlySummaries.OrderBy(kvp => kvp.Key))
                            {
                                ulong pointID = item.Key;
                                Dictionary<int, long[]> summaries = item.Value;

                                writer.Write($"{pointID}, {getPointTag(pointID)}");

                                for (int i = 0; i < totalHours; i++)
                                {
                                    long[] counts;

                                    if (summaries.TryGetValue(i, out counts))
                                    {
                                        string deviceName;

                                        writer.Write($", {counts[MissingSourceValue]}, {counts[MissingDestinationValue]}, {counts[ValidValue]}, {counts[InvalidValue]}, {counts[ComparedValue]}, {counts[DuplicateSourceValue]}, {counts[DuplicateDestinationValue]}");

                                        if (pointDevices.TryGetValue(pointID, out deviceName))
                                        {
                                            Dictionary<int, long[]> deviceSummaries = deviceHourlySummaries.GetOrAdd(deviceName, name => new Dictionary<int, long[]>());
                                            long[] deviceCounts = deviceSummaries.GetOrAdd(i, index => new long[5]);  // Just get first 5 values, ignoring duplicate stats in device summaries

                                            for (int j = 0; j < deviceCounts.Length; j++)
                                                deviceCounts[j] = deviceCounts[j] + counts[j];
                                        }
                                    }
                                    else
                                    {
                                        writer.Write(", 0, 0, 0, 0");
                                    }
                                }

                                writer.WriteLine();
                            }
                        }

                        using (writer = new StreamWriter(FilePath.GetAbsolutePath(string.Format(logFileNameTemplate, "summary"))))
                        {
                            writer.Write("Device Name");

                            for (int i = 0; i < totalHours; i++)
                                writer.Write($", Hour {i} % Source Loss, Hour {i} % Destination Loss");

                            writer.WriteLine();

                            foreach (KeyValuePair<string, Dictionary<int, long[]>> item in deviceHourlySummaries.OrderBy(kvp => kvp.Key))
                            {
                                string deviceName = item.Key;
                                Dictionary<int, long[]> summaries = item.Value;

                                writer.Write($"{deviceName}");

                                for (int i = 0; i < totalHours; i++)
                                {
                                    long[] counts;

                                    if (summaries.TryGetValue(i, out counts))
                                    {
                                        writer.Write($", {Math.Round(counts[MissingSourceValue] / (double)(counts[ComparedValue] + counts[MissingSourceValue]).NotZero(1) * 100000.0D) / 1000.0D:0.00}");
                                        writer.Write($", {Math.Round(counts[MissingDestinationValue] / (double)(counts[ComparedValue] + counts[MissingDestinationValue]).NotZero(1) * 100000.0D) / 1000.0D:0.00}");
                                    }
                                    else
                                    {
                                        writer.Write(", 100.00");
                                        writer.Write(", 100.00");
                                    }
                                }

                                writer.WriteLine();
                            }
                        }

                        ShowUpdateMessage("*** Log Files Complete ***");
                    }
                }
            }
            catch (Exception ex)
            {
                ShowUpdateMessage($"Failure during historian comparison: {ex.Message}");
            }
            finally
            {
                EnableGoButton(true);
            }
        }

        private static void AnalyzeMetadata(StreamWriter writer, ulong startTime, ulong endTime, List<Metadata> sourceMetadata, List<Metadata> destinationMetadata, Dictionary<ulong, ulong> sourcePointMappings, Dictionary<ulong, ulong> destinationPointMappings, Dictionary<ulong, string> pointDevices)
        {
            writer?.WriteLine($"Meta-data dump for archive comparison spanning {new DateTime((long)startTime):yyyy-MM-dd HH:mm:ss} to {new DateTime((long)endTime):yyyy-MM-dd HH:mm:ss}:");
            writer?.WriteLine();
            writer?.WriteLine($"     Source Meta-data: {sourceMetadata.Count:N0} records");
            writer?.WriteLine($"Destination Meta-data: {destinationMetadata.Count:N0} records");

            string lastDeviceName = "";

            // Create point ID cross reference dictionaries
            foreach (Metadata sourceRecord in sourceMetadata.OrderBy(record => record.DeviceName).ThenBy(record => record.PointID))
            {
                ulong sourcePointID = sourceRecord.PointID;
                Metadata destinationRecord = destinationMetadata.FirstOrDefault(record => GetRootTagName(sourceRecord.PointTag).Equals(GetRootTagName(record.PointTag), StringComparison.OrdinalIgnoreCase));
                ulong destinationPointID = destinationRecord?.PointID ?? 0;
                sourcePointMappings[destinationPointID] = sourcePointID;
                destinationPointMappings[sourcePointID] = destinationPointID;
                pointDevices[sourcePointID] = sourceRecord.DeviceName;

                if (!sourceRecord.DeviceName.Equals(lastDeviceName, StringComparison.OrdinalIgnoreCase))
                {
                    lastDeviceName = sourceRecord.DeviceName;
                    writer?.WriteLine();
                    writer?.WriteLine($"Measurements for device \"{lastDeviceName}\":");
                    writer?.WriteLine();
                }

                writer?.WriteLine($"Source \"{sourceRecord.PointTag}\" [{sourcePointID}] = Destination \"{destinationRecord?.PointTag}\" [{destinationPointID}]");
            }

            writer?.Dispose();
        }

        private static string GetRootTagName(string tagName)
        {
            int lastBangIndex = tagName.LastIndexOf('!');
            return lastBangIndex > -1 ? tagName.Substring(lastBangIndex + 1).Trim() : tagName.Trim();
        }

        private void EnableGoButton(bool enabled)
        {
            if (m_formClosing)
                return;

            if (InvokeRequired)
            {
                BeginInvoke(new Action<bool>(EnableGoButton), enabled);
            }
            else
            {
                buttonGo.Enabled = enabled;
            }
        }

        private void UpdateProgressBar(int value)
        {
            if (m_formClosing)
                return;

            if (InvokeRequired)
            {
                BeginInvoke(new Action<int>(UpdateProgressBar), value);
            }
            else
            {
                if (value < progressBar.Minimum)
                    value = progressBar.Minimum;

                if (value > progressBar.Maximum)
                    progressBar.Maximum = value;

                progressBar.Value = value;
            }
        }

        private void SetProgressMaximum(int maximum)
        {
            if (m_formClosing)
                return;

            if (InvokeRequired)
            {
                BeginInvoke(new Action<int>(SetProgressMaximum), maximum);
            }
            else
            {
                progressBar.Maximum = maximum;
            }
        }

        private void ClearUpdateMessages()
        {
            if (m_formClosing)
                return;

            if (InvokeRequired)
            {
                BeginInvoke(new Action(ClearUpdateMessages));
            }
            else
            {
                lock (textBoxMessageOutput)
                    textBoxMessageOutput.Text = "";
            }
        }

        internal void ShowUpdateMessage(string message)
        {
            if (m_formClosing)
                return;

            if (InvokeRequired)
            {
                BeginInvoke(new Action<string>(ShowUpdateMessage), message);
            }
            else
            {
                StringBuilder outputText = new StringBuilder();

                outputText.AppendLine(message);
                outputText.AppendLine();

                lock (textBoxMessageOutput)
                    textBoxMessageOutput.AppendText(outputText.ToString());
            }
        }

        static ComparisonUtility()
        {
            // Set default logging path
            GSF.Diagnostics.Logger.SetLoggingPath(FilePath.GetAbsolutePath(""));
        }
    }
}