using System.Collections.Generic;
using synopackage_dotnet.Model.DTOs;
namespace synopackage_dotnet.Model.Services
{
  public interface ISpkService : IDomainService
  {
    SourceServerResponseDTO GetPackages(string sourceName, string url, string arch, string model, string major, string minor, string build, bool isBeta, string customUserAgent);

  }
}
