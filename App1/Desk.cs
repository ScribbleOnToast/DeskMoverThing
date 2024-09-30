using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading.Tasks;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using Windows.Networking.Sockets;
using Windows.Security.Cryptography;
using Windows.Storage.Streams;

namespace App1
{
    public delegate void HeightChangedHandler(double height);
    public delegate void SendMessageToUiHandler(string message);

    public class Desk
    {
        public event HeightChangedHandler HeightChanged;
        public event SendMessageToUiHandler SendMessageToUi;

        public DeskServices Services;
        public BluetoothLEDevice Device;
        public double Height { get; private set; }

        public Desk(BluetoothLEDevice _device)
        {
            Device = _device;
            InitDevice();
        }

        private async void InitDevice()
        {
            var deviceServices = await Device.GetGattServicesAsync();
            Services = new DeskServices
            {
                REFERENCE_OUTPUT = deviceServices.Services.First(s => s.Uuid == LinakUuids.Services.REFERENCE_OUTPUT),
                CONTROL_SERVICE = deviceServices.Services.First(s => s.Uuid == LinakUuids.Services.CONTROL),
                DPG_SERVICE = deviceServices.Services.First(s => s.Uuid == LinakUuids.Services.DPG)
            };
            var session = await GattSession.FromDeviceIdAsync(Device.BluetoothDeviceId);
            session.MaintainConnection = true;            

            //Attach to DPG notification
            try
            {
                GattCharacteristicsResult dpg_result = await Services.DPG_SERVICE.GetCharacteristicsAsync(BluetoothCacheMode.Uncached);// LinakUuids.Characteristics.DPG__DPG, BluetoothCacheMode.Uncached);
                GattCharacteristic dpg = dpg_result.Characteristics.First(c => c.Uuid == LinakUuids.Characteristics.DPG__DPG);
                GattCommunicationStatus commStatus = await dpg.WriteClientCharacteristicConfigurationDescriptorAsync(GattClientCharacteristicConfigurationDescriptorValue.Notify);
                if (commStatus == GattCommunicationStatus.Success)
                {
                    dpg.ValueChanged += dpg_msg_received;
                }

                //Attach to the Height and Speed GattCharacteristic Change notifier.
                GattCharacteristicsResult height_speed_resut = await Services.REFERENCE_OUTPUT.GetCharacteristicsForUuidAsync(LinakUuids.Characteristics.REFERENCE_OUTPUT__HEIGHT_SPEED, BluetoothCacheMode.Uncached);
                GattCharacteristic height_speed = height_speed_resut.Characteristics.First(c => c.Uuid == LinakUuids.Characteristics.REFERENCE_OUTPUT__HEIGHT_SPEED);
                commStatus = await height_speed.WriteClientCharacteristicConfigurationDescriptorAsync(GattClientCharacteristicConfigurationDescriptorValue.Notify);
                if (commStatus == GattCommunicationStatus.Success)
                {
                    height_speed.ValueChanged += Height_speed_ValueChanged;
                    var x = await height_speed.ReadValueAsync();
                    Height = GetHeightFromBuffer(x.Value) + 22.5;
                    HeightChanged?.Invoke(Height);
                }

                //Attach to the Error notification thing
                GattCharacteristicsResult error_result = await Services.CONTROL_SERVICE.GetCharacteristicsForUuidAsync(LinakUuids.Characteristics.CONTROL__ERROR, BluetoothCacheMode.Uncached);
                GattCharacteristic error = error_result.Characteristics.First(c => c.Uuid == LinakUuids.Characteristics.CONTROL__ERROR);
                commStatus = await error.WriteClientCharacteristicConfigurationDescriptorAsync(GattClientCharacteristicConfigurationDescriptorValue.Notify);
                if (commStatus == GattCommunicationStatus.Success)
                {
                    error.ValueChanged += Error_Recieved;
                }
            }
            catch (Exception ex)
            {
                //another app probably has control. 
                return;
            }
        }

        private void dpg_msg_received(GattCharacteristic sender, GattValueChangedEventArgs args)
        {
            var boop = 0;
        }

        private void Error_Recieved(GattCharacteristic sender, GattValueChangedEventArgs args)
        {
            StringBuilder hex = new StringBuilder((int)args.CharacteristicValue.Length * 2);
        }

        public bool MoveStop()
        {
            return false;
        }

        public async Task<bool> MoveUp()
        {
            //Setup the CONTROL characteristic. Controls the uppy / downy 
            await Services.CONTROL_SERVICE.RequestAccessAsync();
            GattCharacteristicsResult control_result = await Services.CONTROL_SERVICE.GetCharacteristicsForUuidAsync(LinakUuids.Characteristics.CONTROL__CONTROL, BluetoothCacheMode.Uncached);
            GattCharacteristic control = control_result.Characteristics.First(c => c.Uuid == LinakUuids.Characteristics.CONTROL__CONTROL);
            await control.WriteValueAsync(LinakUuids.Commands.TheFuck.AsBuffer(),GattWriteOption.WriteWithoutResponse);
            await control.WriteValueAsync(LinakUuids.Commands.MoveUp.AsBuffer(), GattWriteOption.WriteWithoutResponse);
            return true;
        }

        private void Height_speed_ValueChanged(GattCharacteristic sender, GattValueChangedEventArgs args)
        {
            Height = GetHeightFromBuffer(args.CharacteristicValue) + 22.5;
            HeightChanged?.Invoke(Height);
        }

        private double GetHeightFromBuffer(IBuffer buffer)
        {
            byte[] data;
            CryptographicBuffer.CopyToByteArray(buffer, out data);
            var height = data[0] + (data[0 + 1] << 8);
            return (double)height / 254;
        }
        
    }
}
