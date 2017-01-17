using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace IoTRemovableFiles
{
    public class MyAppData
    {
        public string MAINPAGETITLE { get; set; }
        public int MAININT { get; set; }
        public string LOGFILENAME { get; set; }
    }

    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        public MyAppData AppData { get; set; }
        public async Task OpenAppFile(string fileName)
        {
            var removableDevices = KnownFolders.RemovableDevices;
            var externalDrives = await removableDevices.GetFoldersAsync();
            var usbDrive = externalDrives.Single(e => e.DisplayName.Contains("USB DISK"));
            StorageFile appconfig = await usbDrive.CreateFileAsync(
                string.Format("{0}.jpg", fileName), 
                CreationCollisionOption.OpenIfExists);

            using (StreamReader reader = new StreamReader(await appconfig.OpenStreamForReadAsync()))
            {
                var data = await reader.ReadToEndAsync();
                AppData = Newtonsoft.Json.JsonConvert.DeserializeObject<MyAppData>(data);
            }
        }

        StorageFile logFile;
        public async Task InitLog(string fileName)
        {
            logFile = null;
            var currentFolder = string.Format("{0}{1:00}{2:00}{3}", 
                fileName, 
                DateTime.Now.Month, 
                DateTime.Now.Day, 
                DateTime.Now.Year);
            var removableDevices = KnownFolders.RemovableDevices;
            var externalDrives = await removableDevices.GetFoldersAsync();
            var usbDrive = externalDrives.Single(e => e.DisplayName.Contains("USB DISK"));
            var meWattFolder = await usbDrive.CreateFolderAsync(
                currentFolder, CreationCollisionOption.OpenIfExists);
            logFile = await meWattFolder.CreateFileAsync(
                string.Format("{0}.jpg", fileName), CreationCollisionOption.OpenIfExists);
        }

        public async Task<object> WriteLogAsync(string log, object asyncObject = null)
        {
            using (var outputStream = await logFile.OpenStreamForWriteAsync())
            {
                outputStream.Seek(0, SeekOrigin.End);
                DataWriter dataWriter = new DataWriter(outputStream.AsOutputStream());
                dataWriter.WriteString(log);
                await dataWriter.StoreAsync();
                await outputStream.FlushAsync();
                outputStream.Dispose();
            }
            return asyncObject;
        }

        public MainPage()
        {
            this.InitializeComponent();
            this.Loaded += MainPage_Loaded;
        }

        private async void MainPage_Loaded(object sender, RoutedEventArgs e)
        {
            await this.OpenAppFile("MyAppData");
            titleTextBlock.Text = AppData.MAINPAGETITLE;
            myTextBox.Text = AppData.MAININT.ToString();
            await InitLog(AppData.LOGFILENAME).ContinueWith(async (antecedent) =>
            {
                await Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(
                    Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                {
                    logButton.IsEnabled = true;
                });
            });
        }

        private async void Button_Click(object sender, RoutedEventArgs e)
        {
            logButton.IsEnabled = false;
            await WriteLogAsync(Environment.NewLine + myTextBox.Text).ContinueWith(async (successAsync) =>
            {
                await Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(
                    Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                {
                    myTextBox.Text = string.Empty;
                    logButton.IsEnabled = true;
                });
            });
        }
    }
}
