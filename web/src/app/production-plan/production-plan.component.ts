import { ChangeDetectorRef, Component, effect } from '@angular/core';
import { CommonModule } from '@angular/common';
import { forkJoin } from 'rxjs';
import { ApiClientService } from '../core/api-client.service';
import { ProjectContextService } from '../core/project-context.service';
import { statusPillClass } from '../core/status-style';
import { buildPhaseGroups, PhaseGroup } from './phase-grouping';

@Component({
  selector: 'app-production-plan',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './production-plan.component.html',
})
export class ProductionPlanComponent {
  groups: PhaseGroup[] = [];
  protected readonly statusPillClass = statusPillClass;

  constructor(
    private readonly api: ApiClientService,
    private readonly cdr: ChangeDetectorRef,
    protected readonly projectContext: ProjectContextService,
  ) {
    effect(() => {
      const projectId = this.projectContext.selectedProjectId();
      if (projectId !== null) {
        this.loadGroups(projectId);
      }
    });
  }

  private loadGroups(projectId: number): void {
    this.api.getEpisodes(projectId).subscribe((episodes) => {
      if (this.projectContext.selectedProjectId() !== projectId) return;
      if (episodes.length === 0) {
        this.groups = [];
        this.cdr.markForCheck();
        return;
      }

      forkJoin(episodes.map((episode) => this.api.getAssets(episode.id))).subscribe((assetLists) => {
        if (this.projectContext.selectedProjectId() !== projectId) return;
        this.groups = buildPhaseGroups(assetLists.flat());
        this.cdr.markForCheck();
      });
      this.cdr.markForCheck();
    });
  }
}
