namespace NetInject.Utils {
    using System;
    using System.Linq;
    using System.Management;
    using Microsoft.Win32;
    public static class VmwareHotfix {
        private static RegistryKey GraphicsKey {
            get {
                RegistryKey registryKey = Registry.CurrentUser.CreateSubKey("Software");
                if (registryKey == null) return null;
                RegistryKey subKey = registryKey.CreateSubKey("Microsoft");
                if (subKey == null) return null;
                RegistryKey key = subKey.CreateSubKey("Avalon.Graphics");
                return key;
            }
        }
        public static bool IsVm {
            get {
                using (var searcher = new ManagementObjectSearcher("Select * from Win32_ComputerSystem"))
                using (ManagementObjectCollection items = searcher.Get()) {
                    bool isVm = items.Cast<ManagementBaseObject>()
                        .Select(item => item["Manufacturer"].ToString().ToLower())
                        .Any(manufacturer => manufacturer == "microsoft corporation" || manufacturer.Contains("vmware")) ||
                                items.Cast<ManagementBaseObject>().Any(item => item["Model"].ToString() == "VirtualBox");
                    return isVm;
                }
            }
        }
        public static bool HwAcceleration {
            get {
                Int64 isDisabled = 0;
                if (GraphicsKey == null) return true;
                object val = GraphicsKey.GetValue("DisableHWAcceleration", "False");
                if (val != null)
                    Int64.TryParse(val.ToString(), out isDisabled);
                return isDisabled == 0;
            }
            set {
                if (GraphicsKey != null)
                    GraphicsKey.SetValue("DisableHWAcceleration", value ? 0 : 1, RegistryValueKind.DWord);
            }
        }
    }
}