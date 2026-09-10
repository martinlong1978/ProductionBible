import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { AssetDto, AssetTypeDto, BeatDto, EpisodeDto, ProjectDto, UpdateAssetRequest } from './models';

@Injectable({ providedIn: 'root' })
export class ApiClientService {
  constructor(private readonly http: HttpClient) {}

  getProjects(): Observable<ProjectDto[]> {
    return this.http.get<ProjectDto[]>('/api/projects');
  }

  getEpisodes(projectId: number): Observable<EpisodeDto[]> {
    return this.http.get<EpisodeDto[]>(`/api/projects/${projectId}/episodes`);
  }

  getBeats(episodeId: number): Observable<BeatDto[]> {
    return this.http.get<BeatDto[]>(`/api/episodes/${episodeId}/beats`);
  }

  getAssets(episodeId: number): Observable<AssetDto[]> {
    return this.http.get<AssetDto[]>(`/api/episodes/${episodeId}/assets`);
  }

  getAssetTypes(): Observable<AssetTypeDto[]> {
    return this.http.get<AssetTypeDto[]>('/api/asset-types');
  }

  updateAsset(id: number, request: UpdateAssetRequest): Observable<AssetDto> {
    return this.http.put<AssetDto>(`/api/assets/${id}`, request);
  }
}
