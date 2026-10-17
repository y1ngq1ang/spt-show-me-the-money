using System.Threading.Tasks;

namespace SwiftXP.SPT.Common.Http;

public interface ISPTRequestHandler
{
    ISPTHttpClient HttpClient { get; }

    string Host { get; }

    Task<string> GetJsonAsync(string url);
}