using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using synopackage_dotnet.Model.DTOs;
using synopackage_dotnet.Model.Services;

namespace synopackage_dotnet.Controllers
{
  ///<summary></summary>
  [Route("api/[controller]")]
  public class PackagesController : BaseController
  {
    private ISpkService spkService;
    private ISourceService sourceService;
    private IVersionService versionService;
    private IModelService modelService;

    public PackagesController(ISpkService spkService, ISourceService sourceService, IVersionService versionService, IModelService modelService)
    {
      this.spkService = spkService;
      this.sourceService = sourceService;
      this.versionService = versionService;
      this.modelService = modelService;
    }

    [HttpGet("GetSourceServerResponse")]
    public SourceServerResponseDTO GetSourceServerResponse(string sourceName, string model, string version, bool isBeta, string keyword = null)
    {
      ValidateStringParameter(nameof(sourceName), sourceName, 100);
      ValidateStringParameter(nameof(model), model, 100);
      ValidateStringParameter(nameof(version), version, 100);
      ValidateStringParameter(nameof(keyword), keyword, 300);
      SourceServerResponseDTO response = null;
      if (model == null)
        model = AppSettingsProvider.AppSettings.DefaultModel;
      if (version == null)
        version = AppSettingsProvider.AppSettings.DefaultVersion;

      var versionDto = this.versionService.GetVersion(version);
      var sourceDto = this.sourceService.GetSource(sourceName);
      var modelDto = this.modelService.GetModel(model);

      if (sourceDto != null && versionDto != null && modelDto != null)
      {
        response = this.spkService.GetPackages(sourceName,
          sourceDto.Url,
          modelDto.Arch,
          modelDto.Name,
          versionDto,
          isBeta,
          sourceDto.CustomUserAgent,
          keyword);
      }
      else
      {
        ParametersDTO parameters = new ParametersDTO(sourceName, model, version, isBeta);
        response = new SourceServerResponseDTO(false, "Given parameters are not valid", parameters, null);
      }
      return response;
    }
  }
}
