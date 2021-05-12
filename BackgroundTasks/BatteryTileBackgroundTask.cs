using Microsoft.Toolkit.Uwp.Notifications;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.ApplicationModel.Background;
using Windows.Devices.Power;
using Windows.UI.Notifications;

namespace BackgroundTasks
{
    public sealed class BatteryTileBackgroundTask : IBackgroundTask
    {
        public void Run(IBackgroundTaskInstance taskInstance)
        {
            //BackgroundTaskDeferral deferral = taskInstance.GetDeferral();

            UpdateTileInfo(Battery.AggregateBattery.GetReport());
            SendToast("background task executed");

            //deferral.Complete();
        }

        private void UpdateTileInfo(BatteryReport batteryReport) //TODO add report as param instead of global variable laziness
        {
            string from = ((Convert.ToDouble(batteryReport.RemainingCapacityInMilliwattHours) / Convert.ToDouble(batteryReport.FullChargeCapacityInMilliwattHours)) * 100).ToString("F2") + " % ";

            string subject = batteryReport.Status.ToString();
            string body = batteryReport.ChargeRateInMilliwatts.ToString() + "mW";


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
                                    HintStyle = AdaptiveTextStyle.CaptionSubtle
                                },

                                new AdaptiveText()
                                {
                                    Text = body,
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
    }
}
