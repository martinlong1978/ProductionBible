import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { forkJoin } from 'rxjs';
import { ApiClientService } from '../core/api-client.service';
import { AssetDto } from '../core/models';

interface PhaseGroup {
  phase: string;
  assets: AssetDto[];
}

@Component({
  selector: 'app-production-plan',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './production-plan.component.html',
})
export class ProductionPlanComponent implements OnInit {
  groups: PhaseGroup[] = [];

  constructor(private readonly api: ApiClientService) {}

  ngOnInit(): void {
    this.api.getProjects().subscribe((projects) => {
      const project = projects[0];
      if (!project) return;

      this.api.getEpisodes(project.id).subscribe((episodes) => {
        if (episodes.length === 0) {
          this.groups = [];
          return;
        }

        forkJoin(episodes.map((episode) => this.api.getAssets(episode.id))).subscribe((assetLists) => {
          this.groups = this.buildGroups(assetLists.flat());
        });
      });
    });
  }

  private buildGroups(assets: AssetDto[]): PhaseGroup[] {
    const sorted = [...assets].sort(
      (a, b) => (a.sequenceNumber ?? Number.MAX_SAFE_INTEGER) - (b.sequenceNumber ?? Number.MAX_SAFE_INTEGER));

    const map = new Map<string, AssetDto[]>();
    for (const asset of sorted) {
      const phase = asset.attributes['PhaseGroup'] ?? 'Unphased';
      if (!map.has(phase)) map.set(phase, []);
      map.get(phase)!.push(asset);
    }

    return Array.from(map.entries()).map(([phase, assets]) => ({ phase, assets }));
  }
}
