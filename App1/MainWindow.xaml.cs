using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using System;
using System.ComponentModel;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using Windows.Devices.Enumeration;
using Windows.UI.Popups;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace App1
{
    /// <summary>
    /// An empty window that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainWindow : INotifyPropertyChanged
    {
        private double DeskHeight;
        private BluetoothLEDevice Device;
        private Desk Desk;

        public event PropertyChangedEventHandler PropertyChanged;

        readonly DispatcherQueue UIDispatcher = DispatcherQueue.GetForCurrentThread();


        public void OnPropertyChanged(string propertyName)
        {
            UIDispatcher.TryEnqueue(() => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName)));
        }

        public MainWindow()
        {
            this.InitializeComponent();
            this.AppWindow.Resize(new Windows.Graphics.SizeInt32(500,200));
            InitDesk();
        }

        private async void InitDesk()
        {
            //These will eventually be used when enumerating devices.
            string[] additionalProperties = { "System.Devices.Aep.DeviceAddress" };
            string aqsAllBluetoothLEDevices = "(System.Devices.Aep.ProtocolId:=\"{bb7bb05e-5972-42b5-94fc-76eaa7084d49}\")";

            //In the meantime, just hardcode the bluetooth address.
            Device = await BluetoothLEDevice.FromIdAsync("BluetoothLE#BluetoothLEe4:0d:36:52:46:5c-c4:71:3b:73:31:3e");

            Desk = new Desk(Device);
            Desk.HeightChanged += UpdateHeight;
            Desk.SendMessageToUi += UpdateUIStatus;
        }

        private async void bMoveUp_click(object sender, RoutedEventArgs e)
        {
            var result = await Desk.MoveUp();
            if (result)//== GattCommunicationStatus.Success)
            {
                bMoveUp.Content = "Worked?";
            }
            else
            {
                bMoveUp.Content = "DOH";
            }
           
        }

        public void UpdateHeight(double d)
        {
            DeskHeight = d;
            UIDispatcher.TryEnqueue(() => OnPropertyChanged(nameof(DeskHeight)));
        }

        public void UpdateUIStatus(string message)
        {
            MessageDialog diag = new MessageDialog(message);
            UIDispatcher.TryEnqueue(() => diag.ShowAsync());
        }
    }
}
