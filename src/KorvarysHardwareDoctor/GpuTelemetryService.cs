using System.Management;

namespace KorvarysHardwareDoctor;

public sealed record GpuTelemetry(string? Temperature, string? Usage, string? CoreClock, string? MemoryClock, string Source, bool Supported);

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
                .Select(value => value is null ? 0L : Convert.ToInt64(value))
                .Where(value => value > 0)
                .ToArray();
            var activity = _amd.ReadActivity();
            var usage = activity.Usage ?? (values.Length == 0 ? null : $"{Math.Min(100, values.Sum())}%");
            return new GpuTelemetry(_amd.ReadCelsius(), usage, activity.CoreClock, activity.MemoryClock, _amd.IsAvailable ? "AMD ADL + Windows counters" : "Windows GPU Performance Counters", values.Length > 0 || _amd.IsAvailable);
        }
        catch
        {
            return new GpuTelemetry(null, null, null, null, "Métrica não disponível", false);
        }
    }
}
