using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace gotchaREClient
{
    public static class Constants
    {
        public static Guid CERTIFICATE_SERVICE = Guid.Parse("bbe87709-5b89-4433-ab7f-8b8eef0d8e37");
        public static Guid SIFA_COMMANDS = Guid.Parse("bbe87709-5b89-4433-ab7f-8b8eef0d8e39"); 
        public static Guid CENTRAL_TO_SIFA = Guid.Parse("bbe87709-5b89-4433-ab7f-8b8eef0d8e38");  
        public static Guid SIFA_TO_CENTRAL = Guid.Parse("bbe87709-5b89-4433-ab7f-8b8eef0d8e3a"); 

        public static Guid CONTROL_SERVICE = Guid.Parse("21c50462-67cb-63a3-5c4c-82b5b9939aeb");
        public static Guid BUTTON = Guid.Parse("21c50462-67cb-63a3-5c4c-82b5b9939aed"); 
        public static Guid FW_REQUEST = Guid.Parse("21c50462-67cb-63a3-5c4c-82b5b9939aef"); 
        public static Guid FW_VERSION = Guid.Parse("21c50462-67cb-63a3-5c4c-82b5b9939af0");  

        public static Guid SUOTA_SERVICE_UUID = Guid.Parse("21c50462-67cb-63a3-5c4c-82b5b9939af1");
        public static Guid SUOTA_CTRL_UUID = Guid.Parse("21c50462-67cb-63a3-5c4c-82b5b9939af2");
        public static Guid SUOTA_STATUS_NTF_UUID = Guid.Parse("21c50462-67cb-63a3-5c4c-82b5b9939af3");
        public static Guid SUOTA_READ_UUID = Guid.Parse("21c50462-67cb-63a3-5c4c-82b5b9939af4");
        public static Guid SUOTA_WRITE_UUID = Guid.Parse("21c50462-67cb-63a3-5c4c-82b5b9939af5");
        public static Guid SUOTA_ERASE_UUID = Guid.Parse("21c50462-67cb-63a3-5c4c-82b5b9939af6");

        public static string DeviceName = "Pokemon GO Plus";
        public static string DeviceNameDebug = "Gotcha-Upgrader";
    }
}
