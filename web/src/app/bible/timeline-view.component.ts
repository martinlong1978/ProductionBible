import { Component, Input, OnChanges } from '@angular/core';
import { CommonModule } from '@angular/common';
import { AssetDto, BeatDto } from '../core/models';
import { statusPillClass } from '../core/status-style';

interface TimelineLane {
  name: string;
  assets: AssetDto[];
}

const LANE_BY_ASSET_TYPE: Record<string, string> = {
  PieceToCamera: 'A-Roll',
  Shot: 'B-Roll',
  Animation: 'Animations',
  Title: 'Titles',
};

const LANE_ORDER = ['A-Roll', 'B-Roll', 'Animations', 'Titles', 'Other'];

@Component({
  selector: 'app-timeline-view',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './timeline-view.component.html',
})
export class TimelineViewComponent implements OnChanges {
  @Input() beats: BeatDto[] = [];
  @Input() assets: AssetDto[] = [];

  lanes: TimelineLane[] = [];
  protected readonly statusPillClass = statusPillClass;

  ngOnChanges(): void {
    this.lanes = this.buildLanes();
  }

  private buildLanes(): TimelineLane[] {
    const orderedAssetIds = this.beats.flatMap((beat) => beat.assetIds);
    const orderIndex = new Map(orderedAssetIds.map((id, index) => [id, index]));

    const grouped = new Map<string, AssetDto[]>();
    for (const asset of this.assets) {
      const laneName = LANE_BY_ASSET_TYPE[asset.assetTypeName] ?? 'Other';
      if (!grouped.has(laneName)) grouped.set(laneName, []);
      grouped.get(laneName)!.push(asset);
    }

    for (const list of grouped.values()) {
      list.sort((a, b) => {
        const aIndex = orderIndex.has(a.id) ? orderIndex.get(a.id)! : Number.MAX_SAFE_INTEGER;
        const bIndex = orderIndex.has(b.id) ? orderIndex.get(b.id)! : Number.MAX_SAFE_INTEGER;
        return aIndex - bIndex;
      });
    }

    return LANE_ORDER.filter((name) => grouped.has(name)).map((name) => ({
      name,
      assets: grouped.get(name)!,
    }));
  }
}
