import { ComponentFixture, TestBed } from '@angular/core/testing';
import { By } from '@angular/platform-browser';
import { of } from 'rxjs';
import { signal } from '@angular/core';
import { AssetViewComponent } from './asset-view.component';
import { AssetEditorComponent } from '../manage/asset-editor.component';
import { ApiClientService } from '../core/api-client.service';
import { ProjectContextService } from '../core/project-context.service';
import { AssetDto, AssetTypeDto, BeatDto, EpisodeDto, PhaseDto, ProjectDto } from '../core/models';

function asset(overrides: Partial<AssetDto>): AssetDto {
  return {
    id: 1, episodeId: 10, assetTypeId: 1, assetTypeName: 'Shot', code: 'A-01', title: 'Alpha',
    scriptText: null, status: 'Planned', notes: null, sequenceNumber: null, targetLengthSeconds: null,
    phaseId: null, completedAtUtc: null, attributes: {}, beatIds: [], orderInPhase: null,
    ...overrides,
  };
}

describe('AssetViewComponent', () => {
  let fixture: ComponentFixture<AssetViewComponent>;
  let component: AssetViewComponent;
  let apiSpy: jasmine.SpyObj<ApiClientService>;

  const project: ProjectDto = { id: 1, name: 'HalfNut ELS', description: null };
  const episode1: EpisodeDto = { id: 10, projectId: 1, name: 'EP1', orderIndex: 1 };
  const episode2: EpisodeDto = { id: 11, projectId: 1, name: 'EP2', orderIndex: 2 };
  const assetType: AssetTypeDto = { id: 1, name: 'Shot' };
  const phase: PhaseDto = { id: 5, projectId: 1, name: 'Setup A', orderIndex: 0 };
  const beat: BeatDto = {
    id: 100, episodeId: 10, timecode: '00:00', purpose: 'Cold open',
    ordinal: 0, durationSeconds: 38, startSeconds: 0, endSeconds: 38, assetIds: [1000],
  };
  const assetA = asset({ id: 1000, episodeId: 10, code: 'A-01', title: 'Alpha', beatIds: [100] });
  const assetB = asset({ id: 1001, episodeId: 11, code: 'B-01', title: 'Beta' });

  beforeEach(async () => {
    apiSpy = jasmine.createSpyObj('ApiClientService', [
      'getEpisodes', 'getAssets', 'getBeats', 'getAssetTypes', 'getPhases',
    ]);
    apiSpy.getEpisodes.and.returnValue(of([episode1, episode2]));
    apiSpy.getAssetTypes.and.returnValue(of([assetType]));
    apiSpy.getPhases.and.returnValue(of([phase]));
    apiSpy.getAssets.and.callFake((episodeId: number) =>
      of(episodeId === episode1.id ? [assetA] : [assetB]));
    apiSpy.getBeats.and.callFake((episodeId: number) =>
      of(episodeId === episode1.id ? [beat] : []));

    const projectContextStub = {
      projects: signal<ProjectDto[]>([project]),
      selectedProjectId: signal<number | null>(project.id),
      selectProject: jasmine.createSpy('selectProject'),
    };

    await TestBed.configureTestingModule({
      imports: [AssetViewComponent],
      providers: [
        { provide: ApiClientService, useValue: apiSpy },
        { provide: ProjectContextService, useValue: projectContextStub },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(AssetViewComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
    fixture.detectChanges();
  });

  it('merges assets from every episode in the project', () => {
    expect(component.visibleAssets.map((a) => a.id).sort()).toEqual([1000, 1001]);
  });

  it('opens the editor with the clicked asset and its own episode\'s beats', () => {
    component.openEditor(assetA);
    expect(component.selectedAssetForEdit).toEqual(assetA);
    expect(component.beatsByEpisode.get(10)).toEqual([beat]);
  });

  it('updates the row and closes the editor when saved', () => {
    component.openEditor(assetA);
    const updated = { ...assetA, status: 'Shot' };

    component.onEditorSaved(updated);

    expect(component.assets.find((a) => a.id === assetA.id)?.status).toBe('Shot');
    expect(component.selectedAssetForEdit).toBeNull();
  });

  it('closes the editor without saving on closeEditor', () => {
    component.openEditor(assetA);
    component.closeEditor();
    expect(component.selectedAssetForEdit).toBeNull();
  });

  it('toggles sort direction when the same key is set twice', () => {
    component.setSortKey('name');
    expect(component.sortDir).toBe('asc');
    component.setSortKey('name');
    expect(component.sortDir).toBe('desc');
    component.setSortKey('length');
    expect(component.sortDir).toBe('asc');
  });

  it('opens the editor when a rendered row is clicked', () => {
    fixture.detectChanges();
    const rows = fixture.nativeElement.querySelectorAll('tbody tr');
    expect(rows.length).toBe(2);

    const targetIndex = component.visibleAssets.findIndex((a) => a.id === assetA.id);
    (rows[targetIndex] as HTMLElement).click();
    fixture.detectChanges();

    expect(component.selectedAssetForEdit).toEqual(assetA);
  });

  it('passes the clicked asset\'s own episode\'s beats to the rendered editor, not another episode\'s', () => {
    fixture.detectChanges();
    const rows = fixture.nativeElement.querySelectorAll('tbody tr');
    const indexOf = (id: number) => component.visibleAssets.findIndex((a) => a.id === id);

    (rows[indexOf(assetA.id)] as HTMLElement).click();
    fixture.detectChanges();
    let editor = fixture.debugElement.query(By.directive(AssetEditorComponent)).componentInstance as AssetEditorComponent;
    expect(editor.beats).toEqual([beat]);

    (rows[indexOf(assetB.id)] as HTMLElement).click();
    fixture.detectChanges();
    editor = fixture.debugElement.query(By.directive(AssetEditorComponent)).componentInstance as AssetEditorComponent;
    expect(editor.beats).toEqual([]);
  });

  it('closes the editor on Escape when it is open', () => {
    component.openEditor(assetA);
    component.onEscapeKey();
    expect(component.selectedAssetForEdit).toBeNull();
  });

  it('does nothing on Escape when the editor is already closed', () => {
    component.closeEditor();
    expect(() => component.onEscapeKey()).not.toThrow();
    expect(component.selectedAssetForEdit).toBeNull();
  });

  describe('timelineOrderDisplay', () => {
    it('returns the linked beat\'s ordinal as a string', () => {
      component.beatsById = new Map([[100, beat]]);
      expect(component.timelineOrderDisplay(assetA)).toBe('0');
    });

    it('returns an em dash for an asset with no linked beats', () => {
      component.beatsById = new Map([[100, beat]]);
      expect(component.timelineOrderDisplay(assetB)).toBe('—');
    });
  });
});
