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
  assetIds: number[];
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
