using System.ComponentModel;
using System.Diagnostics;
using System.Management;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text.Json.Serialization;
using WindowsGSM.Games;

namespace WindowsGSM.Services
{
    public class SystemMetricsService : IHostedService, IDisposable
    {
        public class CPUProperties
        {
            /// <summary>
            /// CPU Name
            /// </summary>
            public string? Name { get; set; } = string.Empty;

            /// <summary>
            /// CPU Core Count
            /// </summary>
            public int Count { get; set; }

            /// <summary>
            /// CPU Used Ratio
            /// </summary>
            public double UsedRatio { get; set; }
        }

        public class MemoryProperties
        {
            /// <summary>
            /// RAM Type
            /// </summary>
            public string? Type { get; set; } = string.Empty;

            /// <summary>
            /// RAM Used Size
            /// </summary>
            public double Used { get; set; }

            /// <summary>
            /// RAM Total Size
            /// </summary>
            public double Size { get; set; }

            /// <summary>
            /// RAM Used Ratio
            /// </summary>
            public double UsedRatio => Used / Size * 100;
        }

        public class DiskProperties
        {
            /// <summary>
            /// Disk Name
            /// </summary>
            public string? Name { get; set; } = string.Empty;

            /// <summary>
            /// Disk Type
            /// </summary>
            public string? Type { get; set; } = string.Empty;

            /// <summary>
            /// Disk Used Space
            /// </summary>
            public double Used { get; set; }

            /// <summary>
            /// Disk Total Size
            /// </summary>
            public double Size { get; set; }

            /// <summary>
            /// Disk Used Ratio
            /// </summary>
            public double UsedRatio => Used / Size * 100;
        }

        private int executionCount = 0;
        private readonly ILogger<SystemMetricsService> _logger;
        private Timer? _timer;

        public CPUProperties CPU { get; private set; } = new();
        public MemoryProperties Memory { get; private set; } = new();
        public Dictionary<string, DiskProperties> Disks { get; private set; } = new();

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Interoperability", "CA1416:Validate platform compatibility", Justification = "<Pending>")]
        public SystemMetricsService(ILogger<SystemMetricsService> logger)
        {
            _logger = logger;

            try
            {
                IEnumerable<ManagementObject> mbo = GetManagementObject("SELECT Name, NumberOfCores FROM Win32_Processor");
                CPU.Name = mbo.Select(x => x["Name"].ToString()).FirstOrDefault();
                CPU.Count = mbo.Sum(x => int.TryParse(x["NumberOfCores"].ToString(), out int result) ? result : 0);
            }
            catch
            {
                CPU.Name = string.Empty;
                CPU.Count = 0;
            }

            try
            {
                IEnumerable<ManagementObject> mbo = GetManagementObject($"SELECT MemoryType FROM Win32_PhysicalMemory");
                Memory.Type = GetMemoryType(mbo.Select(c => int.TryParse(c["MemoryType"].ToString(), out int result) ? result : 0).FirstOrDefault());
            }
            catch
            {
                Memory.Type = string.Empty;
            }

            try
            {
                IEnumerable<ManagementObject> mbo = GetManagementObject($"Select TotalVisibleMemorySize from Win32_OperatingSystem");
                Memory.Size = mbo.Select(x => double.TryParse(x["TotalVisibleMemorySize"].ToString(), out double result) ? result : 0).FirstOrDefault();
            }
            catch
            {
                Memory.Size = 0;
            }

            DriveInfo.GetDrives().ToList().ForEach(driveInfo =>
            {
                Disks[driveInfo.Name] = new DiskProperties
                {
                    Name = driveInfo.Name,
                    Type = driveInfo.DriveFormat,
                    Used = driveInfo.TotalSize - driveInfo.AvailableFreeSpace,
                    Size = driveInfo.TotalSize,
                };
            });
        }

        private static IList<ManagementObject> GetManagementObject(string queryString)
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                throw new Exception("Unsupported OS");
            }

            using var mos = new ManagementObjectSearcher(queryString);
            using var moc = mos.Get();

            return moc.Cast<ManagementObject>().ToList();
        }

        private static string GetMemoryType(int memoryType)
        {
            return memoryType switch
            {
                1 => "Other",
                2 => "DRAM",
                3 => "Synchronous DRAM",
                4 => "Cache DRAM",
                5 => "EDO",
                6 => "EDRAM",
                7 => "VRAM",
                8 => "SRAM",
                9 => "RAM",
                10 => "ROM",
                11 => "Flash",
                12 => "EEPROM",
                13 => "FEPROM",
                14 => "EPROM",
                15 => "CDRAM",
                16 => "3DRAM",
                17 => "SDRAM",
                18 => "SGRAM",
                19 => "RDRAM",
                20 => "DDR",
                21 => "DDR2",
                22 => "DDR2 FB-DIMM",
                23 => "Undefined 23",
                24 => "DDR3",
                25 => "Undefined 25",
                26 => "DDR4",
                _ => string.Empty,
            };
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Timed Hosted Service running.");

            _timer = new Timer(DoWork, null, TimeSpan.Zero, TimeSpan.FromSeconds(1));

            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Timed Hosted Service is stopping.");

            _timer?.Change(Timeout.Infinite, 0);

            return Task.CompletedTask;
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Interoperability", "CA1416:Validate platform compatibility", Justification = "<Pending>")]
        private void DoWork(object? state)
        {
            var count = Interlocked.Increment(ref executionCount);

            // Update CPU
            try
            {
                IEnumerable<ManagementObject> mbo = GetManagementObject($"SELECT PercentProcessorTime FROM Win32_PerfFormattedData_PerfOS_Processor WHERE Name='_Total'");
                CPU.UsedRatio = double.Parse(mbo.First().Properties["PercentProcessorTime"].Value.ToString()!);
            }
            catch
            {
                CPU.UsedRatio = 0;
            }

            // Update Memory
            try
            {
                IEnumerable<ManagementObject> mbo = GetManagementObject("Select FreePhysicalMemory from Win32_OperatingSystem");
                Memory.Used = mbo.Select(m => double.Parse(m["FreePhysicalMemory"].ToString()!)).FirstOrDefault();
            }
            catch
            {
                Memory.Used = 0;
            }

            // Update Disks
            DriveInfo.GetDrives().ToList().ForEach(driveInfo =>
            {
                Disks[driveInfo.Name] = new DiskProperties
                {
                    Name = driveInfo.Name,
                    Type = driveInfo.DriveFormat,
                    Used = driveInfo.TotalSize - driveInfo.AvailableFreeSpace,
                    Size = driveInfo.TotalSize,
                };
            });

            _logger.LogInformation("Timed Hosted Service is working. Count: {Count} {Used}", count, Memory.Used);
        }

        public void Dispose()
        {
            _timer?.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}
