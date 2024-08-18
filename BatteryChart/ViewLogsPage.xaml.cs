using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
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
    public sealed partial class ViewLogsPage : Page
    {
        public ViewLogsPage()
        {
            this.InitializeComponent();
            SystemNavigationManager.GetForCurrentView().AppViewBackButtonVisibility = AppViewBackButtonVisibility.Visible;
            LoadLogs();
        }

        private async void LoadLogs()
        {
            StorageFile logFile = await GetLogAsync("BatteryLog.csv");
            if (logFile == null) //TODO or is empty
            {
                LogTextBlock.Text = "No logs currently recorded. Register events to log data in settings.";
                return;
            }
            LogTextBlock.Text = await FileIO.ReadTextAsync(logFile);
        }

        private async void ExportAppBarButton_Click(object sender, RoutedEventArgs e)
        {
            FileSavePicker savePicker = new FileSavePicker();
            savePicker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;
            savePicker.FileTypeChoices.Add("Comma-Delimited Values", new List<string>() { ".csv" });
            savePicker.SuggestedFileName = "BatteryLogExport"; //TODO append datetime

            StorageFile logFile = await GetLogAsync("BatteryLog.csv");
            if (logFile == null)
            {
                await new MessageDialog("Log file is empty").ShowAsync();
                return;
            }

            StorageFile fileToSaveTo = await savePicker.PickSaveFileAsync();
            if (fileToSaveTo == null)
                return;

            CachedFileManager.DeferUpdates(fileToSaveTo);
            await FileIO.WriteTextAsync(fileToSaveTo, await FileIO.ReadTextAsync(logFile));
            if (await CachedFileManager.CompleteUpdatesAsync(fileToSaveTo) != Windows.Storage.Provider.FileUpdateStatus.Complete)
                await new MessageDialog("File could not be saved").ShowAsync();
            //TODO: success message
        }

        private async void DeleteAppBarButton_Click(object sender, RoutedEventArgs e)
        {
            MessageDialog deleteDialog = new MessageDialog("Are you sure you want to delete all log entries?");
            UICommand deleteCommand = new UICommand("Delete") { Id = 0 };
            deleteDialog.Commands.Add(deleteCommand);
            deleteDialog.Commands.Add(new UICommand("Cancel") { Id = 1 });
            deleteDialog.DefaultCommandIndex = 1;
            deleteDialog.CancelCommandIndex = 1;

            IUICommand command = await deleteDialog.ShowAsync();
            if (command == deleteCommand)
                DeleteLogs();
        }

        private async void DeleteLogs()
        {
            StorageFile logFile = await GetLogAsync("BatteryLog.csv");
            if (logFile == null)
            {
                await new MessageDialog("Log file is empty").ShowAsync();
                return;
            }

            await FileIO.WriteBytesAsync(logFile, new byte[0]);
            await new MessageDialog("Logs deleted").ShowAsync(); //TODO validate if log file is empty?
        }

        private async Task<StorageFile> GetLogAsync(string fileName)
        {
            try
            {
                StorageFolder localFolder = ApplicationData.Current.LocalFolder;
                return await localFolder.GetFileAsync(fileName);
            }
            catch (FileNotFoundException e)
            {
                return null;
            }
        }

        private void ReloadAppBarButton_Click(object sender, RoutedEventArgs e)
        {
            LoadLogs();
        }
    }
}
