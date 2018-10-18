using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Net;
using System.Text;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Hosting.Internal;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using synopackage_dotnet.Model.DTOs;
using synopackage_dotnet.Model.SPK;

namespace synopackage_dotnet.Model.Services
{
  public class CacheService : ICacheService
  {
    IDownloadService downloadService;
    ILogger<CacheService> logger;
    private readonly string defaultIconExtension = "png";
    private readonly string defaultCacheExtension = "cache";

    public CacheService(IDownloadService downloadService, ILogger<CacheService> logger)
    {
      if (!Directory.Exists(AppSettingsProvider.AppSettings.FrontendCacheFolder))
        Directory.CreateDirectory(AppSettingsProvider.AppSettings.FrontendCacheFolder);
      if (!Directory.Exists(AppSettingsProvider.AppSettings.BackendCacheFolder))
        Directory.CreateDirectory(AppSettingsProvider.AppSettings.BackendCacheFolder);
      this.downloadService = downloadService;
      this.logger = logger;
    }

    public void ProcessIconsAsync(string sourceName, List<SpkPackage> packages)
    {
      throw new NotImplementedException();
      // BackgroundTaskQueue queue = new BackgroundTaskQueue();
      // WebClient client = new WebClient();

      // foreach (var package in packages)
      // {
      //   queue.QueueBackgroundWorkItem(async token =>
      //   {

      //     var data = await client.DownloadDataTaskAsync(new Uri(package.Thumbnail[0]));
      //     var fileName = GetIconFileNameWithCacheFolder(sourceName, package.Name);
      //     SaveIcon(fileName, data);
      //   }
      //   );
      // }
    }

    public void ProcessIcons(string sourceName, List<SpkPackage> packages)
    {
      if (packages != null)
      {
        byte[] defaultIconBytes = null;
        foreach (var package in packages)
        {
          if (package.Thumbnail != null && package.Thumbnail.Count > 0)
          {
            if (ShouldStoreIcon(sourceName, package.Name))
            {
              try
              {
                var url = GetValidUrl(package.Thumbnail[0]);
                var extension = Path.GetExtension(url);
                byte[] iconBytes = null;
                if (ShouldDownloadIcon(sourceName, url))
                  iconBytes = downloadService.DownloadData(url);
                if (IsValidIcon(iconBytes))
                {
                  File.WriteAllBytesAsync(GetIconFileNameWithCacheFolder(sourceName, package.Name), iconBytes);
                }
                else
                {
                  if (defaultIconBytes == null)
                    defaultIconBytes = File.ReadAllBytes("wwwroot/assets/package.png"); //TODO: assets folder should be in appsettings
                  File.WriteAllBytesAsync(GetIconFileNameWithCacheFolder(sourceName, package.Name), defaultIconBytes);
                }
              }
              catch (Exception ex)
              {
                logger.LogError(ex, "ProcessIcons - could not download or store icon");
              }
            }
          }
          else if (package.Icon != null && package.Icon.Length > 0)
          {
            if (ShouldStoreIcon(sourceName, package.Name))
            {
              try
              {
                byte[] iconBytes = Convert.FromBase64String(package.Icon);
                File.WriteAllBytesAsync(GetIconFileNameWithCacheFolder(sourceName, package.Name), iconBytes);
              }
              catch (Exception ex)
              {
                logger.LogError(ex, "ProcessIcons - could not convert icon from base 64 or store error");
              }
            }
          }
        }
      }
    }

    private bool ShouldDownloadIcon(string sourceName, string url)
    {
      //performance improvement for synologyitalia (downloading one icon is taking too much time and eventually it fails)
      if (sourceName == "synologyitalia" && url != null && url.Contains("piwik"))
        return false;
      else
        return true;
    }

    private bool IsValidIcon(byte[] iconBytes)
    {
      if (iconBytes == null)
        return false;
      //PNG
      if (iconBytes.Length >= 8
        && iconBytes[0] == 137
        && iconBytes[1] == 80
        && iconBytes[2] == 78
        && iconBytes[3] == 71
        && iconBytes[4] == 13
        && iconBytes[5] == 10
        && iconBytes[6] == 26
        && iconBytes[7] == 10)
        return true;
      //GIF
      else if (iconBytes.Length >= 3
        && iconBytes[0] == char.GetNumericValue('G')
        && iconBytes[1] == char.GetNumericValue('I')
        && iconBytes[2] == char.GetNumericValue('F')
        )
        return true;
      //JFIF  
      else if (iconBytes.Length >= 4
        && iconBytes[0] == char.GetNumericValue('J')
        && iconBytes[1] == char.GetNumericValue('F')
        && iconBytes[2] == char.GetNumericValue('I')
        && iconBytes[3] == char.GetNumericValue('F')
        )
        return true;
      else
      {
        return false;
        // var content = System.Text.Encoding.UTF8.GetString(iconBytes);
        // if (content.Contains("piwik.org")) //a hack for one invalid icon from synologyitalia
        //   return false;
        // else
        //   return true;
      }
    }

    public bool SaveSpkResult(string sourceName, string model, string version, bool isBeta, SpkResult spkResult)
    {
      if (!AppSettingsProvider.AppSettings.CacheSpkServerResponse)
        return false;

      try
      {
        var fileName = GetResponseCacheFile(sourceName, model, version, isBeta);
        var serializedData = JsonConvert.SerializeObject(spkResult);
        File.WriteAllText(fileName, serializedData);
        return true;
      }
      catch (Exception ex)
      {
        logger.LogError(ex, "SaveSpkResult - could not save SPK response to cache");
        return false;
      }
    }


    public string GetIconFileName(string sourceName, string packageName)
    {
      return Utils.CleanFileName($"{sourceName}_{packageName}.{defaultIconExtension}");
    }

    public string GetIconFileNameWithCacheFolder(string sourceName, string packageName)
    {
      return Path.Combine(AppSettingsProvider.AppSettings.FrontendCacheFolder, GetIconFileName(sourceName, packageName));
    }

    public CacheSpkResponseDTO GetSpkResponseFromCache(string sourceName, string model, string version, bool isBeta)
    {
      CacheSpkResponseDTO res = new CacheSpkResponseDTO();
      var fileName = GetResponseCacheFile(sourceName, model, version, isBeta);
      FileInfo fi = new FileInfo(fileName);
      if (!fi.Exists || !AppSettingsProvider.AppSettings.CacheSpkServerResponse || !AppSettingsProvider.AppSettings.CacheSpkServerResponseTimeInHours.HasValue)
      {
        res.Result = false;
        return res;
      }
      TimeSpan ts = DateTime.Now - fi.LastWriteTime;
      if (ts.TotalHours <= AppSettingsProvider.AppSettings.CacheSpkServerResponseTimeInHours.Value)
      {
        try
        {
          var content = File.ReadAllText(fileName);
          var deserializedData = JsonConvert.DeserializeObject<SpkResult>(content);
          res.Result = true;
          res.SpkResult = deserializedData;
          return res;
        }
        catch (Exception ex)
        {
          logger.LogError(ex, "GetSpkResponseFromCache - could not get SPK response from cache");
          res.Result = false;
          return res;
        }
      }
      else
      {
        res.Result = false;
        return res;
      }
    }

    private string GetResponseCacheFile(string sourceName, string model, string version, bool isBeta)
    {
      var channelString = isBeta ? "beta" : "stable";
      return Path.Combine(AppSettingsProvider.AppSettings.BackendCacheFolder, Utils.CleanFileName($"{sourceName}_{model}_{version}_{channelString}.{defaultCacheExtension}"));
    }

    private bool ShouldStoreIcon(string sourceName, string packageName)
    {
      if (!File.Exists(GetIconFileNameWithCacheFolder(sourceName, packageName)))
        return true;
      else
      {
        if (AppSettingsProvider.AppSettings.CacheIconExpirationInDays.HasValue)
        {
          FileInfo fi = new FileInfo(GetIconFileNameWithCacheFolder(sourceName, packageName));
          TimeSpan ts = DateTime.Now - fi.LastWriteTime;
          if (ts.TotalDays <= AppSettingsProvider.AppSettings.CacheIconExpirationInDays.Value)
            return true;
        }
        return false;
      }
    }
    private string GetValidUrl(string urlCandidate, bool useSsl = true)
    {
      string protocol = useSsl ? "https" : "http";
      if (string.IsNullOrWhiteSpace(urlCandidate))
        return null;
      else if (urlCandidate.StartsWith("http", true, CultureInfo.InvariantCulture))
        return urlCandidate;
      else if (urlCandidate.StartsWith("//"))
        return $"{protocol}:{urlCandidate}";
      else
        return $"{protocol}://{urlCandidate}";
    }



  }
}