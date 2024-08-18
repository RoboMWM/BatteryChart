using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
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
    public sealed partial class SummariesPage : Page
    {
        public SummariesPage()
        {
            this.InitializeComponent();
            SystemNavigationManager.GetForCurrentView().AppViewBackButtonVisibility = AppViewBackButtonVisibility.Visible;
            LoadBatteryInfo();
        }

        private void LoadBatteryInfo()
        {
            StorageFile file = GetLogAsync("BatteryLog.csv").Result;
            if (file == null)
            {
                entryTextBlock.Text = "file is empty";
                return;
            }
            IList<string> logs = FileIO.ReadLinesAsync(file).GetResults();

            foreach (string entry in logs)
            {
                string[] columns = entry.Split(',');
                StringBuilder sb = new StringBuilder();
                sb.Append("date: ");
                sb.AppendLine(columns[0]);
                sb.Append("state: ");
                sb.AppendLine(columns[1]);
                sb.Append("remaining watts: ");
                sb.AppendLine(columns[2]);
                sb.Append("invoked task: ");
                sb.AppendLine(columns[3]);
                entryTextBlock.Text = sb.ToString();
                break;
            }

            //TODO stackpanel for each entry
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
    }
}
