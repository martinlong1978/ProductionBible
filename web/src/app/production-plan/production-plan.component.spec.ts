import { ComponentFixture, TestBed } from '@angular/core/testing';
import { of } from 'rxjs';
import { ProductionPlanComponent } from './production-plan.component';
import { ApiClientService } from '../core/api-client.service';
import { AssetDto, EpisodeDto, ProjectDto } from '../core/models';

describe('ProductionPlanComponent', () => {
  let fixture: ComponentFixture<ProductionPlanComponent>;
  let component: ProductionPlanComponent;
  let apiSpy: jasmine.SpyObj<ApiClientService>;

  const project: ProjectDto = { id: 1, name: 'HalfNut ELS', description: null };
  const episode1: EpisodeDto = { id: 10, projectId: 1, name: 'EP1', orderIndex: 1 };
  const episode2: EpisodeDto = { id: 20, projectId: 1, name: 'EP2', orderIndex: 2 };

  function asset(id: number, code: string, sequenceNumber: number, phase: string): AssetDto {
    return {
      id, episodeId: 10, assetTypeId: 1, assetTypeName: 'Shot', code, title: code,
      scriptText: null, status: 'Planned', notes: null, sequenceNumber, targetLengthSeconds: null,
      completedAtUtc: null, attributes: { PhaseGroup: phase }, beatIds: [],
    };
  }

  beforeEach(async () => {
    apiSpy = jasmine.createSpyObj('ApiClientService', ['getProjects', 'getEpisodes', 'getAssets']);
    apiSpy.getProjects.and.returnValue(of([project]));
    apiSpy.getEpisodes.and.returnValue(of([episode1, episode2]));
    apiSpy.getAssets.and.callFake((episodeId: number) =>
      episodeId === 20
        ? of([asset(2, 'F-01', 1, 'Phase 1: The software time machine')])
        : of([asset(1, 'B-02', 2, 'Phase 2: Makerspace trip')]));

    await TestBed.configureTestingModule({
      imports: [ProductionPlanComponent],
      providers: [{ provide: ApiClientService, useValue: apiSpy }],
    }).compileComponents();

    fixture = TestBed.createComponent(ProductionPlanComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('merges assets from every episode in the project', () => {
    const allCodes = component.groups.flatMap((g) => g.assets.map((a) => a.code));
    expect(allCodes).toEqual(['F-01', 'B-02']);
  });

  it('orders phase groups by the lowest sequence number in that phase', () => {
    expect(component.groups.map((g) => g.phase)).toEqual([
      'Phase 1: The software time machine',
      'Phase 2: Makerspace trip',
    ]);
  });

  it('falls back to Unphased when an asset has no PhaseGroup attribute', () => {
    apiSpy.getAssets.and.returnValue(of([{
      id: 3, episodeId: 10, assetTypeId: 1, assetTypeName: 'Shot', code: 'X-01', title: 'X-01',
      scriptText: null, status: 'Planned', notes: null, sequenceNumber: 1, targetLengthSeconds: null,
      completedAtUtc: null, attributes: {}, beatIds: [],
    }]));

    component.ngOnInit();

    expect(component.groups.some((g) => g.phase === 'Unphased')).toBeTrue();
  });
});
