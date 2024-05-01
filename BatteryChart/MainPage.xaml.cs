using Microsoft.Toolkit.Uwp.Notifications;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading.Tasks;
using Windows.ApplicationModel.Background;
using Windows.Devices.Enumeration;
using Windows.Devices.Power;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.UI.Core;
using Windows.UI.Notifications;
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
        private ApplicationTrigger backgroundManualTrigger;
        private string unregisteredTasks = "";
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            RegisterBackgroundTask();
        }

        private async void RegisterBackgroundTask()
        {
            BackgroundExecutionManager.RemoveAccess();
            await BackgroundExecutionManager.RequestAccessAsync();
            const string taskName = "BTBT"; //BatteryTileBackgroundTask
            const string taskEntryPoint = "BackgroundTasks.BatteryTileBackgroundTask";
            backgroundAccessStatus = await BackgroundExecutionManager.RequestAccessAsync();

            backgroundManualTrigger = new ApplicationTrigger();

            StringBuilder sb = new StringBuilder();
            try
            {
                Debug.Write("unregistering background tasks");
                foreach (var task in BackgroundTaskRegistration.AllTasks)
                {
                    //if (task.Value.Name == taskName)
                    {
                        Debug.WriteLine(task.Value.Name);
                        sb.AppendLine(task.Value.Name);
                        task.Value.Unregister(true);
                    }
                }
            }
            catch (Exception e)
            {
                SendToast("asynclaunch " + e.Message);
            }

            unregisteredTasks = sb.ToString();


            BackgroundTaskBuilder taskBuilder = new BackgroundTaskBuilder();
            taskBuilder.Name = "powerstate" + taskName;
            taskBuilder.TaskEntryPoint = taskEntryPoint;
            taskBuilder.SetTrigger(new SystemTrigger(SystemTriggerType.PowerStateChange, false));
            BackgroundTaskRegistration registration = taskBuilder.Register();

            BackgroundTaskBuilder taskBuilderTimer = new BackgroundTaskBuilder();
            taskBuilderTimer.Name = "time" + taskName;
            taskBuilderTimer.TaskEntryPoint = taskEntryPoint;
            taskBuilderTimer.SetTrigger(new TimeTrigger(15, false));
            taskBuilderTimer.Register();

            BackgroundTaskBuilder taskBuilderUnlock = new BackgroundTaskBuilder();
            taskBuilderUnlock.Name = "userpresent" + taskName;
            taskBuilderUnlock.TaskEntryPoint = taskEntryPoint;
            taskBuilderUnlock.SetTrigger(new SystemTrigger(SystemTriggerType.UserPresent, false));
            taskBuilderUnlock.Register();

            BackgroundTaskBuilder taskBuilderLock = new BackgroundTaskBuilder();
            taskBuilderLock.Name = "useraway" + taskName;
            taskBuilderLock.TaskEntryPoint = taskEntryPoint;
            taskBuilderLock.SetTrigger(new SystemTrigger(SystemTriggerType.UserAway, false));
            taskBuilderLock.Register();

            BackgroundTaskBuilder taskBuilderManualTrigger = new BackgroundTaskBuilder();
            taskBuilderManualTrigger.Name = "manual" + taskName;
            taskBuilderManualTrigger.TaskEntryPoint = taskEntryPoint;
            taskBuilderManualTrigger.SetTrigger(backgroundManualTrigger);
            taskBuilderManualTrigger.Register();
        }

        private BatteryReport batteryReport;

        public MainPage()
        {
            this.InitializeComponent();
            Application.Current.Resuming += new EventHandler<Object>(ResumingListener);
            RequestAggregateBatteryReport("initialized");
        }

        private void ResumingListener(Object sender, Object e)
        {
            RequestAggregateBatteryReport("resuming");
        }


        private void GetBatteryReport(object sender, RoutedEventArgs e)
        {
            // Request aggregate battery report
            RequestAggregateBatteryReport("button");
        }

        private void RequestAggregateBatteryReport(string reason)
        {
            // Clear UI
            BatteryReportPanel.Children.Clear();

            Battery.AggregateBattery.ReportUpdated += AggregateBattery_ReportUpdated; //????? I think this is the cause of app freezing on resuming... //Update: yes it is lol. For some reason this method will get called a bazillion times on resume and hang/crash the app. //Ok I looked this up some more, and this is supposed to be only called once to register for report updates... So idk what example I used, it was not correct to do this. Also good reason why you gotta learn the code you're writing/using

            // Create aggregate battery object
            var aggBattery = Battery.AggregateBattery;

            // Get report
            batteryReport = aggBattery.GetReport();

            // Update UI
            AddReportUIAsync(BatteryReportPanel, batteryReport, aggBattery.DeviceId);

            UpdateTileInfoAsync(reason);
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
            TextBlock txt7 = new TextBlock { Text = "Unregistered: " + unregisteredTasks };

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
            sp.Children.Add(txt7);
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

        async private void AggregateBattery_ReportUpdated(Battery sender, object args)
        {
            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                // Clear UI
                BatteryReportPanel.Children.Clear();

                // Request aggregate battery report
                RequestAggregateBatteryReport("async updated???"); //very confused what I'm doing here
            });
        }

        private async System.Threading.Tasks.Task UpdateTileInfoAsync(string reason) //TODO add report as param instead of global variable laziness
        {
            //Update Tile (via background task)
            ValueSet reasonSet = new ValueSet();
            reasonSet.Add("reason", reason);
            await backgroundManualTrigger.RequestAsync(reasonSet);
        }

        private async void SetLogBlockAsync()
        {
            try
            {
                StorageFolder localFolder = ApplicationData.Current.LocalFolder;
                StorageFile sampleFile = await localFolder.GetFileAsync("dataFile.txt");
                LogBlock.Text = await FileIO.ReadTextAsync(sampleFile);
            }
            catch (Exception e)
            {
                LogBlock.Text = e.Message;
            }

        }

        private void AppBarButton_Click(object sender, RoutedEventArgs e)
        {
            SetLogBlockAsync();
        }

        /// <summary>
        /// Simple method to show a basic toast with a message.
        /// </summary>
        /// <param name="message"></param>
        private void SendToast(string message)
        {
            ToastContent content = new ToastContent()
            {
                Visual = new ToastVisual()
                {
                    BindingGeneric = new ToastBindingGeneric()
                    {
                        Children =
                        {
                            new AdaptiveText()
                            {
                                Text = message
                            }
                        }
                    }
                },

                //Audio = new ToastAudio()
                //{
                //    Src = new Uri(sound)
                //}
            };

            ToastNotification toast = new ToastNotification(content.GetXml());
            //toast.ExpirationTime = DateTime.Now.AddSeconds(600);

            ToastNotificationManager.CreateToastNotifier().Show(toast);
        }
    }
}
