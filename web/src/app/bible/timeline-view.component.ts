import { Component, Input, OnChanges } from '@angular/core';
import { CommonModule } from '@angular/common';
import { AssetDto, BeatDto } from '../core/models';
import { statusPillClass } from '../core/status-style';
import { buildTimeline, TimelineResult, TrackId, TRACK_ORDER } from './timeline-model';

@Component({
  selector: 'app-timeline-view',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './timeline-view.component.html',
})
export class TimelineViewComponent implements OnChanges {
  @Input() beats: BeatDto[] = [];
  @Input() assets: AssetDto[] = [];

  timeline: TimelineResult = { rows: [], unscheduled: [], unassigned: [] };
  protected readonly statusPillClass = statusPillClass;
  protected readonly trackOrder = TRACK_ORDER;
  protected readonly trackLabels: Record<TrackId, string> = {
    dialogue: 'Dialogue',
    broll: 'B-Roll',
    screen: 'Screen',
    graphics: 'Graphics',
  };

  ngOnChanges(): void {
    this.timeline = buildTimeline(this.beats, this.assets);
  }
}
