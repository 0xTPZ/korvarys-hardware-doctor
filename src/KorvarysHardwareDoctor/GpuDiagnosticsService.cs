using System.Management;

namespace KorvarysHardwareDoctor;

public sealed record GpuDiagnostics(int DisplayErrors, int WheaErrors, string Verdict, string Summary);

public sealed class GpuDiagnosticsService
{
    public GpuDiagnostics Analyze()
    {
        var display = CountEvents("Display", 7);
        var whea = CountEvents("Microsoft-Windows-WHEA-Logger", 7);
        var verdict = display == 0 && whea == 0 ? "SAUDÁVEL" : display + whea < 5 ? "OBSERVAR" : "INVESTIGAR";
        var summary = verdict == "SAUDÁVEL"
            ? "Nenhum erro recente de display ou hardware foi encontrado."
            : $"Foram encontrados {display} erro(s) de display e {whea} evento(s) WHEA nos últimos 7 dias.";
        return new GpuDiagnostics(display, whea, verdict, summary);
    }

    private static int CountEvents(string provider, int days)
    {
        try
        {
            using var query = new ManagementObjectSearcher("root\\cimv2", $"SELECT EventCode FROM Win32_NTLogEvent WHERE Logfile='System' AND SourceName='{provider}' AND TimeGenerated >= '{DateTime.UtcNow.AddDays(-days):yyyyMMddHHmmss}.000000-000'");
            return query.Get().Count;
        }
        catch { return 0; }
    }
}
