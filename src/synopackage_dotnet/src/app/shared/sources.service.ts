import { Observable } from 'rxjs';
import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { SourceDTO, SourcesDTO, SourceServerResponseDTO } from '../sources/sources.model';
import { Config } from './config';
import { Utils } from './Utils';
import { ParametersDTO } from './model';

@Injectable({
  providedIn: 'root',
})
export class SourcesService {
  constructor(private http: HttpClient) {

  }

  public getAllSources(): Observable<SourcesDTO> {
    return this.http.get<SourcesDTO>(`${Config.apiUrl}Sources/GetAllSources`);
  }

  public getPackagesFromSource(sourceName: string, model: string, version: string, isBeta: boolean, keyword: string):
    Observable<SourceServerResponseDTO> {
    const params = new ParametersDTO();
    params.sourceName = sourceName;
    params.model = model;
    params.version = version;
    params.isBeta = isBeta;
    params.keyword = keyword;
    return this.http.get<SourceServerResponseDTO>(`${Config.apiUrl}Packages/GetSourceServerResponse${Utils.getQueryParams(params)}`);
  }

  public getAllActiveSources(): Observable<SourceDTO[]> {
    return this.http.get<SourceDTO[]>(`${Config.apiUrl}Sources/GetAllActiveSources`);
  }
}
