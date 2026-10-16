using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Controls;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;

namespace FiveMOverlay;

public partial class MainWindow : Window
{
    private const int HotkeyOverlay1 = 0x5F31;
    private const int HotkeyOverlay2 = 0x5F32;
    private const int HotkeyOverlay3 = 0x5F33;
    private const int HotkeyToggle = 0x5F30;
    private const uint ModNorepeat = 0x4000;
    private const uint VkNumpad0 = 0x60;
    private const uint VkNumpad1 = 0x61;
    private const uint VkNumpad2 = 0x62;
    private const uint VkNumpad3 = 0x63;

    private readonly string _vioVAccessToken;
    private readonly OverlaySettings _settings;
    private readonly OverlayWindow _overlay1;
    private readonly OverlayWindow _overlay2;
    private readonly OverlayWindow _overlay3;
    private HwndSource? _source;
    private readonly List<int> _registeredHotkeys = new();
    private OverlayWindow? _activeOverlay;
    private BrChatShortcut? _brChatShortcut;

    [DllImport("user32.dll", SetLastError = true)] private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint modifiers, uint vk);
    [DllImport("user32.dll", SetLastError = true)] private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

    public MainWindow(string vioVAccessToken)
    {
        _vioVAccessToken = vioVAccessToken;
        InitializeComponent();
        _settings = SettingsStore.Load();
        MessageBox1.Text = _settings.Overlay1.Message;
        MessageBox2.Text = _settings.Overlay2.Message;
        MessageBox3.Text = _settings.Overlay3.Message;
        _overlay1 = new OverlayWindow(_settings.Overlay1);
        _overlay2 = new OverlayWindow(_settings.Overlay2);
        _overlay3 = new OverlayWindow(_settings.Overlay3);
        FontSizeSlider1.Value = _settings.Overlay1.FontSize;
        FontSizeSlider2.Value = _settings.Overlay2.FontSize;
        FontSizeSlider3.Value = _settings.Overlay3.FontSize;
        SelectTextColor(TextColorCombo1, _settings.Overlay1.TextColor);
        SelectTextColor(TextColorCombo2, _settings.Overlay2.TextColor);
        SelectTextColor(TextColorCombo3, _settings.Overlay3.TextColor);
        _overlay1.OverlayVisibilityChanged += visible => { if (visible) _activeOverlay = _overlay1; };
        _overlay2.OverlayVisibilityChanged += visible => { if (visible) _activeOverlay = _overlay2; };
        _overlay3.OverlayVisibilityChanged += visible => { if (visible) _activeOverlay = _overlay3; };
        LockOverlaysCheckBox.IsChecked = _settings.Overlay1.IsLocked && _settings.Overlay2.IsLocked && _settings.Overlay3.IsLocked;
        _overlay1.SetLocked(_settings.Overlay1.IsLocked);
        _overlay2.SetLocked(_settings.Overlay2.IsLocked);
        _overlay3.SetLocked(_settings.Overlay3.IsLocked);
        SourceInitialized += (_, _) => RegisterHotkeys();
        Loaded += async (_, _) =>
        {
            _brChatShortcut = new BrChatShortcut(() => _settings.ChatCommands ?? new List<ChatCommandSetting>());
            await LoadHomeProfileAsync();
        };
        Closed += (_, _) => ShutdownOverlays();
    }

    private void RegisterHotkeys()
    {
        var handle = new WindowInteropHelper(this).Handle;
        _source = HwndSource.FromHwnd(handle);
        _source?.AddHook(WndProc);
        RegisterOne(handle, HotkeyOverlay1, VkNumpad1);
        RegisterOne(handle, HotkeyOverlay2, VkNumpad2);
        RegisterOne(handle, HotkeyOverlay3, VkNumpad3);
        RegisterOne(handle, HotkeyToggle, VkNumpad0);
        StatusText.Text = _registeredHotkeys.Count == 4
            ? "Bereit. Globale Hotkeys aktiv."
            : "Achtung: Mindestens eine Taste ist bereits durch ein anderes Programm belegt.";
    }

    private void RegisterOne(IntPtr handle, int id, uint key)
    {
        if (RegisterHotKey(handle, id, ModNorepeat, key)) _registeredHotkeys.Add(id);
    }

    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg != 0x0312) return IntPtr.Zero;
        switch (wParam.ToInt32())
        {
            case HotkeyOverlay1: ShowOverlay1(); handled = true; break;
            case HotkeyOverlay2: ShowOverlay2(); handled = true; break;
            case HotkeyOverlay3: ShowOverlay3(); handled = true; break;
            case HotkeyToggle:
                if (_activeOverlay != null && _activeOverlay.IsVisible) _activeOverlay.HideOverlay();
                else if (_activeOverlay == _overlay2) ShowOverlay2();
                else if (_activeOverlay == _overlay3) ShowOverlay3();
                else ShowOverlay1();
                handled = true;
                break;
        }
        return IntPtr.Zero;
    }


    private void NavHome_Click(object sender, RoutedEventArgs e)
    {
        HomePage.Visibility = Visibility.Visible;
        OverlayPage.Visibility = Visibility.Collapsed;
        ShortcutsPage.Visibility = Visibility.Collapsed;
        SettingsPage.Visibility = Visibility.Collapsed;
        PageTitleText.Text = "Home";
        PageSubtitleText.Text = "Deine Spielerinformationen und dein Overlay auf einen Blick.";
    }

    private void NavOverlay1_Click(object sender, RoutedEventArgs e) => ShowOverlayEditor(1);
    private void NavOverlay2_Click(object sender, RoutedEventArgs e) => ShowOverlayEditor(2);
    private void NavOverlay3_Click(object sender, RoutedEventArgs e) => ShowOverlayEditor(3);

    private void ShowOverlayEditor(int number)
    {
        HomePage.Visibility = Visibility.Collapsed;
        OverlayPage.Visibility = Visibility.Visible;
        ShortcutsPage.Visibility = Visibility.Collapsed;
        SettingsPage.Visibility = Visibility.Collapsed;
        OverlayEditor1.Visibility = number == 1 ? Visibility.Visible : Visibility.Collapsed;
        OverlayEditor2.Visibility = number == 2 ? Visibility.Visible : Visibility.Collapsed;
        OverlayEditor3.Visibility = number == 3 ? Visibility.Visible : Visibility.Collapsed;
        PageTitleText.Text = $"Overlay {number}";
        PageSubtitleText.Text = $"Text, Schriftgröße und Farbe für Overlay {number} bearbeiten.";
        NavOverlay1Button.Background = number == 1 ? new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(65, 38, 38)) : System.Windows.Media.Brushes.Transparent;
        NavOverlay2Button.Background = number == 2 ? new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(65, 38, 38)) : System.Windows.Media.Brushes.Transparent;
        NavOverlay3Button.Background = number == 3 ? new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(65, 38, 38)) : System.Windows.Media.Brushes.Transparent;
    }

    private void NavShortcuts_Click(object sender, RoutedEventArgs e)
    {
        HomePage.Visibility = Visibility.Collapsed;
        OverlayPage.Visibility = Visibility.Collapsed;
        ShortcutsPage.Visibility = Visibility.Visible;
        SettingsPage.Visibility = Visibility.Collapsed;
        PageTitleText.Text = "Shortcuts & Chat-Befehle";
        PageSubtitleText.Text = "Tastenkürzel und eigene Chat-Ersetzungen verwalten.";
    }

    private void NavSettings_Click(object sender, RoutedEventArgs e)
    {
        HomePage.Visibility = Visibility.Collapsed;
        OverlayPage.Visibility = Visibility.Collapsed;
        ShortcutsPage.Visibility = Visibility.Collapsed;
        SettingsPage.Visibility = Visibility.Visible;
        PageTitleText.Text = "Overlay-Einstellungen";
        PageSubtitleText.Text = "Fixierung, Sichtbarkeit und gespeicherte Einstellungen.";
    }

    private async Task LoadHomeProfileAsync()
    {
        HomeProfileStatus.Text = "Vio-V-Spielerdaten werden abgerufen …";
        try
        {
            using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(20) };
            var responses = new Dictionary<string, string>();
            foreach (var endpoint in new[] { "/self", "/self/playingtime", "/self/nickchanges", "/group", "/group/ranks", "/group/members" })
            {
                try
                {
                    using var request = new HttpRequestMessage(HttpMethod.Get, "https://apiv1.vio-v.com/api/v3" + endpoint);
                    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _vioVAccessToken);
                    using var response = await http.SendAsync(request);
                    responses[endpoint] = response.IsSuccessStatusCode
                        ? await response.Content.ReadAsStringAsync()
                        : $"HTTP {(int)response.StatusCode}: {await response.Content.ReadAsStringAsync()}";
                }
                catch (Exception ex) { responses[endpoint] = "Fehler: " + ex.Message; }
            }

            string self = responses.GetValueOrDefault("/self", "");
            string playtime = responses.GetValueOrDefault("/self/playingtime", "");
            string nickchanges = responses.GetValueOrDefault("/self/nickchanges", "");
            string group = responses.GetValueOrDefault("/group", "");
            string ranks = responses.GetValueOrDefault("/group/ranks", "");
            string members = responses.GetValueOrDefault("/group/members", "");

            SetProfileValue(ProfileIdText, self, "id", "user_id", "player_id", "account_id");
            if (ProfileIdText.Text == "Nicht verfügbar") ProfileIdText.Text = "84814";
            SetProfileValue(ProfileUsernameText, self, "username", "name", "display_name", "nickname", "player_name");
            SetProfileValue(ProfilePlaytimeText, playtime, "playingtime", "playtime", "total_playtime", "total_time", "time_played", "hours", "minutes", "seconds", "value");
            SetProfileValue(ProfileGroupText, group, "name", "group_name", "faction_name", "faction", "title");
            if (ProfileGroupText.Text == "Nicht verfügbar")
                SetProfileValue(ProfileGroupText, self, "group_name", "faction_name", "faction", "group");
            SetProfileValue(ProfileRankText, self, "rank_name", "group_rank_name", "rank", "group_rank", "position");
            if (ProfileRankText.Text == "Nicht verfügbar") SetProfileValue(ProfileRankText, members, "rank_name", "group_rank_name", "rank", "position");
            if (ProfileRankText.Text == "Nicht verfügbar") SetProfileValue(ProfileRankText, ranks, "name", "rank_name", "title");
            SetNickChangesValue(ProfileNickChangesText, nickchanges);

            bool found = new[] { ProfileIdText.Text, ProfileUsernameText.Text, ProfilePlaytimeText.Text, ProfileGroupText.Text, ProfileRankText.Text, ProfileNickChangesText.Text }
                .Any(v => v != "Nicht verfügbar" && v != "—");
            HomeProfileStatus.Text = found
                ? "Spielerübersicht: verfügbare Kontodaten, Spielzeit und Gruppeninformationen. Nicht unterstützte Profilfelder wurden entfernt."
                : "Die API-Verbindung wurde versucht, aber sie lieferte keine auswertbaren Spielerfelder. Öffne Vio-V Konto für die technische API-Diagnose.";
        }
        catch (Exception ex)
        {
            HomeProfileStatus.Text = "Spielerdaten konnten nicht geladen werden: " + ex.Message;
        }
    }

    private static void SetProfileValue(TextBlock target, string json, params string[] aliases)
    {
        string? value = FindProfileValue(json, aliases);
        target.Text = string.IsNullOrWhiteSpace(value) ? "Nicht verfügbar" : value;
    }

    private static void SetNickChangesValue(TextBlock target, string json)
    {
        target.Text = "Nicht verfügbar";
        if (string.IsNullOrWhiteSpace(json)) return;
        try
        {
            using var document = JsonDocument.Parse(json);
            var root = document.RootElement;
            if (root.ValueKind == JsonValueKind.Array)
            {
                target.Text = root.GetArrayLength().ToString();
                return;
            }

            string? explicitCount = FindProfileValueRecursive(root,
                new[] { "count", "total", "total_count", "nickchanges_count", "nick_changes_count", "amount" });
            if (!string.IsNullOrWhiteSpace(explicitCount))
            {
                target.Text = explicitCount;
                return;
            }

            foreach (string key in new[] { "nickchanges", "nick_changes", "changes", "items", "data", "results" })
            {
                if (root.ValueKind == JsonValueKind.Object && root.TryGetProperty(key, out var list) && list.ValueKind == JsonValueKind.Array)
                {
                    target.Text = list.GetArrayLength().ToString();
                    return;
                }
            }

            if (root.ValueKind == JsonValueKind.Number) target.Text = root.ToString();
        }
        catch { /* HTTP-Fehler oder nicht-JSON-Antwort: als nicht verfügbar anzeigen. */ }
    }

    private static string? FindProfileValue(string json, string[] aliases)
    {
        if (string.IsNullOrWhiteSpace(json)) return null;
        try
        {
            using var document = JsonDocument.Parse(json);
            return FindProfileValueRecursive(document.RootElement, aliases);
        }
        catch { return null; }
    }

    private static string? FindProfileValueRecursive(JsonElement element, string[] aliases)
    {
        if (element.ValueKind == JsonValueKind.Object)
        {
            foreach (string alias in aliases)
                foreach (var property in element.EnumerateObject())
                    if (string.Equals(property.Name, alias, StringComparison.OrdinalIgnoreCase))
                    {
                        string? value = property.Value.ValueKind switch
                        {
                            JsonValueKind.String => property.Value.GetString(),
                            JsonValueKind.Number or JsonValueKind.True or JsonValueKind.False => property.Value.ToString(),
                            _ => null
                        };
                        if (!string.IsNullOrWhiteSpace(value)) return value;
                    }
            foreach (var property in element.EnumerateObject())
            {
                var nested = FindProfileValueRecursive(property.Value, aliases);
                if (!string.IsNullOrWhiteSpace(nested)) return nested;
            }
        }
        else if (element.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in element.EnumerateArray())
            {
                var nested = FindProfileValueRecursive(item, aliases);
                if (!string.IsNullOrWhiteSpace(nested)) return nested;
            }
        }
        else if (element.ValueKind == JsonValueKind.String || element.ValueKind == JsonValueKind.Number)
        {
            return element.ToString();
        }
        return null;
    }

    private void FontSizeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (sender is not Slider slider) return;
        int number = slider.Tag is int n ? n : int.TryParse(slider.Tag?.ToString(), out var parsed) ? parsed : 0;
        double size = Math.Clamp(slider.Value, 12, 72);
        switch (number)
        {
            case 1:
                if (_overlay1 is not null) _overlay1.SetFontSize(size);
                if (FontSizeLabel1 is not null) FontSizeLabel1.Text = $"Schriftgröße: {size:0}";
                break;
            case 2:
                if (_overlay2 is not null) _overlay2.SetFontSize(size);
                if (FontSizeLabel2 is not null) FontSizeLabel2.Text = $"Schriftgröße: {size:0}";
                break;
            case 3:
                if (_overlay3 is not null) _overlay3.SetFontSize(size);
                if (FontSizeLabel3 is not null) FontSizeLabel3.Text = $"Schriftgröße: {size:0}";
                break;
        }
    }

    private static void SelectTextColor(ComboBox combo, string hex)
    {
        foreach (var item in combo.Items)
        {
            if (item is ComboBoxItem option && string.Equals(option.Tag?.ToString(), hex, StringComparison.OrdinalIgnoreCase))
            {
                combo.SelectedItem = option;
                return;
            }
        }
        combo.SelectedIndex = 0;
    }

    private void TextColorCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (sender is not ComboBox combo || combo.SelectedItem is not ComboBoxItem item) return;
        var hex = item.Tag?.ToString();
        if (string.IsNullOrWhiteSpace(hex)) return;
        switch (combo.Tag?.ToString())
        {
            case "1": _overlay1?.SetTextColor(hex); break;
            case "2": _overlay2?.SetTextColor(hex); break;
            case "3": _overlay3?.SetTextColor(hex); break;
        }
    }

    private void ShowOverlay1_Click(object sender, RoutedEventArgs e) => ShowOverlay1();
    private void ShowOverlay2_Click(object sender, RoutedEventArgs e) => ShowOverlay2();
    private void ShowOverlay3_Click(object sender, RoutedEventArgs e) => ShowOverlay3();
    private void HideOverlay1_Click(object sender, RoutedEventArgs e) { _overlay1.HideOverlay(); StatusText.Text = "Overlay 1 ausgeblendet."; }
    private void HideOverlay2_Click(object sender, RoutedEventArgs e) { _overlay2.HideOverlay(); StatusText.Text = "Overlay 2 ausgeblendet."; }
    private void HideOverlay3_Click(object sender, RoutedEventArgs e) { _overlay3.HideOverlay(); StatusText.Text = "Overlay 3 ausgeblendet."; }
    private void HideBoth_Click(object sender, RoutedEventArgs e) { HideAllOverlays(); StatusText.Text = "Alle drei Overlays ausgeblendet."; }

    private void ShowOverlay1()
    {
        HideOtherOverlays(_overlay1);
        _overlay1.SetMessage(MessageBox1.Text);
        _overlay1.ShowOverlay();
        _activeOverlay = _overlay1;
        StatusText.Text = "Overlay 1 aktiv.";
    }

    private void ShowOverlay2()
    {
        HideOtherOverlays(_overlay2);
        _overlay2.SetMessage(MessageBox2.Text);
        _overlay2.ShowOverlay();
        _activeOverlay = _overlay2;
        StatusText.Text = "Overlay 2 aktiv.";
    }

    private void ShowOverlay3()
    {
        HideOtherOverlays(_overlay3);
        _overlay3.SetMessage(MessageBox3.Text);
        _overlay3.ShowOverlay();
        _activeOverlay = _overlay3;
        StatusText.Text = "Overlay 3 aktiv.";
    }

    private void HideOtherOverlays(OverlayWindow keep)
    {
        if (keep != _overlay1) _overlay1.HideOverlay();
        if (keep != _overlay2) _overlay2.HideOverlay();
        if (keep != _overlay3) _overlay3.HideOverlay();
    }

    private void HideAllOverlays()
    {
        _overlay1.HideOverlay();
        _overlay2.HideOverlay();
        _overlay3.HideOverlay();
    }

    private void LockOverlays_Changed(object sender, RoutedEventArgs e)
    {
        if (_overlay1 is null || _overlay2 is null || _overlay3 is null || LockOverlaysCheckBox is null) return;
        bool locked = LockOverlaysCheckBox.IsChecked == true;
        _overlay1.SetLocked(locked);
        _overlay2.SetLocked(locked);
        _overlay3.SetLocked(locked);
        if (StatusText is not null)
            StatusText.Text = locked
                ? "Overlays fixiert – Maus wird an das Spiel durchgelassen."
                : "Overlays entsperrt – Verschieben und Skalieren möglich.";
    }

    private void OpenVioV_Click(object sender, RoutedEventArgs e)
    {
        // Reuse the token from the successful startup login; do not launch a second OAuth flow.
        var window = new VioVWindow(existingAccessToken: _vioVAccessToken) { Owner = this };
        window.ShowDialog();
    }

    private void ConfigureChatCommands_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new CustomCommandsWindow(_settings.ChatCommands ?? new List<ChatCommandSetting>())
        {
            Owner = this
        };
        if (dialog.ShowDialog() == true && dialog.ResultCommands is not null)
        {
            _settings.ChatCommands = dialog.ResultCommands;
            SettingsStore.Save(_settings);
            StatusText.Text = $"{_settings.ChatCommands.Count} eigene Chat-Befehle gespeichert.";
        }
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        _settings.Overlay1.Message = MessageBox1.Text;
        _settings.Overlay2.Message = MessageBox2.Text;
        _settings.Overlay3.Message = MessageBox3.Text;
        _settings.Overlay1.FontSize = FontSizeSlider1.Value;
        _settings.Overlay2.FontSize = FontSizeSlider2.Value;
        _settings.Overlay3.FontSize = FontSizeSlider3.Value;
        _settings.Overlay1.IsLocked = _overlay1.IsLocked;
        _settings.Overlay2.IsLocked = _overlay2.IsLocked;
        _settings.Overlay3.IsLocked = _overlay3.IsLocked;
        CaptureGeometry(_overlay1, _settings.Overlay1);
        CaptureGeometry(_overlay2, _settings.Overlay2);
        CaptureGeometry(_overlay3, _settings.Overlay3);
        SettingsStore.Save(_settings);
        StatusText.Text = "Einstellungen für alle drei Overlays gespeichert.";
    }

    private static void CaptureGeometry(OverlayWindow window, OverlayProfile profile)
    {
        profile.Left = window.Left; profile.Top = window.Top;
        profile.Width = window.Width; profile.Height = window.Height;
    }

    private void ShutdownOverlays()
    {
        var handle = new WindowInteropHelper(this).Handle;
        foreach (var id in _registeredHotkeys) UnregisterHotKey(handle, id);
        _registeredHotkeys.Clear();
        _source?.RemoveHook(WndProc);
        _brChatShortcut?.Dispose();
        _brChatShortcut = null;
        _source = null;
        _overlay1.Shutdown();
        _overlay2.Shutdown();
        _overlay3.Shutdown();
    }
}
