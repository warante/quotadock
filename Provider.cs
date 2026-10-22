namespace UsageTray
{
    internal static class Provider
    {
#if CLAUDE
        public static readonly bool IsClaude = true;
        public const string AppName = "QuotaDock.Claude";
        public const string InstanceName = "ClaudeUsageTray";
        public const string Name = "Claude";
        public const string Service = "Anthropic";
        public const string Login = "Claude Code con tu suscripción de Claude (abre claude y ejecuta /login)";
        public const string Endpoint = "https://api.anthropic.com/api/oauth/usage";
#else
        public static readonly bool IsClaude = false;
        public const string AppName = "QuotaDock.OpenAI";
        public const string InstanceName = "CodexUsageTray";
        public const string Name = "Codex";
        public const string Service = "OpenAI";
        public const string Login = "Codex con tu cuenta de ChatGPT (codex login)";
        public const string Endpoint = "https://chatgpt.com/backend-api/wham/usage";
#endif
    }
}
