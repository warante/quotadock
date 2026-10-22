using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace UsageTray
{
    public sealed class ProviderDefinition
    {
        public string Id, Name, Endpoint, Kind, Help;
        public string[] Environments;
        public Color Accent;
        public string Marker;
        public int PollSeconds;
        public bool AcceptsKey { get { return Kind != "codex" && Kind != "claude" && Kind != "gemini"; } }
        public ProviderDefinition(string id, string name, string endpoint, string kind, string env, string marker, Color color, string help, int interval = 300)
        {
            Id = id; Name = name; Endpoint = endpoint; Kind = kind;
            Environments = env.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
            Marker = marker; Accent = color; Help = help; PollSeconds = interval;
        }
    }

    public static class Providers
    {
        // Fixed, HTTPS-only destinations. Credentials never follow redirects or cross regions.
        public static readonly ProviderDefinition[] All = {
            new ProviderDefinition("openai", "OpenAI / Codex", "https://chatgpt.com/backend-api/wham/usage", "codex", "", "O", Color.MediumSeaGreen, "Inicia sesión con ChatGPT en Codex: codex login. Se lee su auth.json, sin modificarlo.", 60),
            new ProviderDefinition("claude", "Claude", "https://api.anthropic.com/api/oauth/usage", "claude", "", "C", Color.Coral, "Abre Claude Code y ejecuta /login con tu suscripción. Se lee .claude/.credentials.json, sin modificarlo."),
            new ProviderDefinition("opencode-go", "OpenCode Go / Go Plus", "https://opencode.ai/zen/go/v1/usage", "go", "OPENCODE_GO_API_KEY,OPENCODE_API_KEY", "G", Color.LightGray, "Introduce la clave de tu suscripción Go/Go Plus, o define OPENCODE_GO_API_KEY / OPENCODE_API_KEY. OpenCode V2 guarda sus cuentas en SQLite; QuotaDock no abre ni modifica esa base de datos."),
            new ProviderDefinition("kimi", "Kimi Code (global)", "https://api.kimi.ai/coding/v1/usages", "kimi", "KIMI_API_KEY", "K", Color.DeepSkyBlue, "Clave de Kimi Code, no de Moonshot API. Para una cuenta china, activa Kimi Code (China) con su propia clave."),
            new ProviderDefinition("kimi-cn", "Kimi Code (China)", "https://api.kimi.com/coding/v1/usages", "kimi", "KIMI_CN_API_KEY", "K", Color.DodgerBlue, "Clave de Kimi Code para api.kimi.com. No se envía tu clave a la región global."),
            new ProviderDefinition("minimax", "MiniMax Token / M Plan", "https://www.minimax.io/v1/token_plan/remains", "minimax", "MINIMAX_API_KEY", "M", Color.HotPink, "Usa una Subscription Key de MiniMax Token/M Plan, no una clave de pago por uso."),
            new ProviderDefinition("minimax-cn", "MiniMax (China)", "https://api.minimaxi.com/v1/token_plan/remains", "minimax", "MINIMAX_CN_API_KEY", "M", Color.Plum, "Clave de suscripción MiniMax China. No se prueba tu clave en otros hosts."),
            new ProviderDefinition("zai", "Z.AI / GLM Coding", "https://api.z.ai/api/monitor/usage/quota/limit", "zai", "ZAI_API_KEY", "Z", Color.Goldenrod, "Clave del GLM Coding Plan internacional. El endpoint de cuotas es interno."),
            new ProviderDefinition("zhipu", "Zhipu / BigModel (China)", "https://open.bigmodel.cn/api/monitor/usage/quota/limit", "zai", "ZHIPU_API_KEY", "Z", Color.Khaki, "Clave del Coding Plan de BigModel China. No se comparte con Z.AI internacional."),
            new ProviderDefinition("copilot", "GitHub Copilot", "https://api.github.com/copilot_internal/user", "copilot", "GITHUB_COPILOT_TOKEN", "H", Color.MediumPurple, "Necesita un token OAuth con acceso a Copilot. Se admite GITHUB_COPILOT_TOKEN o una credencial guardada aquí. Si falta, se usa gh auth token del GitHub CLI oficial instalado, solo al activar este proveedor. Un PAT genérico puede no servir."),
            new ProviderDefinition("gemini", "Google Gemini CLI", "https://cloudcode-pa.googleapis.com/v1internal:retrieveUserQuota", "gemini", "GEMINI_ACCESS_TOKEN", "A", Color.CornflowerBlue, "Inicia sesión con Google en Gemini CLI. Se lee .gemini/oauth_creds.json, sin renovar tokens. GOOGLE_CLOUD_PROJECT es opcional si Google devuelve tu proyecto. Las barras son Pro y Flash: no representan toda la app Gemini.")
        };
        public static ProviderDefinition Find(string id) { return All.FirstOrDefault(p => p.Id == id); }
    }

    public sealed class Meter
    {
        public string Id, Label;
        public double? Remaining;
        public ResetInfo Reset;
        public Meter(string id, string label, double? remaining, ResetInfo reset = null) { Id = id; Label = label; Remaining = remaining; Reset = reset; }
    }

    public sealed class Snapshot
    {
        public readonly List<Meter> Meters = new List<Meter>();
        public void Add(string id, string label, double? remaining, ResetInfo reset = null) { Meters.Add(new Meter(id, label, remaining, reset)); }
        public static Snapshot Empty(string kind)
        {
            var result = new Snapshot();
            if (kind == "copilot") { result.Add("premium", "Premium/mes", null); result.Add("chat", "Chat/mes", null); }
            else if (kind == "gemini") { result.Add("pro", "Pro", null); result.Add("flash", "Flash", null); }
            else { result.Add("session", "5 h", null); result.Add("weekly", "Semana", null); }
            return result;
        }
        public Meter[] Visible(bool monthly)
        {
            var first = Meters.FirstOrDefault(m => m.Id == "session") ?? Meters.FirstOrDefault(m => m.Id != "weekly" && m.Id != "monthly") ?? new Meter("session", "5 h", null);
            var second = monthly ? Meters.FirstOrDefault(m => m.Id == "monthly") : null;
            if (second == first) second = null;
            second = second ?? Meters.FirstOrDefault(m => m != first);
            return new[] { first ?? new Meter("session", "5 h", null), second ?? new Meter("weekly", "Semana", null) };
        }
    }
}
