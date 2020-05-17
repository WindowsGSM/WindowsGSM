using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace WindowsGSM.Functions
{
    class SystemMetrics
    {
        public string CPUType { get; private set; }
        public int CPUCoreCount { get; private set; }
        public string RAMType { get; private set; }
        public double RAMTotalSize { get; private set; }
        public string DiskName { get; private set; }
        public string DiskType { get; private set; }
        public long DiskTotalSize { get; private set; }

        public void GetCPUStaticInfo()
        {
            try
            {
                var mbo = new ManagementObjectSearcher("SELECT Name, NumberOfCores FROM Win32_Processor").Get();
                CPUType = mbo.Cast<ManagementBaseObject>().Select(c => c["Name"].ToString()).FirstOrDefault();
                CPUCoreCount = mbo.Cast<ManagementBaseObject>().Sum(x => int.Parse(x["NumberOfCores"].ToString()));
            }
            catch
            {
                CPUType = "Fail to get CPU Type";
                CPUCoreCount = -1;
            }
        }

        public void GetRAMStaticInfo()
        {
            try
            {
                const string RAM_TOTAL_MEMORY = "TotalVisibleMemorySize";
                RAMTotalSize = new ManagementObjectSearcher($"Select {RAM_TOTAL_MEMORY} from Win32_OperatingSystem").Get().Cast<ManagementObject>().Select(m => double.Parse(m[RAM_TOTAL_MEMORY].ToString())).FirstOrDefault();
                RAMType = GetMemoryType();
            }
            catch
            {
                RAMTotalSize = -1;
                RAMType = "Fail to get RAM Type";
            }
        }

        public void GetDiskStaticInfo(string disk = null)
        {
            disk = disk ?? Path.GetPathRoot(Process.GetCurrentProcess().MainModule.FileName);
            DiskName = disk.TrimEnd('\\');
            DiskType = DriveInfo.GetDrives().Where(x => (x.Name == disk) && x.IsReady).Select(x => x.DriveFormat).FirstOrDefault();
            DiskTotalSize = DriveInfo.GetDrives().Where(x => (x.Name == disk) && x.IsReady).Select(x => x.TotalSize).FirstOrDefault();
        }

        public double GetCPUUsage()
        {
            try
            {
                const string CPU_USAGE = "PercentProcessorTime";
                return double.Parse(new ManagementObjectSearcher($"SELECT {CPU_USAGE} FROM Win32_PerfFormattedData_PerfOS_Processor WHERE Name='_Total'").Get().Cast<ManagementObject>().First().Properties[CPU_USAGE].Value.ToString());
            }
            catch
            {
                return 0;
            }
        }

        public double GetRAMUsage()
        {
            try
            {
                const string RAM_FREE_MEMORY = "FreePhysicalMemory";
                double freeMemory = new ManagementObjectSearcher($"Select {RAM_FREE_MEMORY} from Win32_OperatingSystem").Get().Cast<ManagementObject>().Select(m => double.Parse(m[RAM_FREE_MEMORY].ToString())).FirstOrDefault();
                return (RAMTotalSize == 0) ? 0 : (1 - freeMemory / RAMTotalSize) * 100;
            }
            catch
            {
                return 0;
            }
        }

        public double GetDiskUsage(string disk = null)
        {
            disk = disk ?? Path.GetPathRoot(Process.GetCurrentProcess().MainModule.FileName);
            double freeSpace = DriveInfo.GetDrives().Where(x => (x.Name == disk) && x.IsReady).Select(x => x.AvailableFreeSpace).FirstOrDefault();
            return (DiskTotalSize == 0) ? 0 : (1 - freeSpace / DiskTotalSize) * 100;
        }

        public static string GetMemoryRatioString(double percent, double totalMemory)
        {
            int count = 0;
            while (totalMemory > 1024.0)
            {
                totalMemory /= 1024.0;
                count++;
            }

            return $"{string.Format("{0:0.00}", totalMemory * percent / 100)}/{string.Format("{0:0.00}", totalMemory)} {(count == 1 ? "MB" : count == 2 ? "GB" : "TB")} ";
        }

        public static string GetDiskRatioString(double percent, double totalDisk)
        {
            int count = 0;
            while (totalDisk > 1024.0)
            {
                totalDisk /= 1024.0;
                count++;
            }

            return $"{string.Format("{0:0.00}", totalDisk * percent / 100)}/{string.Format("{0:0.00}", totalDisk)} {(count == 1 ? "KB" : count == 2 ? "MB" : count == 3 ? "GB" : "TB")} ";
        }

        private string GetMemoryType()
        {
            const string RAM_MEMORY_TYPE = "MemoryType";
            var mbo = new ManagementObjectSearcher($"SELECT {RAM_MEMORY_TYPE} FROM Win32_PhysicalMemory").Get();
            switch (mbo.Cast<ManagementBaseObject>().Select(c => int.Parse(c[RAM_MEMORY_TYPE].ToString())).FirstOrDefault())
            {
                case 1: return "Other";
                case 2: return "DRAM";
                case 3: return "Synchronous DRAM";
                case 4: return "Cache DRAM";
                case 5: return "EDO";
                case 6: return "EDRAM";
                case 7: return "VRAM";
                case 8: return "SRAM";
                case 9: return "RAM";
                case 10: return "ROM";
                case 11: return "Flash";
                case 12: return "EEPROM";
                case 13: return "FEPROM";
                case 14: return "EPROM";
                case 15: return "CDRAM";
                case 16: return "3DRAM";
                case 17: return "SDRAM";
                case 18: return "SGRAM";
                case 19: return "RDRAM";
                case 20: return "DDR";
                case 21: return "DDR2";
                case 22: return "DDR2 FB-DIMM";
                case 23: return "Undefined 23";
                case 24: return "DDR3";
                case 25: return "Undefined 25";
                default: return "Unknown";
            }
        }
    }
}
