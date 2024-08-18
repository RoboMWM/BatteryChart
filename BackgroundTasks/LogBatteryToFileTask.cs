﻿using BatteryChart;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.ApplicationModel.Background;
using Windows.Devices.Power;
using Windows.Storage;

namespace BackgroundTasks
{
    public sealed class LogBatteryToFileTask : IBackgroundTask
    {
        BackgroundTaskDeferral deferral;
        public async void Run(IBackgroundTaskInstance taskInstance)
        {
            deferral = taskInstance.GetDeferral();
            //TODO: better concurrency handling? Create new file, and combine logs when in-app?

            string dateTime = DateTime.Now.ToString();
            BatteryReport batteryReport = Battery.AggregateBattery.GetReport();
            string state = batteryReport.Status.ToString();
            Nullable<int> remainingMwh = batteryReport.RemainingCapacityInMilliwattHours;
            string remaining;
            if (remainingMwh != null)
                remaining = remainingMwh.ToString();
            else
                remaining = "N/A";

            StringBuilder sb = new StringBuilder();
            sb.Append(dateTime);
            sb.Append(",");
            sb.Append(state);
            sb.Append(",");
            sb.Append(remaining);
            sb.Append(",");
            sb.AppendLine(taskInstance.Task.Name.ToString());

            await LogFileStuff.WriteToFile("BatteryLog.csv", sb.ToString());

            deferral.Complete();
        }
    }
}
