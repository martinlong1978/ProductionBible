export interface ProjectDto {
  id: number;
  name: string;
  description: string | null;
}

export interface EpisodeDto {
  id: number;
  projectId: number;
  name: string;
  orderIndex: number;
}

export interface AssetTypeDto {
  id: number;
  name: string;
}

export interface BeatDto {
  id: number;
  episodeId: number;
  timecode: string;
  purpose: string;
  ordinal: number;
  durationSeconds: number;
  startSeconds: number;
  endSeconds: number;
  assetIds: number[];
}

export interface CreateBeatRequest {
  timecode: string;
  purpose: string;
  ordinal: number;
  durationSeconds: number;
}

export interface UpdateBeatRequest {
  timecode: string;
  purpose: string;
  ordinal: number;
  durationSeconds: number;
}

export interface AssetDto {
  id: number;
  episodeId: number;
  assetTypeId: number;
  assetTypeName: string;
  code: string;
  title: string;
  scriptText: string | null;
  status: string;
  notes: string | null;
  sequenceNumber: number | null;
  targetLengthSeconds: number | null;
  phaseId: number | null;
  completedAtUtc: string | null;
  attributes: Record<string, string>;
  beatIds: number[];
}

export interface UpdateAssetRequest {
  assetTypeId: number;
  code: string;
  title: string;
  scriptText: string | null;
  status: string;
  notes: string | null;
  sequenceNumber: number | null;
  targetLengthSeconds: number | null;
  phaseId: number | null;
  attributes: Record<string, string> | null;
  beatIds: number[] | null;
}

export interface PhaseDto {
  id: number;
  projectId: number;
  name: string;
  orderIndex: number;
}

export interface CreatePhaseRequest {
  name: string;
  orderIndex: number;
}

export interface UpdatePhaseRequest {
  name: string;
  orderIndex: number;
}

export interface CreateProjectRequest {
  name: string;
  description: string | null;
}

export interface UpdateProjectRequest {
  name: string;
  description: string | null;
}

export interface CreateEpisodeRequest {
  name: string;
  orderIndex: number;
}

export interface UpdateEpisodeRequest {
  name: string;
  orderIndex: number;
}

export interface CreateAssetRequest {
  assetTypeId: number;
  code: string;
  title: string;
  scriptText: string | null;
  status: string;
  notes: string | null;
  sequenceNumber: number | null;
  targetLengthSeconds: number | null;
  phaseId: number | null;
  attributes: Record<string, string> | null;
  beatIds: number[] | null;
}
