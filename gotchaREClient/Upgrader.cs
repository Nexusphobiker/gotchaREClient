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
    /*
    0 Init
    1 ReleaseSPIFlash
    2 ??? triggers a notfication
    3 ReadData
    4 DoCRC
    5 StartAnimation
    6,1 Reboot

    */
    public class Upgrader
    {
        private static string tag = "Upgrader";
        private static Upgrader handle;
        public static void StartCommunication(ulong deviceAddress)
        {
            handle = new Upgrader(deviceAddress);
        }

        private ulong DeviceAddress;
        private BluetoothLEDevice Device;
        private List<GattDeviceService> openServices;

        private Upgrader(ulong deviceAddress)
        {
            this.openServices = new List<GattDeviceService>();
            this.DeviceAddress = deviceAddress;
            Initialize();
        }

        private async void Initialize()
        {
            Console.WriteLine(tag, "Connecting...");
            this.Device = await BluetoothLEDevice.FromBluetoothAddressAsync(this.DeviceAddress);

            while (Device.ConnectionStatus != BluetoothConnectionStatus.Connected)
            {
                try
                {
                    Console.WriteLine(tag, "Waiting to be connected. ConnectionState:"+Device.ConnectionStatus + " IsPaired:"+Device.DeviceInformation.Pairing.IsPaired);
                    var services = await Device.GetGattServicesAsync();
                    foreach(var s in services.Services)
                    {
                        Console.WriteLine(tag, "Service found:" + s.Uuid);
                    }
                }
                catch(Exception ex)
                {

                }
                await System.Threading.Tasks.Task.Delay(3000);
            }

            Console.WriteLine(tag, "Connected");
            //Here you can test things


            InitUpgrader();
            
            bool r = await Dump();
            Console.WriteLine(tag, "Dump done.");
            /*byte[] data = await readData(0x8000);
            if(data != null)
            {
                Console.WriteLine(tag, "Data read:" + data.Length);
                WriteDataToFile(data);
                //PrintByteArray(data);
            }*/

            Console.WriteLine(tag, "------------- upgrader interaction end -------------");
            await System.Threading.Tasks.Task.Delay(3000);


            RemoveDevice();
        }

        private async Task<bool> Dump()
        {
            Console.WriteLine(tag, "InitDump");
            string dTag = "Dump";
            Windows.Storage.StorageFolder storageFolder = await Windows.Storage.StorageFolder.GetFolderFromPathAsync(@"Input a path here in case you want to dump a file");
            Windows.Storage.StorageFile sampleFile = await storageFolder.CreateFileAsync("sample.dump", Windows.Storage.CreationCollisionOption.ReplaceExisting);
            var stream = await sampleFile.OpenAsync(Windows.Storage.FileAccessMode.ReadWrite);
            Int32 address = 0xFE50;
            byte[] buff;
            using (var outputStream = stream.GetOutputStreamAt(0))
            {
                //while (address < 130000)
                //{
                    buff = await readData(address);
                    if (buff != null)
                    {
                        await outputStream.WriteAsync(buff.AsBuffer(0x10,buff.Length-0x10));
                        //The first 16 bytes are not the memory
                        address = address + 0xF4;
                        Console.WriteLine(dTag, "Read from:"+BitConverter.ToInt32(buff,0).ToString("X")+" next address:" + address);
                        
                    }
                    else
                    {
                        Console.WriteLine(dTag, "readData failed");
                    }
                //}
            }
            stream.Dispose();
            return true;
        }

        private async void InitUpgrader()
        {
            try
            {
                Console.WriteLine(tag, "InitUpgrader");
                GattDeviceService service = await GetServiceAsync(Constants.SUOTA_SERVICE_UUID);
                GattCharacteristic characteristic = await GetCharacteristicAsync(Constants.SUOTA_CTRL_UUID, service);
                byte[] buff = new byte[1];
                GattWriteResult result = await characteristic.WriteValueWithResultAsync(buff.AsBuffer());
                if (result.Status != GattCommunicationStatus.Success)
                    throw (new Exception("InitUpgrader. =>" + result.Status));
            }
            catch(Exception ex)
            {
                Console.WriteLine(tag, "Exception while InitUpgrader. " + ex.Message);
            }
        }

        private async void ReleaseSPIFlash()
        {
            try
            {
                Console.WriteLine(tag, "ReleaseSPIFlash");
                GattDeviceService service = await GetServiceAsync(Constants.SUOTA_SERVICE_UUID);
                GattCharacteristic characteristic = await GetCharacteristicAsync(Constants.SUOTA_CTRL_UUID, service);
                byte[] buff = { 1 };
                GattWriteResult result = await characteristic.WriteValueWithResultAsync(buff.AsBuffer());
                if (result.Status != GattCommunicationStatus.Success)
                    throw (new Exception("ReleaseSPIFlash. =>" + result.Status));
            }
            catch (Exception ex)
            {
                Console.WriteLine(tag, "Exception while ReleaseSPIFlash. " + ex.Message);
            }
        }

        private async void SubscribeToNotification()
        {
            try
            {
                Console.WriteLine(tag, "SubscribeToNotification");
                var service = await GetServiceAsync(Constants.SUOTA_SERVICE_UUID);
                var characteristic = await GetCharacteristicAsync(Constants.SUOTA_STATUS_NTF_UUID, service);
                characteristic.ValueChanged += Characteristic_ValueChanged;
                GattCommunicationStatus status = await characteristic.WriteClientCharacteristicConfigurationDescriptorAsync(GattClientCharacteristicConfigurationDescriptorValue.Notify);
                if (status != GattCommunicationStatus.Success)
                    throw (new Exception(status.ToString()));
            }
            catch (Exception ex)
            {
                Console.WriteLine(tag, "Exception while SubscribeToNotification. " + ex.Message);
            }
        }

        private async void UnsubscribeFromNotification()
        {
            try
            {
                Console.WriteLine(tag, "UnsubscribeFromNotification");
                var service = await GetServiceAsync(Constants.SUOTA_SERVICE_UUID);
                var characteristic = await GetCharacteristicAsync(Constants.SUOTA_STATUS_NTF_UUID, service);
                characteristic.ValueChanged += Characteristic_ValueChanged;
                GattCommunicationStatus status = await characteristic.WriteClientCharacteristicConfigurationDescriptorAsync(GattClientCharacteristicConfigurationDescriptorValue.None);
                if (status != GattCommunicationStatus.Success)
                    throw (new Exception(status.ToString()));
            }
            catch (Exception ex)
            {
                Console.WriteLine(tag, "Exception while UnsubscribeFromNotification. " + ex.Message);
            }
        }

        private void Characteristic_ValueChanged(GattCharacteristic sender, GattValueChangedEventArgs args)
        {
            Console.WriteLine(tag, "Notification recieved from SUOTA_STATUS_NTF_UUID");
        }

        private async Task<byte[]> readData(Int32 startAddr)
        {
            try
            {
                GattDeviceService service = await GetServiceAsync(Constants.SUOTA_SERVICE_UUID);
                GattCharacteristic characteristic = await GetCharacteristicAsync(Constants.SUOTA_CTRL_UUID, service);
                Int32 endAddr = startAddr + 0x100;
                byte[] retVar = null;
                byte[] requestBuff = new byte[] { 3, (byte)(BitConverter.GetBytes(startAddr)[0] & 0xFF), (byte)(BitConverter.GetBytes(startAddr)[1] & 0xFF), (byte)(BitConverter.GetBytes(startAddr)[2] & 0xFF), (byte)(BitConverter.GetBytes(startAddr)[3] & 0xFF), (byte)(BitConverter.GetBytes(endAddr)[0] & 0xFF), (byte)(BitConverter.GetBytes(endAddr)[1] & 0xFF), (byte)(BitConverter.GetBytes(endAddr)[2] & 0xFF), (byte)(BitConverter.GetBytes(endAddr)[3] & 0xFF) };
                GattWriteResult result = await characteristic.WriteValueWithResultAsync(requestBuff.AsBuffer());
                if (result.Status != GattCommunicationStatus.Success)
                    throw (new Exception("readData. =>" + result.Status));


                characteristic = await GetCharacteristicAsync(Constants.SUOTA_READ_UUID, service);
                var ReadResult = await characteristic.ReadValueAsync();
                if (ReadResult.Status != GattCommunicationStatus.Success)
                    throw (new Exception("readDataResult =>" + ReadResult.Status));

                retVar = ReadResult.Value.ToArray();
                return retVar;
            }
            catch (Exception ex)
            {
                Console.WriteLine(tag, "Exception while readData. " + ex.Message);
                return null;
            }
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
                    Console.WriteLine(tag, "Failed to dispose service " + ex.Message);
                }
            }

            await Device.DeviceInformation.Pairing.UnpairAsync();
            Device.Dispose();
            Device = null;
            Connector.upgraderConnected = false;
        }

        private async void RebootDevice()
        {
            try
            {
                byte[] buffer = { 6, 1 };
                Console.WriteLine(tag, "RebootDevice");
                var service = await GetServiceAsync(Constants.SUOTA_SERVICE_UUID);
                var characteristic = await GetCharacteristicAsync(Constants.SUOTA_CTRL_UUID, service);
                var gattWriteResult = await characteristic.WriteValueWithResultAsync(buffer.AsBuffer());
                if (gattWriteResult.Status != GattCommunicationStatus.Success)
                    throw (new Exception(gattWriteResult.Status.ToString()));
            }
            catch(Exception ex)
            {
                Console.WriteLine(tag, "Failed to reboot device. " + ex.Message);
            }
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

        private void PrintByteArray(byte[] data)
        {
            string m = "";
            foreach(byte b in data)
            {
                if(b.ToString("X").Length < 2)
                {
                    m = m + b.ToString("X") + "0 ";
                }
                else
                {
                    m = m + b.ToString("X") + " ";
                }
            }
            Console.WriteLine(tag, m);
        }
    }
}
