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
        private Guid taskId;
        private Windows.Globalization.DateTimeFormatting.DateTimeFormatter formatter;
        private string timeStamp;

        public void Run(IBackgroundTaskInstance taskInstance)
        {
            BackgroundTaskDeferral deferral = taskInstance.GetDeferral();
            formatter = new Windows.Globalization.DateTimeFormatting.DateTimeFormatter("longtime");
            timeStamp = formatter.Format(DateTime.Now);
            taskId = taskInstance.InstanceId;

            UpdateTileInfo(Battery.AggregateBattery.GetReport(), taskInstance);

            StringBuilder sb = new StringBuilder();
            sb.Append(timeStamp);
            if (taskInstance.TriggerDetails != null)
            {
                ApplicationTriggerDetails details = (ApplicationTriggerDetails)taskInstance.TriggerDetails;
                sb.AppendLine(details.Arguments["reason"].ToString());
            }
            
            sb.AppendLine(taskInstance.Task.Name + taskId);
            
            //sb.Append(taskInstance.TriggerDetails.ToString());
            //sb.Append(Environment.NewLine);
            WriteToFile("dataFile.txt", sb.ToString(), deferral);

            //deferral.Complete(); //Maybe this is being called prematurely?
        }

        private void UpdateTileInfo(BatteryReport batteryReport, IBackgroundTaskInstance taskInstance) //TODO add report as param instead of global variable laziness
        {
            string from = ((Convert.ToDouble(batteryReport.RemainingCapacityInMilliwattHours) / Convert.ToDouble(batteryReport.FullChargeCapacityInMilliwattHours)) * 100).ToString("F2") + " % ";

            string subject = batteryReport.Status.ToString();
            string body = batteryReport.ChargeRateInMilliwatts.ToString() + "mW\n" + DateTime.Now.ToString();
            string footer = taskInstance.Task.Name;


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
                                    Text = from
                                },

                                new AdaptiveText()
                                {
                                    Text = subject,
                                    //HintStyle = AdaptiveTextStyle.CaptionSubtle
                                },

                                new AdaptiveText()
                                {
                                    Text = body,
                                    //HintStyle = AdaptiveTextStyle.CaptionSubtle
                                },

                                new AdaptiveText()
                                {
                                    Text = footer,
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
                                    Text = from,
                                    HintStyle = AdaptiveTextStyle.Subtitle
                                },

                                new AdaptiveText()
                                {
                                    Text = subject,
                                    HintStyle = AdaptiveTextStyle.CaptionSubtle
                                },

                                new AdaptiveText()
                                {
                                    Text = body,
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
                    SendToast("CreateFile failed. ID: " + taskId + " t: " + timeStamp + " e: " + e.Message);
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
                    SendToast("WriteText failed. ID " + taskId + " t: " + timeStamp + " e: " + e.Message);
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
