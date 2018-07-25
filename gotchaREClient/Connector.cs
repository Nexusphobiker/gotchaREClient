using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.Advertisement;
using Windows.Devices.Enumeration;

namespace gotchaREClient
{
    public class Connector
    {
        private static string tag = "Connector";
        private static Connector handle;
        public static bool gotchaConnected = false;
        public static bool upgraderConnected = false;

        public static void Init()
        {
            handle = new Connector();
        }

        private BluetoothLEAdvertisementWatcher advWatcher;

        private Connector()
        {
            Console.WriteLine(tag, "Initizialize");
            UnpairAll();
            advWatcher = new BluetoothLEAdvertisementWatcher();
            advWatcher.Received += AdvWatcher_Received;
            advWatcher.ScanningMode = BluetoothLEScanningMode.Active;
            advWatcher.Start();
            Console.WriteLine(tag, "Advertisement watcher started!");
        }

        private void AdvWatcher_Received(BluetoothLEAdvertisementWatcher sender, BluetoothLEAdvertisementReceivedEventArgs args)
        {
            if (args.Advertisement.LocalName == "")
                return;

            Console.WriteLine(tag, "Advertisement recieved! LocalName:" + args.Advertisement.LocalName);
            if (args.Advertisement.LocalName == Constants.DeviceName && !gotchaConnected)
            {
                Console.WriteLine(tag, "Found a "+Constants.DeviceName);
                Gotcha.StartCommunication(args.BluetoothAddress);
                gotchaConnected = true;
            }
            else if (args.Advertisement.LocalName == Constants.DeviceNameDebug && !upgraderConnected)
            {
                Console.WriteLine(tag, "Found a " + Constants.DeviceNameDebug);
                Upgrader.StartCommunication(args.BluetoothAddress);
                upgraderConnected = true;
            }
        }

        private async void UnpairAll()
        {
            var selector = BluetoothDevice.GetDeviceSelector();
            var devices = await DeviceInformation.FindAllAsync(selector);
            foreach (var device in devices)
            {
                if (device.Name == Constants.DeviceName || device.Name == Constants.DeviceNameDebug)
                {
                    Console.WriteLine(tag,"Unpairing " + device.Id);
                    await device.Pairing.UnpairAsync();
                }
            }
        }

    }
}
