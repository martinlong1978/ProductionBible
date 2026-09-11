import { ComponentFixture, TestBed } from '@angular/core/testing';
import { of } from 'rxjs';
import { signal } from '@angular/core';
import { ProductionPlanComponent } from './production-plan.component';
import { ApiClientService } from '../core/api-client.service';
import { ProjectContextService } from '../core/project-context.service';
import { AssetDto, EpisodeDto, ProjectDto } from '../core/models';
import { buildPhaseGroups } from './phase-grouping';

function asset(id: number, code: string, sequenceNumber: number, phase: string | undefined, episodeId = 10): AssetDto {
  return {
    id, episodeId, assetTypeId: 1, assetTypeName: 'Shot', code, title: code,
    scriptText: null, status: 'Planned', notes: null, sequenceNumber, targetLengthSeconds: null,
    phaseId: null, completedAtUtc: null, attributes: phase ? { PhaseGroup: phase } : {}, beatIds: [], orderInPhase: null,
  };
}

describe('buildPhaseGroups', () => {
  it('orders assets by sequence number across groups', () => {
    const groups = buildPhaseGroups([
      asset(2, 'F-01', 1, 'Phase 1: The software time machine'),
      asset(1, 'B-02', 2, 'Phase 2: Makerspace trip'),
    ]);
    const allCodes = groups.flatMap((g) => g.assets.map((a) => a.code));
    expect(allCodes).toEqual(['F-01', 'B-02']);
  });

  it('orders phase groups by the lowest sequence number in that phase', () => {
    const groups = buildPhaseGroups([
      asset(2, 'F-01', 1, 'Phase 1: The software time machine'),
      asset(1, 'B-02', 2, 'Phase 2: Makerspace trip'),
    ]);
    expect(groups.map((g) => g.phase)).toEqual([
      'Phase 1: The software time machine',
      'Phase 2: Makerspace trip',
    ]);
  });

  it('falls back to Unphased when an asset has no PhaseGroup attribute', () => {
    const groups = buildPhaseGroups([asset(3, 'X-01', 1, undefined)]);
    expect(groups.some((g) => g.phase === 'Unphased')).toBeTrue();
  });
});

describe('ProductionPlanComponent', () => {
  let fixture: ComponentFixture<ProductionPlanComponent>;
  let component: ProductionPlanComponent;
  let apiSpy: jasmine.SpyObj<ApiClientService>;

  const project: ProjectDto = { id: 1, name: 'HalfNut ELS', description: null };
  const episode1: EpisodeDto = { id: 10, projectId: 1, name: 'EP1', orderIndex: 1 };
  const episode2: EpisodeDto = { id: 11, projectId: 1, name: 'EP2', orderIndex: 2 };

  beforeEach(async () => {
    apiSpy = jasmine.createSpyObj('ApiClientService', ['getEpisodes', 'getAssets']);
    apiSpy.getEpisodes.and.returnValue(of([episode1, episode2]));
    apiSpy.getAssets.and.callFake((episodeId: number) => {
      if (episodeId === episode1.id) {
        return of([asset(2, 'F-01', 1, 'Phase 1: The software time machine', episode1.id)]);
      }
      return of([asset(1, 'B-02', 2, 'Phase 2: Makerspace trip', episode2.id)]);
    });

    const projectContextStub = {
      projects: signal<ProjectDto[]>([project]),
      selectedProjectId: signal<number | null>(project.id),
      selectProject: jasmine.createSpy('selectProject'),
    };

    await TestBed.configureTestingModule({
      imports: [ProductionPlanComponent],
      providers: [
        { provide: ApiClientService, useValue: apiSpy },
        { provide: ProjectContextService, useValue: projectContextStub },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(ProductionPlanComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
    fixture.detectChanges();
  });

  it('loads phase groups for the selected project on construction', () => {
    expect(apiSpy.getEpisodes).toHaveBeenCalledWith(1);
    expect(component.groups.map((g) => g.phase)).toEqual([
      'Phase 1: The software time machine',
      'Phase 2: Makerspace trip',
    ]);
  });

  it('merges assets from every episode in the project', () => {
    const allCodes = component.groups.flatMap((g) => g.assets.map((a) => a.code));
    expect(allCodes).toEqual(['F-01', 'B-02']);
  });
});
