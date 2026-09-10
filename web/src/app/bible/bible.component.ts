import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ApiClientService } from '../core/api-client.service';
import { AssetDto, BeatDto, EpisodeDto, UpdateAssetRequest } from '../core/models';
import { TimelineViewComponent } from './timeline-view.component';

@Component({
  selector: 'app-bible',
  standalone: true,
  imports: [CommonModule, FormsModule, TimelineViewComponent],
  templateUrl: './bible.component.html',
})
export class BibleComponent implements OnInit {
  episodes: EpisodeDto[] = [];
  selectedEpisodeId: number | null = null;
  beats: BeatDto[] = [];
  assets: AssetDto[] = [];
  viewMode: 'list' | 'timeline' = 'list';

  constructor(private readonly api: ApiClientService) {}

  ngOnInit(): void {
    this.api.getProjects().subscribe((projects) => {
      const project = projects[0];
      if (!project) return;
      this.api.getEpisodes(project.id).subscribe((episodes) => {
        this.episodes = episodes;
        if (episodes.length > 0) {
          this.selectEpisode(episodes[0].id);
        }
      });
    });
  }

  selectEpisode(episodeId: number): void {
    this.selectedEpisodeId = episodeId;
    this.api.getBeats(episodeId).subscribe((beats) => (this.beats = beats));
    this.api.getAssets(episodeId).subscribe((assets) => (this.assets = assets));
  }

  assetsForBeat(beat: BeatDto): AssetDto[] {
    return this.assets.filter((asset) => beat.assetIds.includes(asset.id));
  }

  get unassignedAssets(): AssetDto[] {
    const linkedIds = new Set(this.beats.flatMap((beat) => beat.assetIds));
    return this.assets.filter((asset) => !linkedIds.has(asset.id));
  }

  toggleTimeline(): void {
    this.viewMode = this.viewMode === 'list' ? 'timeline' : 'list';
  }

  editingAssetId: number | null = null;
  editStatus = '';
  editNotes = '';

  startEdit(asset: AssetDto): void {
    this.editingAssetId = asset.id;
    this.editStatus = asset.status;
    this.editNotes = asset.notes ?? '';
  }

  cancelEdit(): void {
    this.editingAssetId = null;
  }

  saveEdit(asset: AssetDto): void {
    const request: UpdateAssetRequest = {
      assetTypeId: asset.assetTypeId,
      code: asset.code,
      title: asset.title,
      scriptText: asset.scriptText,
      status: this.editStatus,
      notes: this.editNotes || null,
      sequenceNumber: asset.sequenceNumber,
      targetLengthSeconds: asset.targetLengthSeconds,
      attributes: asset.attributes,
      beatIds: asset.beatIds,
    };
    this.api.updateAsset(asset.id, request).subscribe((updated) => {
      const index = this.assets.findIndex((a) => a.id === asset.id);
      if (index !== -1) {
        this.assets[index] = updated;
      }
      this.editingAssetId = null;
    });
  }
}
