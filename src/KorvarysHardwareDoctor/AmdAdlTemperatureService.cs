using System.Runtime.InteropServices;

namespace KorvarysHardwareDoctor;

internal sealed class AmdAdlTemperatureService : IDisposable
{
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)] private delegate IntPtr AllocDelegate(int size);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)] private delegate int MainControlCreateDelegate(AllocDelegate callback, int enumConnected, out IntPtr context);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)] private delegate int MainControlDestroyDelegate(IntPtr context);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)] private delegate int AdapterCountDelegate(IntPtr context, out int count);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)] private delegate int TemperatureGetDelegate(IntPtr context, int adapterIndex, int thermalIndex, ref AdlTemperature temperature);

    [StructLayout(LayoutKind.Sequential)] private struct AdlTemperature { public int Value; public int Flags; }

    private readonly IntPtr _library;
    private readonly IntPtr _context;
    private readonly MainControlDestroyDelegate? _destroy;
    private readonly TemperatureGetDelegate? _temperatureGet;
    private readonly TemperatureGetDelegate? _temperatureGetN;
    private readonly AllocDelegate _allocator = Marshal.AllocHGlobal;

    public bool IsAvailable { get; }

    public AmdAdlTemperatureService()
    {
        try
        {
            _library = NativeLibrary.Load("atiadlxx.dll");
            var create = Marshal.GetDelegateForFunctionPointer<MainControlCreateDelegate>(NativeLibrary.GetExport(_library, "ADL2_Main_Control_Create"));
            _destroy = Marshal.GetDelegateForFunctionPointer<MainControlDestroyDelegate>(NativeLibrary.GetExport(_library, "ADL2_Main_Control_Destroy"));
            var count = Marshal.GetDelegateForFunctionPointer<AdapterCountDelegate>(NativeLibrary.GetExport(_library, "ADL2_Adapter_NumberOfAdapters_Get"));
            _temperatureGet = Marshal.GetDelegateForFunctionPointer<TemperatureGetDelegate>(NativeLibrary.GetExport(_library, "ADL2_Overdrive5_Temperature_Get"));
            _temperatureGetN = TryGetTemperatureDelegate("ADL2_OverdriveN_Temperature_Get");
            if (create(_allocator, 1, out _context) != 0 || count(_context, out var adapters) != 0 || adapters < 1) return;
            IsAvailable = true;
        }
        catch { IsAvailable = false; }
    }

    public string? ReadCelsius()
    {
        if (!IsAvailable || _temperatureGet is null) return null;
        foreach (var thermalIndex in new[] { 0, 1 })
        {
            var temperature = new AdlTemperature();
            if (_temperatureGet(_context, 0, thermalIndex, ref temperature) == 0 && temperature.Value > 0)
                return $"{temperature.Value / 1000.0:0.#} °C";
        }
        if (_temperatureGetN is not null)
        {
            foreach (var sensorIndex in new[] { 0, 1, 2 })
            {
                var temperature = new AdlTemperature();
                if (_temperatureGetN(_context, 0, sensorIndex, ref temperature) == 0 && temperature.Value > 0)
                    return $"{temperature.Value / 1000.0:0.#} °C";
            }
        }
        return null;
    }

    private TemperatureGetDelegate? TryGetTemperatureDelegate(string name)
    {
        try { return Marshal.GetDelegateForFunctionPointer<TemperatureGetDelegate>(NativeLibrary.GetExport(_library, name)); }
        catch { return null; }
    }

    public void Dispose()
    {
        if (_context != IntPtr.Zero) _destroy?.Invoke(_context);
        if (_library != IntPtr.Zero) NativeLibrary.Free(_library);
    }
}
