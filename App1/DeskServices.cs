

using Windows.Devices.Bluetooth.GenericAttributeProfile;

namespace App1
{
    public class DeskServices
    {
        public GattDeviceService REFERENCE_OUTPUT {  get; set; }
        public GattDeviceService CONTROL_SERVICE { get; set; }    
        public GattDeviceService DPG_SERVICE { get; set; }
    }
}
