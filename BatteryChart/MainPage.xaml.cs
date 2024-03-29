﻿using Microsoft.Toolkit.Uwp.Notifications;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.ApplicationModel.Background;
using Windows.Devices.Enumeration;
using Windows.Devices.Power;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.UI.Core;
using Windows.UI.Notifications;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace BatteryChart
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        private BackgroundAccessStatus backgroundAccessStatus;
        private ApplicationTrigger backgroundManualTrigger = new ApplicationTrigger();
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            RegisterBackgroundTask();
        }

        private async void RegisterBackgroundTask()
        {
            BackgroundExecutionManager.RemoveAccess();
            await BackgroundExecutionManager.RequestAccessAsync();
            const string taskName = "BatteryTileBackgroundTask";
            const string taskEntryPoint = "BackgroundTasks.BatteryTileBackgroundTask";
            backgroundAccessStatus = await BackgroundExecutionManager.RequestAccessAsync();

            foreach (var task in BackgroundTaskRegistration.AllTasks)
            {
                if (task.Value.Name == taskName)
                {
                    task.Value.Unregister(true);
                }
            }

            BackgroundTaskBuilder taskBuilder = new BackgroundTaskBuilder();
            taskBuilder.Name = taskName;
            taskBuilder.TaskEntryPoint = taskEntryPoint;
            taskBuilder.SetTrigger(new SystemTrigger(SystemTriggerType.PowerStateChange, false));
            BackgroundTaskRegistration registration = taskBuilder.Register();

            BackgroundTaskBuilder taskBuilderTimer = new BackgroundTaskBuilder();
            taskBuilderTimer.Name = taskName;
            taskBuilderTimer.TaskEntryPoint = taskEntryPoint;
            taskBuilderTimer.SetTrigger(new TimeTrigger(15, false));
            taskBuilderTimer.Register();

            BackgroundTaskBuilder taskBuilderUnlock = new BackgroundTaskBuilder();
            taskBuilderUnlock.Name = taskName;
            taskBuilderUnlock.TaskEntryPoint = taskEntryPoint;
            taskBuilderUnlock.SetTrigger(new SystemTrigger(SystemTriggerType.UserPresent, false));
            taskBuilderUnlock.Register();

            //BackgroundTaskBuilder taskBuilderLock = new BackgroundTaskBuilder();
            //taskBuilderLock.Name = taskName;
            //taskBuilderLock.TaskEntryPoint = taskEntryPoint;
            //taskBuilderLock.SetTrigger(new SystemTrigger(SystemTriggerType.UserAway, false));
            //taskBuilderLock.Register();

            BackgroundTaskBuilder taskBuilderManualTrigger = new BackgroundTaskBuilder();
            taskBuilderManualTrigger.Name = taskName;
            taskBuilderManualTrigger.TaskEntryPoint = taskEntryPoint;
            taskBuilderManualTrigger.SetTrigger(backgroundManualTrigger);
            taskBuilderManualTrigger.Register();
        }

        private BatteryReport batteryReport;

        public MainPage()
        {
            this.InitializeComponent();
            Application.Current.Resuming += new EventHandler<Object>(ResumingListener);
            RequestAggregateBatteryReport();
        }

        private void ResumingListener(Object sender, Object e)
        {
            RequestAggregateBatteryReport();
        }


        private void GetBatteryReport(object sender, RoutedEventArgs e)
        {
            // Request aggregate battery report
            RequestAggregateBatteryReport();
        }

        private void RequestAggregateBatteryReport()
        {
            // Clear UI
            BatteryReportPanel.Children.Clear();

            Battery.AggregateBattery.ReportUpdated += AggregateBattery_ReportUpdated;

            // Create aggregate battery object
            var aggBattery = Battery.AggregateBattery;

            // Get report
            batteryReport = aggBattery.GetReport();

            // Update UI
            AddReportUIAsync(BatteryReportPanel, batteryReport, aggBattery.DeviceId);

            UpdateTileInfoAsync();

            // Log entry
            //LogReport(batteryReport);
        }


        private void AddReportUIAsync(StackPanel sp, BatteryReport report, string DeviceID)
        {
            // Create battery report UI
            TextBlock txt1 = new TextBlock { Text = "Device ID: " + DeviceID };
            txt1.FontSize = 15;
            txt1.Margin = new Thickness(0, 15, 0, 0);
            txt1.TextWrapping = TextWrapping.WrapWholeWords;

            TextBlock txt2 = new TextBlock { Text = "Battery status: " + report.Status.ToString() };
            txt2.FontStyle = Windows.UI.Text.FontStyle.Italic;
            txt2.Margin = new Thickness(0, 0, 0, 15);

            TextBlock txt3 = new TextBlock { Text = "Charge rate (mW): " + report.ChargeRateInMilliwatts.ToString() };
            TextBlock txt4 = new TextBlock { Text = "Design energy capacity (mWh): " + report.DesignCapacityInMilliwattHours.ToString() };
            TextBlock txt5 = new TextBlock { Text = "Fully-charged energy capacity (mWh): " + report.FullChargeCapacityInMilliwattHours.ToString() };
            TextBlock txt6 = new TextBlock { Text = "Remaining energy capacity (mWh): " + report.RemainingCapacityInMilliwattHours.ToString() };

            // Create energy capacity progress bar & labels
            TextBlock pbLabel = new TextBlock { Text = "Percent remaining energy capacity" };
            pbLabel.Margin = new Thickness(0, 10, 0, 5);
            pbLabel.FontFamily = new FontFamily("Segoe UI");
            pbLabel.FontSize = 11;

            ProgressBar pb = new ProgressBar();
            pb.Margin = new Thickness(0, 5, 0, 0);
            pb.Width = 200;
            pb.Height = 10;
            pb.IsIndeterminate = false;
            pb.HorizontalAlignment = HorizontalAlignment.Left;

            TextBlock pbPercent = new TextBlock();
            pbPercent.Margin = new Thickness(0, 5, 0, 10);
            pbPercent.FontFamily = new FontFamily("Segoe UI");
            pbLabel.FontSize = 11;

            // Disable progress bar if values are null
            if ((report.FullChargeCapacityInMilliwattHours == null) ||
                (report.RemainingCapacityInMilliwattHours == null))
            {
                pb.IsEnabled = false;
                pbPercent.Text = "N/A";
            }
            else
            {
                pb.IsEnabled = true;
                pb.Maximum = Convert.ToDouble(report.FullChargeCapacityInMilliwattHours);
                pb.Value = Convert.ToDouble(report.RemainingCapacityInMilliwattHours);
                pbPercent.Text = ((pb.Value / pb.Maximum) * 100).ToString("F2") + "%";
            }

            TextBlock accessStatusBlock = new TextBlock();
            accessStatusBlock.Text = backgroundAccessStatus.ToString();

            // Add controls to stackpanel
            sp.Children.Add(txt1);
            sp.Children.Add(txt2);
            sp.Children.Add(txt3);
            sp.Children.Add(txt4);
            sp.Children.Add(txt5);
            sp.Children.Add(txt6);
            sp.Children.Add(pbLabel);
            sp.Children.Add(pb);
            sp.Children.Add(pbPercent);
            sp.Children.Add(accessStatusBlock);
            foreach (var task in BackgroundTaskRegistration.AllTasks)
            {
                TextBlock bgBlock = new TextBlock();
                bgBlock.Text = task.Value.Name;
                sp.Children.Add(bgBlock);
            }
        }

        //TODO This is probably not good/blocking call?
        private void AddLogEntriesUIAsync()
        {
            TextBlock textBlock = new TextBlock { Text = retrieveLogAsync().Result };
            BatteryReportPanel.Children.Add(textBlock); //TODO: clear before you do this
        }

        async private void AggregateBattery_ReportUpdated(Battery sender, object args)
        {
            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                // Clear UI
                BatteryReportPanel.Children.Clear();

                // Request aggregate battery report
                RequestAggregateBatteryReport();
            });
        }

        private async System.Threading.Tasks.Task UpdateTileInfoAsync() //TODO add report as param instead of global variable laziness //TODO is that todo even possible?
        {
            //Update Tile (via background task)
            await backgroundManualTrigger.RequestAsync();
        }

        private async void LogReport(BatteryReport report)
        {
            StorageFolder localFolder = ApplicationData.Current.LocalFolder;
            try
            {
                StorageFile reportsFile = await localFolder.CreateFileAsync("reportsFile.txt", CreationCollisionOption.OpenIfExists);
                await FileIO.WriteTextAsync(reportsFile, report.RemainingCapacityInMilliwattHours.ToString());
            }
            catch (Exception e)
            {
                await new MessageDialog("error occurred when logging message", "uh oh spaghettio").ShowAsync();
            }
        }

        private async Task<string> retrieveLogAsync()
        {
            StorageFolder localFolder = ApplicationData.Current.LocalFolder;
            StorageFile reportsFile = await localFolder.GetFileAsync("reportsFile.txt");
            return await FileIO.ReadTextAsync(reportsFile);
        }

        private void LogsButton(object sender, RoutedEventArgs e)
        {
            //AddLogEntriesUIAsync();
        }
    }
}
