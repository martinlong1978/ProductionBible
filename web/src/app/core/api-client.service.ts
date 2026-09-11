import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import {
  AssetDto, AssetTypeDto, BeatDto, CreateAssetRequest, CreateBeatRequest, CreateEpisodeRequest,
  CreatePhaseRequest, CreateProjectRequest, EpisodeDto, PhaseDto, ProjectDto, UpdateAssetRequest,
  UpdateBeatRequest, UpdateEpisodeRequest, UpdatePhaseRequest, UpdateProjectRequest,
} from './models';

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

  createProject(request: CreateProjectRequest): Observable<ProjectDto> {
    return this.http.post<ProjectDto>('/api/projects', request);
  }

  updateProject(id: number, request: UpdateProjectRequest): Observable<ProjectDto> {
    return this.http.put<ProjectDto>(`/api/projects/${id}`, request);
  }

  deleteProject(id: number): Observable<void> {
    return this.http.delete<void>(`/api/projects/${id}`);
  }

  createEpisode(projectId: number, request: CreateEpisodeRequest): Observable<EpisodeDto> {
    return this.http.post<EpisodeDto>(`/api/projects/${projectId}/episodes`, request);
  }

  updateEpisode(id: number, request: UpdateEpisodeRequest): Observable<EpisodeDto> {
    return this.http.put<EpisodeDto>(`/api/episodes/${id}`, request);
  }

  deleteEpisode(id: number): Observable<void> {
    return this.http.delete<void>(`/api/episodes/${id}`);
  }

  getPhases(projectId: number): Observable<PhaseDto[]> {
    return this.http.get<PhaseDto[]>(`/api/projects/${projectId}/phases`);
  }

  createPhase(projectId: number, request: CreatePhaseRequest): Observable<PhaseDto> {
    return this.http.post<PhaseDto>(`/api/projects/${projectId}/phases`, request);
  }

  updatePhase(id: number, request: UpdatePhaseRequest): Observable<PhaseDto> {
    return this.http.put<PhaseDto>(`/api/phases/${id}`, request);
  }

  deletePhase(id: number): Observable<void> {
    return this.http.delete<void>(`/api/phases/${id}`);
  }

  createBeat(episodeId: number, request: CreateBeatRequest): Observable<BeatDto> {
    return this.http.post<BeatDto>(`/api/episodes/${episodeId}/beats`, request);
  }

  updateBeat(id: number, request: UpdateBeatRequest): Observable<BeatDto> {
    return this.http.put<BeatDto>(`/api/beats/${id}`, request);
  }

  deleteBeat(id: number): Observable<void> {
    return this.http.delete<void>(`/api/beats/${id}`);
  }

  createAsset(episodeId: number, request: CreateAssetRequest): Observable<AssetDto> {
    return this.http.post<AssetDto>(`/api/episodes/${episodeId}/assets`, request);
  }

  deleteAsset(id: number): Observable<void> {
    return this.http.delete<void>(`/api/assets/${id}`);
  }

  reorderBeats(episodeId: number, orderedIds: number[]): Observable<void> {
    return this.http.patch<void>(`/api/episodes/${episodeId}/beats/reorder`, { orderedIds });
  }

  reorderAssetsWithinBeat(beatId: number, orderedIds: number[]): Observable<void> {
    return this.http.patch<void>(`/api/beats/${beatId}/asset-beats/reorder`, { orderedIds });
  }
}
