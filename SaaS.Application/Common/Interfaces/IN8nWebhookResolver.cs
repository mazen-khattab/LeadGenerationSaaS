namespace SaaS.Application.Common.Interfaces
{
    /// <summary>
    /// Resolves the concrete n8n webhook URL for a given bot identifier/code.
    /// Implementations should map bot codes (e.g. UiModuleCode) to configured webhook endpoints.
    /// </summary>
    public interface IN8nWebhookResolver
    {
        /// <summary>
        /// Returns the full webhook URL for the provided botCode.
        /// </summary>
        /// <param name="botCode">Bot code / identifier used to select an n8n webhook.</param>
        string GetWebhookUrl(string botCode);
    }
}
