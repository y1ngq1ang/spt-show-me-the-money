using System.Threading.Tasks;
using SPTushonka.Common.Http;

namespace SwiftXP.SPT.Common.Http;

public class SPTRequestHandler(ISPTHttpClient sptHttpClient) : ISPTRequestHandler
{
    public ISPTHttpClient HttpClient => sptHttpClient;

    public string Host => RequestHandler.Host;

    public Task<string> GetJsonAsync(string url)
    {
        return RequestHandler.GetJsonAsync(url);
    }
}