import { ComponentFixture, TestBed } from '@angular/core/testing';
import { of } from 'rxjs';
import { signal } from '@angular/core';
import { ProductionPlanComponent } from './production-plan.component';
import { ApiClientService } from '../core/api-client.service';
import { ProjectContextService } from '../core/project-context.service';
import { AssetDto, EpisodeDto, PhaseDto, ProjectDto } from '../core/models';
import { buildPhaseGroups } from './phase-grouping';

function asset(
  id: number, code: string, sequenceNumber: number | null, phaseId: number | null,
  episodeId = 10, orderInPhase: number | null = null,
): AssetDto {
  return {
    id, episodeId, assetTypeId: 1, assetTypeName: 'Shot', code, title: code,
    scriptText: null, status: 'Planned', notes: null, sequenceNumber, targetLengthSeconds: null,
    phaseId, orderInPhase, completedAtUtc: null, attributes: {}, beatIds: [],
  };
}

describe('buildPhaseGroups', () => {
  const phase1: PhaseDto = { id: 1, projectId: 1, name: 'Phase 1: The software time machine', orderIndex: 0 };
  const phase2: PhaseDto = { id: 2, projectId: 1, name: 'Phase 2: Makerspace trip', orderIndex: 1 };

  it('groups assets by phaseId, not by any attribute', () => {
    const { phaseGroups } = buildPhaseGroups(
      [asset(2, 'F-01', 1, phase1.id), asset(1, 'B-02', 2, phase2.id)],
      [phase1, phase2],
    );
    expect(phaseGroups.map((g) => g.assets.map((a) => a.code))).toEqual([['F-01'], ['B-02']]);
  });

  it('orders phase groups by Phase.orderIndex, not by first-seen order in the asset list', () => {
    const { phaseGroups } = buildPhaseGroups(
      [asset(1, 'B-02', 2, phase2.id), asset(2, 'F-01', 1, phase1.id)],
      [phase1, phase2],
    );
    expect(phaseGroups.map((g) => g.phase)).toEqual([
      'Phase 1: The software time machine',
      'Phase 2: Makerspace trip',
    ]);
  });

  it('orders assets within a phase by orderInPhase, falling back to sequenceNumber', () => {
    const { phaseGroups } = buildPhaseGroups(
      [
        asset(1, 'A-01', 10, phase1.id, 10, null),
        asset(2, 'A-02', 5, phase1.id, 10, 1),
        asset(3, 'A-03', 1, phase1.id, 10, 0),
      ],
      [phase1],
    );
    expect(phaseGroups[0].assets.map((a) => a.code)).toEqual(['A-03', 'A-02', 'A-01']);
  });

  it('buckets assets with no phaseId into a separate unphasedGroup', () => {
    const { phaseGroups, unphasedGroup } = buildPhaseGroups([asset(3, 'X-01', 1, null)], []);
    expect(phaseGroups).toEqual([]);
    expect(unphasedGroup).not.toBeNull();
    expect(unphasedGroup!.assets.map((a) => a.code)).toEqual(['X-01']);
    expect(unphasedGroup!.phaseId).toBeNull();
  });

  it('returns a null unphasedGroup when every asset has a phaseId', () => {
    const { unphasedGroup } = buildPhaseGroups([asset(1, 'A-01', 1, phase1.id)], [phase1]);
    expect(unphasedGroup).toBeNull();
  });

  it('includes a phase group with an empty assets array for a phase with no linked assets yet', () => {
    const { phaseGroups } = buildPhaseGroups([], [phase1]);
    expect(phaseGroups).toEqual([{ phaseId: phase1.id, phase: phase1.name, assets: [] }]);
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
