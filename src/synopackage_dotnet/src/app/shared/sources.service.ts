import { Observable } from 'rxjs';
import { Injectable } from '@angular/core';
import { UserSettingsService } from './user-settings.service';
import { HttpClient } from '@angular/common/http';
import { SourceDTO, PackageDTO } from '../sources/sources.model';
import { Config } from './config';
import { Utils } from './Utils';

@Injectable({
    providedIn: 'root',
  })
export class SourcesService {
    constructor(private http: HttpClient, private userSettingsService: UserSettingsService) {

    }

    public getAllSources(): Observable<SourceDTO[]> {
        console.log('getAllSources');
        return this.http.get<SourceDTO[]>(`${Config.apiUrl}Sources/GetList`);
    }

    public getPackagesFromSource(sourceName: string, model: string, version: string, isBeta: boolean): Observable<PackageDTO[]> {
        const params = new SourceBrowseDTO();
        params.sourceName = sourceName;
        params.model = model;
        params.version = version;
        params.isBeta = isBeta;
        return this.http.get<PackageDTO[]>(`${Config.apiUrl}Pacakges/GetList${Utils.getQueryParams(params)}`);
    }
}

export class SourceBrowseDTO {
    sourceName: string;
    model: string;
    version: string;
    isBeta: boolean;
}
