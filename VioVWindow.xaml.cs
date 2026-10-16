using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Windows;

namespace FiveMOverlay;

public partial class VioVWindow : Window
{
    // Public OAuth client previously configured for this application. No client secret is used.
    private const string ClientId = "nxl09YjIbwP2zK8-Op4aq5Uw5wih15mfOtlNzdhL";
    private const string RedirectUri = "http://127.0.0.1:43821/callback/";
    private const string AuthorizeEndpoint = "https://apiv1.vio-v.com/api/oauth2/authorize";
    private const string TokenEndpoint = "https://apiv1.vio-v.com/api/oauth2/token";
    private const string ApiBase = "https://apiv1.vio-v.com/api/v3";
    private readonly HttpClient _http = new() { Timeout = TimeSpan.FromSeconds(25) };
    private string? _accessToken;
    private CancellationTokenSource? _loginCancellation;
    private bool _loginRunning;
    private readonly bool _requireLogin;

    public string? AccessToken => _accessToken;

    public VioVWindow(bool requireLogin = false, string? existingAccessToken = null)
    {
        _requireLogin = requireLogin;
        _accessToken = existingAccessToken;
        InitializeComponent();
        if (_requireLogin)
        {
            Title = "FiveMOverlay | Vio-V Zugang";
            RefreshButton.Visibility = Visibility.Collapsed;
            PlayerNameText.Text = "Anmeldung erforderlich";
            FactionText.Text = "Vio-V OAuth";
            RankText.Text = "PKCE geschützt";
        }
        else if (!string.IsNullOrWhiteSpace(_accessToken))
        {
            Title = "FiveMOverlay | Vio-V Konto";
            LoginButton.Visibility = Visibility.Collapsed;
            RefreshButton.Visibility = Visibility.Visible;
            StatusText.Text = "Angemeldet. Profildaten werden geladen …";
            Loaded += async (_, _) =>
            {
                try { await LoadProfileAsync(); }
                catch (Exception ex) { StatusText.Text = "Profil konnte nicht geladen werden: " + ex.Message; }
            };
        }
        Closed += (_, _) =>
        {
            _loginCancellation?.Cancel();
            _http.Dispose();
        };
    }

    private async void LoginButton_Click(object sender, RoutedEventArgs e)
    {
        if (_loginRunning) return;
        _loginRunning = true;
        LoginButton.IsEnabled = false;
        StatusText.Text = "Warte auf die Anmeldung im Browser …";
        _loginCancellation = new CancellationTokenSource();
        try
        {
            var token = await LoginWithPkceAsync(_loginCancellation.Token);
            _accessToken = token;
            RefreshButton.IsEnabled = true;
            StatusText.Text = "Anmeldung erfolgreich. Lade Profildaten …";
            await LoadProfileAsync();
            if (_requireLogin)
            {
                StatusText.Text = "Zugang bestätigt. FiveMOverlay wird geöffnet …";
                DialogResult = true;
            }
        }
        catch (OperationCanceledException)
        {
            StatusText.Text = "Anmeldung abgebrochen. Ohne Anmeldung bleibt das Programm gesperrt.";
        }
        catch (Exception ex)
        {
            StatusText.Text = "Anmeldung fehlgeschlagen: " + ex.Message;
            RawJsonText.Text = ex.ToString();
        }
        finally
        {
            _loginRunning = false;
            LoginButton.IsEnabled = true;
        }
    }

    private async void RefreshButton_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(_accessToken)) return;
        RefreshButton.IsEnabled = false;
        StatusText.Text = "Aktualisiere Profildaten …";
        try { await LoadProfileAsync(); }
        catch (Exception ex)
        {
            StatusText.Text = "Abruf fehlgeschlagen: " + ex.Message;
            RawJsonText.Text = ex.ToString();
        }
        finally { RefreshButton.IsEnabled = !string.IsNullOrWhiteSpace(_accessToken); }
    }

    private async Task<string> LoginWithPkceAsync(CancellationToken cancellationToken)
    {
        using var listener = new HttpListener();
        listener.Prefixes.Add(RedirectUri);
        listener.Start();

        string verifier = Base64Url(RandomNumberGenerator.GetBytes(64));
        string challenge = Base64Url(SHA256.HashData(Encoding.ASCII.GetBytes(verifier)));
        string state = Base64Url(RandomNumberGenerator.GetBytes(24));
        string scope = Uri.EscapeDataString("read.self read.group");
        string authUrl = AuthorizeEndpoint
            + "?response_type=code"
            + "&client_id=" + Uri.EscapeDataString(ClientId)
            + "&redirect_uri=" + Uri.EscapeDataString(RedirectUri)
            + "&scope=" + scope
            + "&state=" + Uri.EscapeDataString(state)
            + "&code_challenge=" + Uri.EscapeDataString(challenge)
            + "&code_challenge_method=S256";

        Process.Start(new ProcessStartInfo(authUrl) { UseShellExecute = true });
        HttpListenerContext context;
        try { context = await listener.GetContextAsync().WaitAsync(cancellationToken); }
        catch { listener.Stop(); throw; }

        string? returnedState = context.Request.QueryString["state"];
        string? code = context.Request.QueryString["code"];
        string? oauthError = context.Request.QueryString["error"];
        string html;
        if (!string.IsNullOrWhiteSpace(oauthError))
        {
            html = "<html><body style='font-family:Segoe UI;background:#171a22;color:white;padding:30px'><h2>Vio-V Anmeldung nicht abgeschlossen</h2>Du kannst dieses Fenster schließen und zum Overlay zurückkehren.</body></html>";
        }
        else if (!string.Equals(returnedState, state, StringComparison.Ordinal) || string.IsNullOrWhiteSpace(code))
        {
            html = "<html><body style='font-family:Segoe UI;background:#171a22;color:white;padding:30px'><h2>Anmeldung fehlgeschlagen</h2>Ungültige oder fehlende OAuth-Antwort. Du kannst dieses Fenster schließen.</body></html>";
            await WriteBrowserResponseAsync(context, html);
            throw new InvalidOperationException("Die OAuth-Rückgabe war ungültig (state/code fehlt). Prüfe, ob die Redirect-URI in der Vio-V-App exakt http://127.0.0.1:43821/callback/ lautet.");
        }
        else
        {
            html = "<html><body style='font-family:Segoe UI;background:#171a22;color:white;padding:30px'><h2>Vio-V Anmeldung erfolgreich</h2>Du kannst dieses Browserfenster schließen und zum FiveMOverlay zurückkehren.</body></html>";
        }
        await WriteBrowserResponseAsync(context, html);
        if (!string.IsNullOrWhiteSpace(oauthError))
            throw new InvalidOperationException("Vio-V hat die Anmeldung abgelehnt: " + oauthError);

        using var tokenRequest = new HttpRequestMessage(HttpMethod.Post, TokenEndpoint);
        tokenRequest.Content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["grant_type"] = "authorization_code",
            ["client_id"] = ClientId,
            ["redirect_uri"] = RedirectUri,
            ["code"] = code!,
            ["code_verifier"] = verifier
        });
        using var tokenResponse = await _http.SendAsync(tokenRequest, cancellationToken);
        string tokenJson = await tokenResponse.Content.ReadAsStringAsync(cancellationToken);
        if (!tokenResponse.IsSuccessStatusCode)
            throw new InvalidOperationException($"Token-Endpunkt antwortete mit HTTP {(int)tokenResponse.StatusCode}: {tokenJson}");
        using var tokenDoc = JsonDocument.Parse(tokenJson);
        if (!tokenDoc.RootElement.TryGetProperty("access_token", out var accessToken) || string.IsNullOrWhiteSpace(accessToken.GetString()))
            throw new InvalidOperationException("Die Token-Antwort enthielt kein access_token: " + tokenJson);
        return accessToken.GetString()!;
    }

    private static async Task WriteBrowserResponseAsync(HttpListenerContext context, string html)
    {
        byte[] bytes = Encoding.UTF8.GetBytes(html);
        context.Response.StatusCode = 200;
        context.Response.ContentType = "text/html; charset=utf-8";
        context.Response.ContentLength64 = bytes.Length;
        await context.Response.OutputStream.WriteAsync(bytes);
        context.Response.Close();
    }

    private async Task LoadProfileAsync()
    {
        if (string.IsNullOrWhiteSpace(_accessToken)) return;
        var endpoints = new[] { "/self", "/self/playingtime", "/self/nickchanges", "/group", "/group/ranks", "/group/members" };
        var responses = new Dictionary<string, string>();
        foreach (var endpoint in endpoints)
        {
            try
            {
                using var request = new HttpRequestMessage(HttpMethod.Get, ApiBase + endpoint);
                request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _accessToken);
                using var response = await _http.SendAsync(request);
                string body = await response.Content.ReadAsStringAsync();
                responses[endpoint] = response.IsSuccessStatusCode
                    ? body
                    : $"HTTP {(int)response.StatusCode} {response.ReasonPhrase}: {body}";
            }
            catch (Exception ex) { responses[endpoint] = "Fehler: " + ex.Message; }
        }

        string self = responses.GetValueOrDefault("/self", "");
        string playingTime = responses.GetValueOrDefault("/self/playingtime", "");
        string nickChanges = responses.GetValueOrDefault("/self/nickchanges", "");
        string group = responses.GetValueOrDefault("/group", "");
        string ranks = responses.GetValueOrDefault("/group/ranks", "");
        string members = responses.GetValueOrDefault("/group/members", "");

        PlayerNameText.Text = FindValue(self, "name", "player_name", "character_name", "nickname", "username", "display_name") ?? "Nicht im /self erkannt";
        FactionText.Text = FindValue(group, "name", "group_name", "faction_name", "faction", "title") ?? FindValue(self, "group_name", "faction_name", "faction") ?? "Nicht im /group erkannt";
        RankText.Text = FindValue(self, "rank_name", "group_rank_name", "rank", "group_rank", "position")
            ?? FindValue(members, "rank_name", "group_rank_name", "rank", "position")
            ?? FindValue(ranks, "name", "rank_name", "title")
            ?? "Nicht automatisch erkannt";
        StatusText.Text = "API-Abfrage abgeschlossen. Falls ein Feld fehlt, prüfe unten die tatsächlichen API-Antworten.";
        RawJsonText.Text = "GET /self\n" + PrettyJsonOrText(self)
            + "\n\nGET /self/playingtime\n" + PrettyJsonOrText(playingTime)
            + "\n\nGET /self/nickchanges\n" + PrettyJsonOrText(nickChanges)
            + "\n\nGET /group\n" + PrettyJsonOrText(group)
            + "\n\nGET /group/ranks\n" + PrettyJsonOrText(ranks)
            + "\n\nGET /group/members\n" + PrettyJsonOrText(members);
    }

    private static string? FindValue(string json, params string[] names)
    {
        if (string.IsNullOrWhiteSpace(json) || !json.TrimStart().StartsWith('{') && !json.TrimStart().StartsWith('[')) return null;
        try
        {
            using var doc = JsonDocument.Parse(json);
            return FindValueRecursive(doc.RootElement, names);
        }
        catch { return null; }
    }

    private static string? FindValueRecursive(JsonElement element, string[] names)
    {
        if (element.ValueKind == JsonValueKind.Object)
        {
            foreach (var name in names)
            {
                foreach (var property in element.EnumerateObject())
                {
                    if (string.Equals(property.Name, name, StringComparison.OrdinalIgnoreCase))
                    {
                        var value = ScalarToString(property.Value);
                        if (!string.IsNullOrWhiteSpace(value)) return value;
                    }
                }
            }
            foreach (var property in element.EnumerateObject())
            {
                var nested = FindValueRecursive(property.Value, names);
                if (!string.IsNullOrWhiteSpace(nested)) return nested;
            }
        }
        else if (element.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in element.EnumerateArray())
            {
                var nested = FindValueRecursive(item, names);
                if (!string.IsNullOrWhiteSpace(nested)) return nested;
            }
        }
        return null;
    }

    private static string? ScalarToString(JsonElement element) => element.ValueKind switch
    {
        JsonValueKind.String => element.GetString(),
        JsonValueKind.Number => element.ToString(),
        JsonValueKind.True => "true",
        JsonValueKind.False => "false",
        _ => null
    };

    private static string PrettyJsonOrText(string content)
    {
        try { using var doc = JsonDocument.Parse(content); return JsonSerializer.Serialize(doc.RootElement, new JsonSerializerOptions { WriteIndented = true }); }
        catch { return content; }
    }

    private static string Base64Url(byte[] bytes) => Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
}
