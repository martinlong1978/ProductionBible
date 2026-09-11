import { ChangeDetectorRef, Component, effect } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ApiClientService } from '../core/api-client.service';
import { ProjectContextService } from '../core/project-context.service';
import { ASSET_STATUSES, statusPillClass } from '../core/status-style';
import { AssetDto, AssetTypeDto, BeatDto, EpisodeDto, PhaseDto } from '../core/models';

interface AttributeRow {
  key: string;
  value: string;
}

@Component({
  selector: 'app-assets-tab',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './assets-tab.component.html',
})
export class AssetsTabComponent {
  episodes: EpisodeDto[] = [];
  selectedEpisodeId: number | null = null;
  assets: AssetDto[] = [];
  assetTypes: AssetTypeDto[] = [];
  phases: PhaseDto[] = [];
  beats: BeatDto[] = [];
  protected readonly assetStatuses = ASSET_STATUSES;
  protected readonly statusPillClass = statusPillClass;

  newCode = '';
  newTitle = '';
  newAssetTypeId: number | null = null;

  editingId: number | null = null;
  editCode = '';
  editTitle = '';
  editScriptText = '';
  editAssetTypeId = 0;
  editStatus = '';
  editNotes = '';
  editSequenceNumber: number | null = null;
  editTargetLengthSeconds: number | null = null;
  editPhaseId: number | null = null;
  editAttributes: AttributeRow[] = [];
  editBeatIds: number[] = [];

  confirmingDeleteId: number | null = null;

  private projectId: number | null = null;

  constructor(
    private readonly api: ApiClientService,
    private readonly cdr: ChangeDetectorRef,
    protected readonly projectContext: ProjectContextService,
  ) {
    this.api.getAssetTypes().subscribe((types) => {
      this.assetTypes = types;
      this.cdr.markForCheck();
    });

    effect(() => {
      const projectId = this.projectContext.selectedProjectId();
      this.projectId = projectId;
      this.editingId = null;
      this.confirmingDeleteId = null;
      if (projectId === null) {
        this.episodes = [];
        this.selectedEpisodeId = null;
        this.assets = [];
        this.phases = [];
        return;
      }
      this.api.getPhases(projectId).subscribe((phases) => {
        if (this.projectId !== projectId) return;
        this.phases = phases;
        this.cdr.markForCheck();
      });
      this.loadEpisodes(projectId);
    });
  }

  private loadEpisodes(projectId: number): void {
    this.api.getEpisodes(projectId).subscribe((episodes) => {
      if (this.projectId !== projectId) return;
      this.episodes = episodes;
      if (episodes.length > 0) this.selectEpisode(episodes[0].id);
      else {
        this.selectedEpisodeId = null;
        this.assets = [];
        this.beats = [];
      }
      this.cdr.markForCheck();
    });
  }

  selectEpisode(episodeId: number): void {
    this.selectedEpisodeId = episodeId;
    this.editingId = null;
    this.confirmingDeleteId = null;
    this.api.getBeats(episodeId).subscribe((beats) => {
      if (this.selectedEpisodeId !== episodeId) return;
      this.beats = beats;
      this.cdr.markForCheck();
    });
    this.loadAssets(episodeId);
  }

  private loadAssets(episodeId: number): void {
    this.api.getAssets(episodeId).subscribe((assets) => {
      if (this.selectedEpisodeId !== episodeId) return;
      this.assets = assets;
      this.cdr.markForCheck();
    });
  }

  create(): void {
    if (this.selectedEpisodeId === null || this.newAssetTypeId === null || !this.newCode.trim()) return;
    this.api.createAsset(this.selectedEpisodeId, {
      assetTypeId: this.newAssetTypeId,
      code: this.newCode,
      title: this.newTitle,
      scriptText: null,
      status: 'Planned',
      notes: null,
      sequenceNumber: null,
      targetLengthSeconds: null,
      phaseId: null,
      attributes: null,
      beatIds: null,
    }).subscribe(() => {
      this.newCode = '';
      this.newTitle = '';
      this.newAssetTypeId = null;
      this.loadAssets(this.selectedEpisodeId!);
      this.cdr.markForCheck();
    });
  }

  startEdit(asset: AssetDto): void {
    this.editingId = asset.id;
    this.editCode = asset.code;
    this.editTitle = asset.title;
    this.editScriptText = asset.scriptText ?? '';
    this.editAssetTypeId = asset.assetTypeId;
    this.editStatus = asset.status;
    this.editNotes = asset.notes ?? '';
    this.editSequenceNumber = asset.sequenceNumber;
    this.editTargetLengthSeconds = asset.targetLengthSeconds;
    this.editPhaseId = asset.phaseId;
    this.editAttributes = Object.entries(asset.attributes).map(([key, value]) => ({ key, value }));
    this.editBeatIds = [...asset.beatIds];
  }

  cancelEdit(): void {
    this.editingId = null;
  }

  addAttributeRow(): void {
    this.editAttributes = [...this.editAttributes, { key: '', value: '' }];
  }

  removeAttributeRow(index: number): void {
    this.editAttributes = this.editAttributes.filter((_, i) => i !== index);
  }

  toggleBeatLink(beatId: number, linked: boolean): void {
    this.editBeatIds = linked
      ? [...this.editBeatIds, beatId]
      : this.editBeatIds.filter((id) => id !== beatId);
  }

  saveEdit(asset: AssetDto): void {
    const attributes: Record<string, string> = {};
    for (const row of this.editAttributes) {
      const key = row.key.trim();
      if (key) attributes[key] = row.value;
    }

    this.api.updateAsset(asset.id, {
      assetTypeId: this.editAssetTypeId,
      code: this.editCode,
      title: this.editTitle,
      scriptText: this.editScriptText || null,
      status: this.editStatus,
      notes: this.editNotes || null,
      sequenceNumber: this.editSequenceNumber,
      targetLengthSeconds: this.editTargetLengthSeconds,
      phaseId: this.editPhaseId,
      attributes,
      beatIds: this.editBeatIds,
    }).subscribe(() => {
      this.editingId = null;
      this.loadAssets(this.selectedEpisodeId!);
      this.cdr.markForCheck();
    });
  }

  confirmDelete(id: number): void {
    if (this.confirmingDeleteId !== id) {
      this.confirmingDeleteId = id;
      return;
    }
    this.api.deleteAsset(id).subscribe(() => {
      this.confirmingDeleteId = null;
      this.loadAssets(this.selectedEpisodeId!);
      this.cdr.markForCheck();
    });
  }

  cancelDelete(): void {
    this.confirmingDeleteId = null;
  }
}
