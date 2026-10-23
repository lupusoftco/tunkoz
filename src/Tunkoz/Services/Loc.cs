using System.ComponentModel;
using System.Globalization;

namespace Tunkoz.Services;

/// <summary>
/// Tiny localization service. XAML binds with
/// <c>{Binding [Key], Source={x:Static svc:Loc.Instance}}</c>, so switching the language
/// updates every open window live. Strings follow the Claude Design mockups.
/// </summary>
public sealed class Loc : INotifyPropertyChanged
{
    public static Loc Instance { get; } = new();

    public event PropertyChangedEventHandler? PropertyChanged;
    public event EventHandler? LanguageChanged;

    public static readonly string[] Supported = { "tr", "en" };

    private string _language = "en";

    public string Language
    {
        get => _language;
        set
        {
            var lang = Supported.Contains(value) ? value : "en";
            if (lang == _language)
                return;
            _language = lang;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Item[]"));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Language)));
            LanguageChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    public static string SystemDefaultLanguage =>
        CultureInfo.CurrentUICulture.TwoLetterISOLanguageName == "tr" ? "tr" : "en";

    public string this[string key] => T(key);

    public static string T(string key)
    {
        var table = Instance._language == "tr" ? Tr : En;
        if (table.TryGetValue(key, out var value))
            return value;
        return En.TryGetValue(key, out var fallback) ? fallback : key;
    }

    public static string F(string key, params object[] args) => string.Format(T(key), args);

    private static readonly Dictionary<string, string> En = new()
    {
        // Tray
        ["Tray.Active"] = "Active",
        ["Tray.Paused"] = "Paused",
        ["Tray.Settings"] = "Settings",
        ["Tray.About"] = "About",
        ["Tray.Quit"] = "Quit",
        ["Tray.Tooltip.Active"] = "Tünköz — active",
        ["Tray.Tooltip.Paused"] = "Tünköz — paused",
        ["Tray.Started.Title"] = "Tünköz is running",
        ["Tray.Started.Body"] = "Right-click any QR code on screen. Tünköz lives in the notification area.",

        // Card: common
        ["Card.Close"] = "Close",
        ["Card.Copied"] = "Copied",
        ["Card.OpenLink"] = "Open link",
        ["Card.CopyLink"] = "Copy link",
        ["Card.OpenAnyway"] = "Open anyway",
        ["Card.CopyText"] = "Copy text",
        ["Card.WebLink"] = "Web link",
        ["Card.Shortened"] = "shortened",
        ["Card.BlockedType"] = "Blocked link type",
        ["Card.Blocked.Title"] = "This link can’t be opened",
        ["Card.Blocked.Body"] = "javascript:, data: and file: links can run code or reach your files. Tünköz never opens them — you can only copy the text.",
        ["Card.Dangerous.Body"] = "This link opens a Windows system feature or installs an app. Tünköz never opens it — you can only copy the text.",

        // Card: warnings
        ["Warn.known_shortener.Title"] = "Shortened link — real destination hidden",
        ["Warn.known_shortener.Body"] = "We can’t see where it goes. If you’re unsure, copy it first.",
        ["Warn.punycode_domain.Title"] = "Look-alike domain",
        ["Warn.punycode_domain.Body"] = "This domain uses characters that can imitate another address. Check it carefully.",
        ["Warn.plain_http.Title"] = "Unencrypted link (HTTP)",
        ["Warn.plain_http.Body"] = "Anything you send on this page can be read on the way. Don’t enter passwords.",
        ["Warn.raw_ip_host.Title"] = "Address is a bare IP number",
        ["Warn.raw_ip_host.Body"] = "Real sites rarely use raw IP addresses. Open it only if you trust the source.",
        ["Warn.suspicious_pattern.Title"] = "Suspicious-looking link",
        ["Warn.suspicious_pattern.Body"] = "The address is unusually long or obfuscated. If you’re unsure, copy it first.",
        ["Warn.non_http_scheme.Title"] = "Not a web link",
        ["Warn.non_http_scheme.Body"] = "This opens another app on your computer. Open it only if you expected that.",
        ["Warn.default.Title"] = "Be careful with this link",
        ["Warn.default.Body"] = "If you’re unsure, copy it first.",

        // Card: Wi-Fi
        ["Wifi.Type"] = "Wi-Fi network",
        ["Wifi.Password"] = "Password",
        ["Wifi.ShowPassword"] = "Show password",
        ["Wifi.HidePassword"] = "Hide password",
        ["Wifi.CopyPassword"] = "Copy password",
        ["Wifi.CopyName"] = "Copy network name",
        ["Wifi.Open"] = "Open network",

        // Card: contact
        ["Contact.Type"] = "Contact card (vCard)",
        ["Contact.Unnamed"] = "Contact",
        ["Contact.CopyPhone"] = "Copy phone",
        ["Contact.CopyEmail"] = "Copy email",
        ["Contact.Save"] = "Save as .vcf",

        // Card: other types
        ["Text.Type"] = "Plain text · {0} characters",
        ["Tel.Type"] = "Phone number",
        ["Tel.Open"] = "Call",
        ["Mail.Type"] = "Email address",
        ["Mail.Open"] = "Write email",
        ["Sms.Type"] = "Text message",
        ["Sms.Open"] = "Open in messages",
        ["Geo.Type"] = "Location",
        ["Geo.Open"] = "Open in maps",
        ["Common.Copy"] = "Copy",

        // Settings
        ["Settings.WindowTitle"] = "Tünköz — Settings",
        ["Settings.Title"] = "Settings",
        ["Settings.General"] = "General",
        ["Settings.Language"] = "Language",
        ["Settings.Language.Desc"] = "Language of the card, menu and settings",
        ["Settings.Language.System"] = "Windows language",

        // About
        ["About.WindowTitle"] = "About Tünköz",
        ["About.Version"] = "Version {0}",
        ["About.Story"] = "Old Turkic tün (night) + köz (eye): the eye that sees in the dark.",
        ["About.License"] = "MIT License",
        ["About.OpenGitHub"] = "Open on GitHub",
        ["About.Close"] = "Close",
    };

    private static readonly Dictionary<string, string> Tr = new()
    {
        ["Tray.Active"] = "Etkin",
        ["Tray.Paused"] = "Duraklatıldı",
        ["Tray.Settings"] = "Ayarlar",
        ["Tray.About"] = "Hakkında",
        ["Tray.Quit"] = "Çık",
        ["Tray.Tooltip.Active"] = "Tünköz — etkin",
        ["Tray.Tooltip.Paused"] = "Tünköz — duraklatıldı",
        ["Tray.Started.Title"] = "Tünköz çalışıyor",
        ["Tray.Started.Body"] = "Ekrandaki herhangi bir QR koda sağ tıkla. Tünköz bildirim alanında duruyor.",

        ["Card.Close"] = "Kapat",
        ["Card.Copied"] = "Kopyalandı",
        ["Card.OpenLink"] = "Bağlantıya git",
        ["Card.CopyLink"] = "Bağlantıyı kopyala",
        ["Card.OpenAnyway"] = "Yine de git",
        ["Card.CopyText"] = "Metni kopyala",
        ["Card.WebLink"] = "Web bağlantısı",
        ["Card.Shortened"] = "kısaltılmış",
        ["Card.BlockedType"] = "Engellenen bağlantı türü",
        ["Card.Blocked.Title"] = "Bu bağlantı açılamaz",
        ["Card.Blocked.Body"] = "javascript:, data: ve file: bağlantıları kod çalıştırabilir ya da dosyalarına erişebilir. Tünköz bunları hiç açmaz; yalnızca metni kopyalayabilirsin.",
        ["Card.Dangerous.Body"] = "Bu bağlantı bir Windows sistem özelliğini açar ya da uygulama kurar. Tünköz bunu hiç açmaz; yalnızca metni kopyalayabilirsin.",

        ["Warn.known_shortener.Title"] = "Kısaltılmış bağlantı — gerçek adres gizli",
        ["Warn.known_shortener.Body"] = "Nereye gideceğini göremiyoruz. Emin değilsen önce kopyala.",
        ["Warn.punycode_domain.Title"] = "Benzer görünen alan adı",
        ["Warn.punycode_domain.Body"] = "Bu alan adı başka bir adresi taklit edebilecek karakterler içeriyor. Dikkatle kontrol et.",
        ["Warn.plain_http.Title"] = "Şifrelenmemiş bağlantı (HTTP)",
        ["Warn.plain_http.Body"] = "Bu sayfaya gönderdiklerin yolda okunabilir. Şifre girme.",
        ["Warn.raw_ip_host.Title"] = "Adres yalnızca bir IP numarası",
        ["Warn.raw_ip_host.Body"] = "Gerçek siteler nadiren çıplak IP adresi kullanır. Yalnızca kaynağa güveniyorsan aç.",
        ["Warn.suspicious_pattern.Title"] = "Şüpheli görünen bağlantı",
        ["Warn.suspicious_pattern.Body"] = "Adres alışılmadık derecede uzun ya da gizlenmiş. Emin değilsen önce kopyala.",
        ["Warn.non_http_scheme.Title"] = "Web bağlantısı değil",
        ["Warn.non_http_scheme.Body"] = "Bu, bilgisayarında başka bir uygulamayı açar. Yalnızca bunu bekliyorsan aç.",
        ["Warn.default.Title"] = "Bu bağlantıya dikkat et",
        ["Warn.default.Body"] = "Emin değilsen önce kopyala.",

        ["Wifi.Type"] = "Wi-Fi ağı",
        ["Wifi.Password"] = "Şifre",
        ["Wifi.ShowPassword"] = "Şifreyi göster",
        ["Wifi.HidePassword"] = "Şifreyi gizle",
        ["Wifi.CopyPassword"] = "Şifreyi kopyala",
        ["Wifi.CopyName"] = "Ağ adını kopyala",
        ["Wifi.Open"] = "Açık ağ",

        ["Contact.Type"] = "Kişi kartı (vCard)",
        ["Contact.Unnamed"] = "Kişi",
        ["Contact.CopyPhone"] = "Telefonu kopyala",
        ["Contact.CopyEmail"] = "E-postayı kopyala",
        ["Contact.Save"] = ".vcf olarak kaydet",

        ["Text.Type"] = "Düz metin · {0} karakter",
        ["Tel.Type"] = "Telefon numarası",
        ["Tel.Open"] = "Ara",
        ["Mail.Type"] = "E-posta adresi",
        ["Mail.Open"] = "E-posta yaz",
        ["Sms.Type"] = "Kısa mesaj",
        ["Sms.Open"] = "Mesajlarda aç",
        ["Geo.Type"] = "Konum",
        ["Geo.Open"] = "Haritada aç",
        ["Common.Copy"] = "Kopyala",

        ["Settings.WindowTitle"] = "Tünköz — Ayarlar",
        ["Settings.Title"] = "Ayarlar",
        ["Settings.General"] = "Genel",
        ["Settings.Language"] = "Dil",
        ["Settings.Language.Desc"] = "Kart, menü ve ayarların dili",
        ["Settings.Language.System"] = "Windows dili",

        ["About.WindowTitle"] = "Tünköz hakkında",
        ["About.Version"] = "Sürüm {0}",
        ["About.Story"] = "Eski Türkçe tün (gece) + köz (göz): karanlıkta gören göz.",
        ["About.License"] = "MIT Lisansı",
        ["About.OpenGitHub"] = "GitHub’da aç",
        ["About.Close"] = "Kapat",
    };
}
