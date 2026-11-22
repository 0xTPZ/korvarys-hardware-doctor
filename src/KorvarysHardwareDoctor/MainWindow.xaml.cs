using System.Management;
using System.Windows;
using System.Windows.Threading;

namespace KorvarysHardwareDoctor;
public partial class MainWindow : Window
{
    private readonly GpuTelemetryService _telemetry = new();
    private readonly DispatcherTimer _telemetryTimer = new() { Interval = TimeSpan.FromSeconds(2) };

    public MainWindow()
    {
        InitializeComponent();
        Loaded += (_, _) => { RefreshHardware(); RefreshTelemetry(); _telemetryTimer.Start(); };
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
        SupportText.Text = telemetry.Supported ? $"Monitoramento ativo • {telemetry.Source}" : "Modo seguro: métrica não exposta pelo Windows";
    }
    private void Overview_Click(object sender, RoutedEventArgs e) { PageTitle.Text = "Visão geral"; PageSubtitle.Text = "Leitura segura do estado atual do equipamento"; }
    private void Diagnostics_Click(object sender, RoutedEventArgs e) { PageTitle.Text = "Diagnóstico"; PageSubtitle.Text = "Evidências locais de instabilidade e falhas de driver"; }
    private void Performance_Click(object sender, RoutedEventArgs e) { PageTitle.Text = "Desempenho"; PageSubtitle.Text = "Monitoramento e tuning compatível com o hardware"; }
    private void Settings_Click(object sender, RoutedEventArgs e) { PageTitle.Text = "Configurações"; PageSubtitle.Text = "Preferências, segurança e restauração"; }
    private void Profile_Click(object sender, RoutedEventArgs e) => MessageBox.Show("O perfil só será aplicado após confirmar suporte ADLX e limites térmicos seguros.", "KORVARYS Hardware Doctor", MessageBoxButton.OK, MessageBoxImage.Information);
}
