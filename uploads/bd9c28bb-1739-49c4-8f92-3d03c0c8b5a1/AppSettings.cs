using System.Text.Json;
using ZXing;

namespace EpicCameraScanner;

internal sealed class AppSettings
{
    public string Prefix { get; set; } = "\\";
    public string Suffix { get; set; } = "\\";
    public SendAfterScan SendAfterScan { get; set; } = SendAfterScan.None;
    public int CameraIndex { get; set; } = 0;
    public int ScanTimeoutSeconds { get; set; } = 15;
    public bool PlaySuccessSound { get; set; } = true;
    public bool ShowSuccessOverlay { get; set; } = true;
    public bool EnableLogging { get; set; } = false;
    public bool StartWithWindows { get; set; } = false;
    public bool AutoRotate { get; set; } = true;
    public bool TryHarder { get; set; } = true;
    public bool TryInverted { get; set; } = true;
    public List<string> EnabledFormats { get; set; } = DefaultFormats();
    public bool CompatibilityMode { get; set; } = false;
    public uint HotkeyModifiers { get; set; } = NativeMethods.MOD_CONTROL | NativeMethods.MOD_ALT;
    public uint HotkeyVirtualKey { get; set; } = NativeMethods.VK_S;

    public static string SettingsDirectory => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "EpicCameraScanner");

    public static string SettingsPath => Path.Combine(SettingsDirectory, "settings.json");
    public static string LogPath => Path.Combine(SettingsDirectory, "scanner.log");

    public static AppSettings Load()
    {
        try
        {
            if (!File.Exists(SettingsPath)) return new AppSettings();
            var json = File.ReadAllText(SettingsPath);
            return JsonSerializer.Deserialize<AppSettings>(json, JsonOptions()) ?? new AppSettings();
        }
        catch
        {
            return new AppSettings();
        }
    }

    public void Save()
    {
        Directory.CreateDirectory(SettingsDirectory);
        File.WriteAllText(SettingsPath, JsonSerializer.Serialize(this, JsonOptions()));
    }

    public IReadOnlyList<BarcodeFormat> GetBarcodeFormats()
    {
        var formats = new List<BarcodeFormat>();
        foreach (var name in EnabledFormats)
            if (Enum.TryParse<BarcodeFormat>(name, true, out var format)) formats.Add(format);
        return formats.Count > 0 ? formats : DefaultFormats().Select(Enum.Parse<BarcodeFormat>).ToList();
    }

    public static List<string> DefaultFormats() => new()
    {
        nameof(BarcodeFormat.CODE_128), nameof(BarcodeFormat.CODE_39),
        nameof(BarcodeFormat.DATA_MATRIX), nameof(BarcodeFormat.QR_CODE),
        nameof(BarcodeFormat.PDF_417), nameof(BarcodeFormat.AZTEC),
        nameof(BarcodeFormat.UPC_A), nameof(BarcodeFormat.UPC_E),
        nameof(BarcodeFormat.EAN_13), nameof(BarcodeFormat.EAN_8)
    };

    private static JsonSerializerOptions JsonOptions() => new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true,
        Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() }
    };
}

internal enum SendAfterScan { None, Enter, Tab }
