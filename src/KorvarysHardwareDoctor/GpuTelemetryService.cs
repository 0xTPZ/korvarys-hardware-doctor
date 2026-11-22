using System.Management;

namespace KorvarysHardwareDoctor;

public sealed record GpuTelemetry(string? Temperature, string? Usage, string Source, bool Supported);

public sealed class GpuTelemetryService
{
    private readonly AmdAdlTemperatureService _amd = new();

    public GpuTelemetry Read()
    {
        try
        {
            using var searcher = new ManagementObjectSearcher("root\\CIMV2", "SELECT Name, UtilizationPercentage FROM Win32_PerfFormattedData_GPUPerformanceCounters_GPUEngine");
            var values = searcher.Get().Cast<ManagementObject>()
                .Select(x => x["UtilizationPercentage"])
                .OfType<uint>()
                .ToArray();
            var usage = values.Length == 0 ? null : $"{Math.Min(100, values.Select(v => (long)v).Sum())}%";
            return new GpuTelemetry(_amd.ReadCelsius(), usage, _amd.IsAvailable ? "AMD ADL + Windows counters" : "Windows GPU Performance Counters", values.Length > 0 || _amd.IsAvailable);
        }
        catch
        {
            return new GpuTelemetry(null, null, "Métrica não disponível", false);
        }
    }
}

