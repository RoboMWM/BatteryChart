using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using Windows.Devices.Power;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.System.Power;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409
// TODO features cuz I'm too lazy to use issue tracking rn:
// - Reminder/alert to plug in device if battery is too low of a percentage at night

namespace BatteryChart
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        public MainPage()
        {
            this.InitializeComponent();
            SystemNavigationManager.GetForCurrentView().AppViewBackButtonVisibility = AppViewBackButtonVisibility.Collapsed;
            LoadBatteryInfo();
        }

        private void SettingsAppBarButtonClick(object sender, RoutedEventArgs e)
        {
            this.Frame.Navigate(typeof(SettingsPage), null);
        }

        private void ViewLogsAppBarButton_Click(object sender, RoutedEventArgs e)
        {
            this.Frame.Navigate(typeof(ViewLogsPage), null);
        }

        private void LoadBatteryInfo()
        {
            BatteryReport batteryReport = Battery.AggregateBattery.GetReport();
            Nullable<int> remainingMwh = batteryReport.RemainingCapacityInMilliwattHours;
            if (remainingMwh == null || batteryReport.Status == BatteryStatus.NotPresent)
            {
                BatteryInfoTextBlock.Text = "No battery detected.";
                return;
            }

            StringBuilder sb = new StringBuilder();
            sb.Append("Last updated ");
            sb.Append(DateTime.Now.ToString());
            BatteryInfoTextBlock.Text = sb.ToString();

            TextBlock txt2 = new TextBlock { Text = "Current state: " + batteryReport.Status.ToString() };
            txt2.FontStyle = Windows.UI.Text.FontStyle.Italic;
            txt2.Margin = new Thickness(0, 0, 0, 15);

            TextBlock txt3 = new TextBlock { Text = "Charge rate (mW): " + batteryReport.ChargeRateInMilliwatts.ToString() };
            TextBlock txt4 = new TextBlock { Text = "Design energy capacity (mWh): " + batteryReport.DesignCapacityInMilliwattHours.ToString() };
            TextBlock txt5 = new TextBlock { Text = "Fully-charged energy capacity (mWh): " + batteryReport.FullChargeCapacityInMilliwattHours.ToString() };
            TextBlock txt6 = new TextBlock { Text = "Remaining energy capacity (mWh): " + batteryReport.RemainingCapacityInMilliwattHours.ToString() };

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
            if ((batteryReport.FullChargeCapacityInMilliwattHours == null) ||
                (batteryReport.RemainingCapacityInMilliwattHours == null))
            {
                pb.IsEnabled = false;
                pbPercent.Text = "N/A";
            }
            else
            {
                pb.IsEnabled = true;
                pb.Maximum = Convert.ToDouble(batteryReport.FullChargeCapacityInMilliwattHours);
                pb.Value = Convert.ToDouble(batteryReport.RemainingCapacityInMilliwattHours);
                pbPercent.Text = ((pb.Value / pb.Maximum) * 100).ToString("F2") + "%";
            }

            // Add controls to stackpanel
            BatteryInfoStackPanel.Children.Add(txt2);
            BatteryInfoStackPanel.Children.Add(txt3);
            BatteryInfoStackPanel.Children.Add(txt4);
            BatteryInfoStackPanel.Children.Add(txt5);
            BatteryInfoStackPanel.Children.Add(txt6);
            BatteryInfoStackPanel.Children.Add(pbLabel);
            BatteryInfoStackPanel.Children.Add(pb);
            BatteryInfoStackPanel.Children.Add(pbPercent);
        }
    }
}
