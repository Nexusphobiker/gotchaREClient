using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading.Tasks;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using Windows.Devices.Enumeration;

namespace gotchaREClient
{
    public class Gotcha
    {
        private static string tag = "Gotcha";
        private static Gotcha handle;
        public static void StartCommunication(ulong deviceAddress)
        {
            handle = new Gotcha(deviceAddress);
        }

        private ulong DeviceAddress;
        private BluetoothLEDevice Device;
        private List<GattDeviceService> openServices;

        private Gotcha(ulong deviceAddress)
        {
            this.openServices = new List<GattDeviceService>();
            this.DeviceAddress = deviceAddress;
            Initialize();
        }

        private async void Initialize()
        {
            Console.WriteLine(tag, "Connecting...");
            this.Device = await BluetoothLEDevice.FromBluetoothAddressAsync(this.DeviceAddress);
            DevicePairingResult result = await this.Device.DeviceInformation.Pairing.PairAsync();
            if(result.Status != DevicePairingResultStatus.AlreadyPaired && result.Status != DevicePairingResultStatus.Paired)
            {
                Console.WriteLine(tag, "Failed to pair:" + result.Status);
                this.Device.Dispose();
                return;
            }
            EnableUpgradeMode();
        }

        private async void EnableUpgradeMode()
        {
            string upTag = "EnableUpgradeMode";
            GattDeviceService service;
            GattCharacteristic characteristic;
            GattWriteResult gattWriteResult;
            byte[] buffer;

            try
            {
                Console.WriteLine(upTag, "Step 1");
                service = await GetServiceAsync(Constants.CERTIFICATE_SERVICE);
                characteristic = await GetCharacteristicAsync(Constants.CENTRAL_TO_SIFA, service);
                buffer = new byte[] { 0 };
                gattWriteResult = await characteristic.WriteValueWithResultAsync(buffer.AsBuffer());
                if (gattWriteResult.Status != GattCommunicationStatus.Success)
                    throw (new Exception(gattWriteResult.Status.ToString()));
            }
            catch(Exception ex)
            {
                Console.WriteLine(upTag, "Step 1 failed:"+ex.Message);
                goto ForceUpdate;
            }

            try
            {
                Console.WriteLine(upTag, "Step 2");
                service = await GetServiceAsync(Constants.CERTIFICATE_SERVICE);
                characteristic = await GetCharacteristicAsync(Constants.SIFA_COMMANDS, service);
                GattCommunicationStatus status = await characteristic.WriteClientCharacteristicConfigurationDescriptorAsync(GattClientCharacteristicConfigurationDescriptorValue.Notify);
                if (status != GattCommunicationStatus.Success)
                    throw (new Exception(status.ToString()));
            }
            catch(Exception ex)
            {
                Console.WriteLine(upTag, "Step 2 failed:" + ex.Message);
                RemoveDevice();
            }


            ForceUpdate:
            try
            {
                Console.WriteLine(upTag, "ForceUpdate");
                service = await GetServiceAsync(Constants.CERTIFICATE_SERVICE);
                characteristic = await GetCharacteristicAsync(Constants.CENTRAL_TO_SIFA, service);
                buffer = new byte[] { 0xfe, 0xff, 0xff, 0xff, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 };
                gattWriteResult = await characteristic.WriteValueWithResultAsync(buffer.AsBuffer());
                if (gattWriteResult.Status != GattCommunicationStatus.Success)
                    throw (new Exception(gattWriteResult.Status.ToString()));
            }
            catch(Exception ex)
            {
                Console.WriteLine(upTag, "ForceUpdate failed:" + ex.Message);
                RemoveDevice();
                return;
            }

            try
            {
                Console.WriteLine(upTag, "Confirmation");
                service = await GetServiceAsync(Constants.CONTROL_SERVICE);
                characteristic = await GetCharacteristicAsync(Constants.FW_VERSION, service);
                var ReadResult = await characteristic.ReadValueAsync();
                if (ReadResult.Status != GattCommunicationStatus.Success)
                    throw (new Exception(ReadResult.Status.ToString()));
                string confirmationString = Encoding.UTF8.GetString(ReadResult.Value.ToArray());
                Console.WriteLine(upTag, confirmationString);
                if (!confirmationString.Contains("Datel"))
                    throw (new Exception("Invalid confirmation recieved"));
            }
            catch (Exception ex)
            {
                Console.WriteLine(upTag, "Confirmation failed:" + ex.Message);
                RemoveDevice();
            }

            try
            {
                Console.WriteLine(upTag, "Enable Upgrade mode");
                buffer = new byte[] { 1 };
                service = await GetServiceAsync(Constants.CONTROL_SERVICE);
                characteristic = await GetCharacteristicAsync(Constants.FW_REQUEST, service);
                gattWriteResult = await characteristic.WriteValueWithResultAsync(buffer.AsBuffer());
                if (gattWriteResult.Status != GattCommunicationStatus.Success)
                {
                    throw (new Exception(gattWriteResult.Status.ToString()));
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(upTag, "Enable Upgrade mode failed:" + ex.Message);
                RemoveDevice();
            }
            RemoveDevice();
        }

        private async void RemoveDevice()
        {
            Console.WriteLine(tag, "Removing device");
            //trying to dispose all open services so the gotcha disconnects
            foreach (var s in openServices)
            {
                try
                {
                    s.Dispose();
                }
                catch (Exception ex)
                {
                    Console.WriteLine(tag, "Failed to dispose service "+ex.Message);
                }
            }

            //await Device.DeviceInformation.Pairing.UnpairAsync();
            Device.Dispose();
            Device = null;
            Connector.gotchaConnected = false;
        }

        private async Task<GattDeviceService> GetServiceAsync(Guid guid)
        {
            GattDeviceServicesResult result = await Device.GetGattServicesAsync();
            if (result.Status == GattCommunicationStatus.Success)
            {
                var services = result.Services;
                foreach (var service in services)
                {
                    if (service.Uuid.Equals(guid))
                    {
                        openServices.Add(service);
                        return service;
                    }
                }
            }
            throw (new Exception("GetService: Couldnt find Service " + guid + " Result:" + result.Status));
        }

        private async Task<GattCharacteristic> GetCharacteristicAsync(Guid guid, GattDeviceService service)
        {
            GattCharacteristicsResult result = await service.GetCharacteristicsAsync();
            if (result.Status == GattCommunicationStatus.Success)
            {
                var characteristics = result.Characteristics;
                foreach (var characteristic in characteristics)
                {
                    if (characteristic.Uuid.Equals(guid))
                    {
                        return characteristic;
                    }
                }
            }
            throw (new Exception("GetCharacteristic: Couldnt find Characteristic " + guid + " Result:" + result.Status));
        }
    }
}
