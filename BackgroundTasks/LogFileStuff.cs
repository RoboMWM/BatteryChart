using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;

namespace BatteryChart
{
    public class LogFileStuff
    {
        public static async Task WriteToFile(string fileName, string content)
        {
            try
            {
                StorageFolder localFolder = ApplicationData.Current.LocalFolder;
                StorageFile file = await localFolder.CreateFileAsync(fileName, CreationCollisionOption.OpenIfExists);

                try
                {
                    await FileIO.AppendTextAsync(file, content);
                }
                catch (Exception e)
                {
                    StorageFile errorFile = await localFolder.CreateFileAsync("Err-" + fileName, CreationCollisionOption.OpenIfExists);
                    await FileIO.AppendTextAsync(errorFile, content);
                }
            }

            catch (Exception e)
            {
                //Nothing for now TODO: error log?
            }
        }
    }
}
