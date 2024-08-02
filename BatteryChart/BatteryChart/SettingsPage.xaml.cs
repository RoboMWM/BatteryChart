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
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.UI.Core;
using Windows.UI.Popups;
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
                    sendMessageDialog("Background tasks denied by system. Manually set BatteryChart to \"Always allowed\" in Battery Saver.");
                    return false;
                case BackgroundAccessStatus.DeniedByUser:
                    sendMessageDialog("You denied BatteryChart from running in the background. Background tasks can't be registered until you change this setting in Battery Saver.");
                    return false;
                default:
                    return true;
            }
        }

        public SettingsPage()
        {
            this.InitializeComponent();
            SystemNavigationManager.GetForCurrentView().AppViewBackButtonVisibility = AppViewBackButtonVisibility.Visible;
            loadCheckboxes();
        }

        //will also print messages to the user if unable to register
        //TODO timer is not a SystemTrigger
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

        private void loadCheckboxes()
        {
            foreach (KeyValuePair<Guid, IBackgroundTaskRegistration> task in BackgroundTaskRegistration.AllTasks)
            {
                if (task.Value.Name == SystemTriggerType.PowerStateChange.ToString())
                    PowerstateCheckbox.IsChecked = true;
                else if (task.Value.Name == SystemTriggerType.UserAway.ToString())
                    UserAwayCheckBox.IsChecked = true;
                else if (task.Value.Name == SystemTriggerType.UserPresent.ToString())
                    UserPresentCheckBox.IsChecked = true;
            }
        }

        private async void ApplyButton_Click(object sender, RoutedEventArgs e)
        {
            ApplyButton.IsEnabled = false;
            ApplyProgressRing.IsActive = true;

            List<SystemTrigger> tasksToRegister = new List<SystemTrigger>();

            if (PowerstateCheckbox.IsChecked == true)
                tasksToRegister.Add(new SystemTrigger(SystemTriggerType.PowerStateChange, false));
            if (UserAwayCheckBox.IsChecked == true)
                tasksToRegister.Add(new SystemTrigger(SystemTriggerType.UserAway, false));
            if (UserPresentCheckBox.IsChecked == true)
                tasksToRegister.Add(new SystemTrigger(SystemTriggerType.UserPresent, false));
                
            if (await RegisterBackgroundTask(tasksToRegister.ToArray()))
            {
                new MessageDialog("Successfully registered").ShowAsync(); //TODO: use a checkmark ui instead, especially since this doesn't account for unregistering
            }

            ApplyProgressRing.IsActive = false;
            ApplyButton.IsEnabled = true;
        }

        private async void sendMessageDialog(string message)
        {
            MessageDialog messageDialog = new MessageDialog(message);
            UICommand goToSystemSettingsCommand = new UICommand("Go to system settings") { Id = 0 };
            messageDialog.Commands.Add(goToSystemSettingsCommand);
            messageDialog.Commands.Add(new UICommand("Dismiss") { Id = 1 });
            messageDialog.DefaultCommandIndex = 1;
            messageDialog.CancelCommandIndex = 1;

            IUICommand command = await messageDialog.ShowAsync();
            //if (command == goToSystemSettingsCommand)
            //    DeleteLogs();
        }

        
    }
}
