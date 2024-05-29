using Microsoft.Toolkit.Uwp.Notifications;
using System;
using System.Text;
using System.Threading;
using Windows.ApplicationModel.AppService;
using Windows.ApplicationModel.Background;
using Windows.Devices.Power;
using Windows.Storage;
using Windows.UI.Notifications;

namespace BackgroundTasks
{
    public sealed class BatteryTileBackgroundTask : IBackgroundTask
    {
        private string taskName;
        private Windows.Globalization.DateTimeFormatting.DateTimeFormatter formatter;
        private string timeStamp;

        public void Run(IBackgroundTaskInstance taskInstance)
        {
            BackgroundTaskDeferral deferral = taskInstance.GetDeferral();
            formatter = new Windows.Globalization.DateTimeFormatting.DateTimeFormatter("longtime");
            timeStamp = formatter.Format(DateTime.Now);
            taskName = taskInstance.Task.Name;

            BatteryReport batteryReport = Battery.AggregateBattery.GetReport();

            string percentage = ((Convert.ToDouble(batteryReport.RemainingCapacityInMilliwattHours) / Convert.ToDouble(batteryReport.FullChargeCapacityInMilliwattHours)) * 100).ToString("F2") + " % ";
            string state = batteryReport.Status.ToString();
            string mWh = batteryReport.ChargeRateInMilliwatts.ToString() + "mW\n";
            string reason = "";
            if (taskInstance.TriggerDetails != null)
            {
                ApplicationTriggerDetails details = (ApplicationTriggerDetails)taskInstance.TriggerDetails;
                reason = details.Arguments["reason"].ToString();
            }

            StringBuilder sb = new StringBuilder();
            sb.Append(percentage);
            sb.Append(',');
            sb.Append(timeStamp);
            sb.Append(',');
            sb.Append(taskName);
            sb.Append(',');
            sb.AppendLine(reason);

            UpdateTileInfo(percentage, state, mWh, reason + taskName, timeStamp);

            //sb.Append(taskInstance.TriggerDetails.ToString());
            //sb.Append(Environment.NewLine);
            WriteToFile("dataFile.txt", sb.ToString(), deferral);

            //deferral.Complete(); //Maybe this is being called prematurely?
        }

        private void UpdateTileInfo(string percentage, string state, string mWh, string taskName, string timeStamp) //TODO add report as param instead of global variable laziness
        {





            // Construct the tile content
            TileContent content = new TileContent()
            {
                Visual = new TileVisual()
                {
                    TileMedium = new TileBinding()
                    {
                        Content = new TileBindingContentAdaptive()
                        {
                            Children =
                            {
                                new AdaptiveText()
                                {
                                    Text = percentage,
                                    HintStyle = AdaptiveTextStyle.Header
                                },

                                new AdaptiveText()
                                {
                                    Text = state
                                    //HintStyle = AdaptiveTextStyle.CaptionSubtle
                                },

                                new AdaptiveText()
                                {
                                    Text = timeStamp
                                    //HintStyle = AdaptiveTextStyle.CaptionSubtle
                                },

                                new AdaptiveText()
                                {
                                    Text = taskName
                                    //HintStyle = AdaptiveTextStyle.CaptionSubtle
                                },

                                new AdaptiveText()
                                {
                                    Text = mWh,
                                    HintStyle = AdaptiveTextStyle.CaptionSubtle
                                }
                            }
                        }
                    },

                    TileWide = new TileBinding()
                    {
                        Content = new TileBindingContentAdaptive()
                        {
                            Children =
                            {
                                new AdaptiveText()
                                {
                                    Text = percentage,
                                    HintStyle = AdaptiveTextStyle.Header
                                },

                                new AdaptiveText()
                                {
                                    Text = state
                                    //HintStyle = AdaptiveTextStyle.CaptionSubtle
                                },

                                new AdaptiveText()
                                {
                                    Text = timeStamp
                                    //HintStyle = AdaptiveTextStyle.CaptionSubtle
                                },

                                new AdaptiveText()
                                {
                                    Text = taskName
                                    //HintStyle = AdaptiveTextStyle.CaptionSubtle
                                },

                                new AdaptiveText()
                                {
                                    Text = mWh,
                                    HintStyle = AdaptiveTextStyle.CaptionSubtle
                                }
                            }
                        }
                    }
                }
            };

            TileNotification notification = new TileNotification(content.GetXml());
            notification.ExpirationTime = DateTimeOffset.UtcNow.AddMinutes(15);
            TileUpdateManager.CreateTileUpdaterForApplication().Update(notification);
        }

        /// <summary>
        /// Send silent toast
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

                Audio = new ToastAudio()
                {
                    Silent = true
                }
            };

            ToastNotification toast = new ToastNotification(content.GetXml());
            //toast.ExpirationTime = DateTime.Now.AddSeconds(600);

            ToastNotificationManager.CreateToastNotifier().Show(toast);
        }

        private async void WriteToFile(string fileName, string content, BackgroundTaskDeferral deferral)
        {
            try
            {
                StorageFolder localFolder = ApplicationData.Current.LocalFolder;
                StorageFile file = null;

                try
                {
                    file = await localFolder.CreateFileAsync(fileName, CreationCollisionOption.OpenIfExists);
                }
                catch (Exception e)
                {
                    SendToast("CreateFile failed. ID: " + taskName + " t: " + timeStamp + " e: " + e.Message);
                }

                if (file == null)
                    return;

                try
                {
                    //await sSlim.WaitAsync();
                    await FileIO.WriteTextAsync(file, content + await FileIO.ReadTextAsync(file));
                }
                catch (Exception e)
                {
                    SendToast("WriteText failed. ID " + taskName + " t: " + timeStamp + " e: " + e.Message);
                }
            }

            catch (Exception e)
            {
                SendToast("why" + e.Message);
            }

            deferral.Complete();
        }
    }
}
