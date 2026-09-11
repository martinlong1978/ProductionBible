import { ChangeDetectorRef, Component, effect } from '@angular/core';
import { CommonModule } from '@angular/common';
import { forkJoin } from 'rxjs';
import { CdkDragDrop, DragDropModule, moveItemInArray } from '@angular/cdk/drag-drop';
import { ApiClientService } from '../core/api-client.service';
import { ProjectContextService } from '../core/project-context.service';
import { statusPillClass } from '../core/status-style';
import { buildPhaseGroups, PhaseGroup } from './phase-grouping';
import { AssetDto } from '../core/models';

@Component({
  selector: 'app-production-plan',
  standalone: true,
  imports: [CommonModule, DragDropModule],
  templateUrl: './production-plan.component.html',
})
export class ProductionPlanComponent {
  phaseGroups: PhaseGroup[] = [];
  unphasedGroup: PhaseGroup | null = null;
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

  onPhaseDrop(event: CdkDragDrop<PhaseGroup[]>): void {
    if (!event.isPointerOverContainer) return;
    if (event.previousIndex === event.currentIndex) return;
    const projectId = this.projectContext.selectedProjectId();
    if (projectId === null) return;
    moveItemInArray(this.phaseGroups, event.previousIndex, event.currentIndex);
    const orderedIds = this.phaseGroups.map((g) => g.phaseId!);
    this.cdr.markForCheck();
    this.api.reorderPhases(projectId, orderedIds).subscribe(() => {
      this.loadGroups(projectId);
    });
  }

  onShotDrop(group: PhaseGroup, event: CdkDragDrop<AssetDto[]>): void {
    if (!event.isPointerOverContainer) return;
    if (event.previousIndex === event.currentIndex) return;
    if (group.phaseId === null) return;
    const phaseId = group.phaseId;
    const projectId = this.projectContext.selectedProjectId();
    moveItemInArray(group.assets, event.previousIndex, event.currentIndex);
    const orderedIds = group.assets.map((a) => a.id);
    this.cdr.markForCheck();
    this.api.reorderAssetsWithinPhase(phaseId, orderedIds).subscribe(() => {
      if (projectId !== null) this.loadGroups(projectId);
    });
  }

  private loadGroups(projectId: number): void {
    forkJoin({
      episodes: this.api.getEpisodes(projectId),
      phases: this.api.getPhases(projectId),
    }).subscribe(({ episodes, phases }) => {
      if (this.projectContext.selectedProjectId() !== projectId) return;
      if (episodes.length === 0) {
        this.phaseGroups = [];
        this.unphasedGroup = null;
        this.cdr.markForCheck();
        return;
      }

      forkJoin(episodes.map((episode) => this.api.getAssets(episode.id))).subscribe((assetLists) => {
        if (this.projectContext.selectedProjectId() !== projectId) return;
        const { phaseGroups, unphasedGroup } = buildPhaseGroups(assetLists.flat(), phases);
        this.phaseGroups = phaseGroups;
        this.unphasedGroup = unphasedGroup;
        this.cdr.markForCheck();
      });
      this.cdr.markForCheck();
    });
  }
}
