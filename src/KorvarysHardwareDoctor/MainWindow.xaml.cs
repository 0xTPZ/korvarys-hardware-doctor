using System.Management;
using System.Windows;
using System.Windows.Threading;

namespace KorvarysHardwareDoctor;
public partial class MainWindow : Window
{
    private readonly GpuTelemetryService _telemetry = new();
    private readonly GpuDiagnosticsService _diagnostics = new();
    private readonly DispatcherTimer _telemetryTimer = new() { Interval = TimeSpan.FromSeconds(2) };

    public MainWindow()
    {
        InitializeComponent();
        Loaded += (_, _) => { RefreshHardware(); RefreshTelemetry(); RefreshDiagnostics(); _telemetryTimer.Start(); };
        Closed += (_, _) => _telemetryTimer.Stop();
        _telemetryTimer.Tick += (_, _) => RefreshTelemetry();
    }

    private void RefreshHardware()
    {
        try
        {
            using var searcher = new ManagementObjectSearcher("SELECT Name, DriverVersion, Status FROM Win32_VideoController");
            var gpu = searcher.Get().Cast<ManagementObject>().FirstOrDefault();
            if (gpu is null) { GpuName.Text = "GPU não detectada"; HealthText.Text = "ATENÇÃO"; return; }
            GpuName.Text = gpu["Name"]?.ToString() ?? "GPU sem nome";
            GpuDriver.Text = $"Driver {gpu["DriverVersion"] ?? "desconhecido"}  •  Status {gpu["Status"] ?? "desconhecido"}";
            DriverStatus.Text = gpu["Status"]?.ToString() ?? "—";
            HealthText.Text = string.Equals(gpu["Status"]?.ToString(), "OK", StringComparison.OrdinalIgnoreCase) ? "SAUDÁVEL" : "ATENÇÃO";
        }
        catch { GpuName.Text = "Leitura bloqueada pelo sistema"; GpuDriver.Text = "Execute como administrador para diagnóstico completo"; HealthText.Text = "LIMITADO"; }
    }
    private void Refresh_Click(object sender, RoutedEventArgs e) => RefreshHardware();
    private void RefreshTelemetry()
    {
        var telemetry = _telemetry.Read();
        UsageText.Text = telemetry.Usage ?? "—";
        TempText.Text = telemetry.Temperature ?? "—";
        SupportText.Text = telemetry.Supported ? $"Monitoramento ativo • {telemetry.Source} • GPU {telemetry.CoreClock ?? "—"} • VRAM {telemetry.MemoryClock ?? "—"}" : "Modo seguro: métrica não exposta pelo Windows";
    }
    private void RefreshDiagnostics()
    {
        var result = _diagnostics.Analyze();
        if (result.Verdict != "SAUDÁVEL") HealthText.Text = result.Verdict;
        SectionDetails.Text = result.Summary;
    }
    private void Overview_Click(object sender, RoutedEventArgs e) { PageTitle.Text = "Visão geral"; PageSubtitle.Text = "Leitura segura do estado atual do equipamento"; SectionDetails.Text = "Resumo do hardware detectado e disponibilidade dos sensores."; }
    private void Diagnostics_Click(object sender, RoutedEventArgs e) { PageTitle.Text = "Diagnóstico"; PageSubtitle.Text = "Evidências locais de instabilidade e falhas de driver"; RefreshDiagnostics(); }
    private void Performance_Click(object sender, RoutedEventArgs e) { PageTitle.Text = "Desempenho"; PageSubtitle.Text = "Monitoramento e tuning compatível com o hardware"; SectionDetails.Text = "Telemetria ao vivo ativa. Os controles ADLX serão liberados após validar limites térmicos."; }
    private void Settings_Click(object sender, RoutedEventArgs e) { PageTitle.Text = "Configurações"; PageSubtitle.Text = "Preferências, segurança e restauração"; SectionDetails.Text = "Perfis, intervalo de leitura, backup e rollback ficarão disponíveis nesta seção."; }
    private void Profile_Click(object sender, RoutedEventArgs e) => MessageBox.Show("Perfil preparado, mas a aplicação de clock, tensão e potência ficará bloqueada até o KORVARYS confirmar os limites reais da sua GPU pelo driver AMD. Nenhuma alteração foi feita.", "KORVARYS Hardware Doctor", MessageBoxButton.OK, MessageBoxImage.Information);
}
