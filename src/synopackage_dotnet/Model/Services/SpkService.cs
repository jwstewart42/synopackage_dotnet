using System.Collections.Generic;
using System.Net;
using ExpressMapper;
using ExpressMapper.Extensions;
using RestSharp;
using synopackage_dotnet.Model.DTOs;
using synopackage_dotnet.Model.SPK;

namespace synopackage_dotnet.Model.Services
{
    public class SpkService : ISpkService
    {
        private ICacheService cacheService;
        public SpkService(ICacheService cacheService)
        {
            this.cacheService = cacheService;
        }
        public IEnumerable<PackageDTO> GetPackages(string sourceName, string url, string arch, string model, string major, string minor, string build, bool isBeta, string customUserAgent, out string errorMessage)
        {
            errorMessage = null;
            var client = new RestClient(url);
            if (!string.IsNullOrEmpty(AppSettingsProvider.AppSettings.ProxyUrl))
            {
                client.Proxy = new WebProxy(AppSettingsProvider.AppSettings.ProxyUrl);
            }
            var request = new RestRequest(Method.POST);
            var unique = $"synology_{arch}_{model}";

            request.AddParameter("language", "enu");
            request.AddParameter("unique", unique);
            request.AddParameter("arch", arch);
            request.AddParameter("major", major);
            request.AddParameter("minor", minor);
            request.AddParameter("build", build);
            request.AddParameter("pacakge_update_channel", isBeta ? "beta": "stable");
            request.AddParameter("timezone", "Brussels");
            request.AddHeader("User-Agent", customUserAgent != null ? customUserAgent : unique);
            var response = client.Execute<SpkResult>(request);

            if (response.ResponseStatus == ResponseStatus.Completed && response.Data != null)
            {
                this.cacheService.ProcessIconsAsync(sourceName, response.Data.Packages);
                List<PackageDTO> list = new List<PackageDTO>();
                if (response.Data.Packages == null)
                    return list;
                foreach (var spkPackage in response.Data.Packages)
                {
                    PackageDTO package = new PackageDTO();
                    spkPackage.Map(package);
                    package.CachedIconLocation = cacheService.GetFileNameWithCacheFolder(sourceName, package.Name);
                    list.Add(package);
                }
                list.Sort();
                return list;
            }
            errorMessage = response.ErrorMessage;
            return null;

        }
    }
}
