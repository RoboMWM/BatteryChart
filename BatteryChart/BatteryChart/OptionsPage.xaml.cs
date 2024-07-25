using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
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
    public sealed partial class OptionsPage : Page
    {
        public OptionsPage()
        {
            //TODO: load from file
            this.InitializeComponent();
            SystemNavigationManager.GetForCurrentView().AppViewBackButtonVisibility = AppViewBackButtonVisibility.Visible;
        }

        private async void RegisterBackgroundTask()
        {
            switch (await BackgroundExecutionManager.RequestAccessAsync())
            {
                case BackgroundAccessStatus.DeniedBySystemPolicy:
                case BackgroundAccessStatus.DeniedByUser:
                    return; //TODO: error message
                default:
                    break;
            }

            //Unregister all tasks
            foreach (KeyValuePair<Guid, IBackgroundTaskRegistration> task in BackgroundTaskRegistration.AllTasks)
                task.Value.Unregister(true);

            //Register new background task exclusively for PowerStateChange
            //TODO implement other triggers here or in another method?
            BackgroundTaskBuilder taskBuilder = new BackgroundTaskBuilder();
            taskBuilder.Name = "LogAndUpdateTile";
            taskBuilder.TaskEntryPoint = "BackgroundTasks.LogAndUpdateTile";
            taskBuilder.SetTrigger(new SystemTrigger(SystemTriggerType.PowerStateChange, false));
            BackgroundTaskRegistration registration = taskBuilder.Register();
        }

        private void SaveAppBarButton(object sender, RoutedEventArgs e)
        {
            //TODO: how to handle button spam? Maybe show messagedialog confirming changes?
            RegisterBackgroundTask();
        }

        private void TriggersComboBox_Loaded(object sender, RoutedEventArgs e)
        {
            TriggersComboBox.SelectedItem = "Only plug/unplug";
        }
    }
}
