using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading.Tasks;
using Windows.ApplicationModel.Background;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace BatteryChart
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class SettingsPage : Page
    {
        private BackgroundAccessStatus backgroundAccessStatus;

        private async Task<bool> IsBackgroundAccessAllowed()
        {
            if (this.backgroundAccessStatus != BackgroundAccessStatus.Unspecified)
                this.backgroundAccessStatus = await BackgroundExecutionManager.RequestAccessAsync();

            switch (this.backgroundAccessStatus)
            {
                case BackgroundAccessStatus.DeniedBySystemPolicy:
                case BackgroundAccessStatus.DeniedByUser:
                    return false; //TODO: error message(?)
                default:
                    return true;
            }
        }

        public SettingsPage()
        {
            //TODO: load from file
            this.InitializeComponent();
            SystemNavigationManager.GetForCurrentView().AppViewBackButtonVisibility = AppViewBackButtonVisibility.Visible;
        }

        private async Task<bool> RegisterBackgroundTask(params SystemTrigger[] triggers)
        {
            if (!await IsBackgroundAccessAllowed())
                return false;

            //Unregister all tasks
            //Will not be viable when we start registering different tasks for updating the tile
            foreach (KeyValuePair<Guid, IBackgroundTaskRegistration> task in BackgroundTaskRegistration.AllTasks)
                task.Value.Unregister(true);

            //Register each trigger
            foreach (SystemTrigger trigger in triggers)
            {
                BackgroundTaskBuilder taskBuilder = new BackgroundTaskBuilder();
                taskBuilder.Name = trigger.TriggerType.ToString();
                taskBuilder.TaskEntryPoint = "BackgroundTasks.LogBatteryToFileTask";
                taskBuilder.SetTrigger(trigger);
                taskBuilder.Register();
            }

            return true;
        }

        private async void TriggersComboBox_Loaded(object sender, RoutedEventArgs e)
        {
            if (!await IsBackgroundAccessAllowed())
            {
                TriggersComboBox.PlaceholderText = "Background access denied";
                return;
            }

            //All this to avoid a config file
            bool power = false;
            bool userPresentAway = false;

            foreach (KeyValuePair<Guid, IBackgroundTaskRegistration> task in BackgroundTaskRegistration.AllTasks)
            {
                if (task.Value.Name == SystemTriggerType.PowerStateChange.ToString())
                    power = true;
                else if (task.Value.Name == SystemTriggerType.UserPresent.ToString() || task.Value.Name == SystemTriggerType.UserAway.ToString())
                    userPresentAway = true;
            }

            if (power && userPresentAway)
                TriggersComboBox.SelectedItem = "All Events";
            else if (power)
                TriggersComboBox.SelectedItem = "Only plug/unplug";
            else if (userPresentAway)
                TriggersComboBox.SelectedItem = "Only wake/sleep";
            else
                TriggersComboBox.SelectedItem = "None";
        }

        private async void ApplyButton_Click(object sender, RoutedEventArgs e)
        {
            ApplyProgressRing.IsActive = true;

            switch (TriggersComboBox.SelectedItem)
            {
                case "None":
                    if (!await RegisterBackgroundTask())
                        return;
                    break;
                case "Only plug/unplug":
                    if (!await RegisterBackgroundTask(new SystemTrigger(SystemTriggerType.PowerStateChange, false)))
                        return; //TODO: print error and clear progress ring
                    break;
                case "Only wake/sleep":
                    if (!await RegisterBackgroundTask(new SystemTrigger(SystemTriggerType.UserPresent, false), new SystemTrigger(SystemTriggerType.UserAway, false)))
                        return; //TODO: print error and clear progress ring
                    break;
            }

            ApplyProgressRing.IsActive = false;
        }
    }
}
