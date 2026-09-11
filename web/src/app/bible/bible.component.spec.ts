import { ComponentFixture, TestBed } from '@angular/core/testing';
import { of } from 'rxjs';
import { signal } from '@angular/core';
import { BibleComponent } from './bible.component';
import { ApiClientService } from '../core/api-client.service';
import { ProjectContextService } from '../core/project-context.service';
import { AssetDto, BeatDto, EpisodeDto, ProjectDto } from '../core/models';

describe('BibleComponent', () => {
  let fixture: ComponentFixture<BibleComponent>;
  let component: BibleComponent;
  let apiSpy: jasmine.SpyObj<ApiClientService>;

  const project: ProjectDto = { id: 1, name: 'HalfNut ELS', description: null };
  const episode: EpisodeDto = { id: 10, projectId: 1, name: 'EP1', orderIndex: 1 };
  const beat: BeatDto = { id: 100, episodeId: 10, timecode: '00:00', purpose: 'Cold open', ordinal: 0, durationSeconds: 38, startSeconds: 0, endSeconds: 38, assetIds: [1000] };
  const linkedAsset: AssetDto = {
    id: 1000, episodeId: 10, assetTypeId: 1, assetTypeName: 'Shot', code: 'A-01',
    title: 'Tool entering the work', scriptText: null, status: 'Planned', notes: null,
    sequenceNumber: 1, targetLengthSeconds: null, phaseId: 5, completedAtUtc: null, attributes: {}, beatIds: [100],
  };
  const unlinkedAsset: AssetDto = { ...linkedAsset, id: 1001, code: 'A-02', beatIds: [] };

  beforeEach(async () => {
    apiSpy = jasmine.createSpyObj('ApiClientService', [
      'getEpisodes', 'getBeats', 'getAssets', 'updateAsset', 'reorderBeats', 'reorderAssetsWithinBeat',
    ]);
    apiSpy.getEpisodes.and.returnValue(of([episode]));
    apiSpy.getBeats.and.returnValue(of([beat]));
    apiSpy.getAssets.and.returnValue(of([linkedAsset, unlinkedAsset]));

    const projectContextStub = {
      projects: signal<ProjectDto[]>([project]),
      selectedProjectId: signal<number | null>(project.id),
      selectProject: jasmine.createSpy('selectProject'),
    };

    await TestBed.configureTestingModule({
      imports: [BibleComponent],
      providers: [
        { provide: ApiClientService, useValue: apiSpy },
        { provide: ProjectContextService, useValue: projectContextStub },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(BibleComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
    fixture.detectChanges();
  });

  it('loads the first episode of the selected project on construction', () => {
    expect(apiSpy.getEpisodes).toHaveBeenCalledWith(1);
    expect(component.selectedEpisodeId).toBe(10);
    expect(component.beats).toEqual([beat]);
  });

  it('groups assets under the beat that links to them, in the beat\'s own asset order', () => {
    const secondBeat: BeatDto = { id: 101, episodeId: 10, timecode: '00:38', purpose: 'Next', ordinal: 1, durationSeconds: 30, startSeconds: 38, endSeconds: 68, assetIds: [1001, 1000] };
    component.beats = [beat, secondBeat];
    component.assets = [linkedAsset, unlinkedAsset];

    // beat.assetIds is [1000] (unchanged from the top-level fixture); secondBeat's is
    // deliberately reversed ([1001, 1000]) relative to `this.assets`' own order, to prove
    // assetsForBeat follows beat.assetIds' order rather than this.assets' fetch order.
    expect(component.assetsForBeat(beat)).toEqual([linkedAsset]);
    expect(component.assetsForBeat(secondBeat).map((a) => a.id)).toEqual([1001, 1000]);
  });

  it('lists assets with no beat link as unassigned', () => {
    expect(component.unassignedAssets).toEqual([unlinkedAsset]);
  });

  it('sets the view mode via setViewMode', () => {
    expect(component.viewMode).toBe('list');
    component.setViewMode('timeline');
    expect(component.viewMode).toBe('timeline');
    component.setViewMode('list');
    expect(component.viewMode).toBe('list');
  });

  it('saves inline edits to status and notes via updateAsset', () => {
    const updated: AssetDto = { ...linkedAsset, status: 'Shot', notes: 'Went well' };
    apiSpy.updateAsset.and.returnValue(of(updated));

    component.startEdit(linkedAsset);
    component.editStatus = 'Shot';
    component.editNotes = 'Went well';
    component.saveEdit(linkedAsset);

    expect(apiSpy.updateAsset).toHaveBeenCalledWith(linkedAsset.id, jasmine.objectContaining({
      status: 'Shot',
      notes: 'Went well',
      code: linkedAsset.code,
      beatIds: linkedAsset.beatIds,
      phaseId: linkedAsset.phaseId,
    }));
    expect(component.assets.find((a) => a.id === linkedAsset.id)?.status).toBe('Shot');
    expect(component.editingAssetId).toBeNull();
  });

  it('reorders beats in place and calls reorderBeats with the new order', () => {
    const secondBeat: BeatDto = { id: 101, episodeId: 10, timecode: '00:38', purpose: 'Next', ordinal: 1, durationSeconds: 30, startSeconds: 38, endSeconds: 68, assetIds: [] };
    component.beats = [beat, secondBeat];
    apiSpy.reorderBeats.and.returnValue(of(undefined));
    // Model a real backend: after the reorder is persisted, the reload triggered by
    // onBeatDrop's success callback fetches beats back in the new order.
    apiSpy.getBeats.and.returnValue(of([secondBeat, beat]));

    component.onBeatDrop({ previousIndex: 0, currentIndex: 1, isPointerOverContainer: true } as any);

    expect(component.beats.map((b) => b.id)).toEqual([101, 100]);
    expect(apiSpy.reorderBeats).toHaveBeenCalledWith(10, [101, 100]);
  });

  it('reorders a beat\'s asset links in place and calls reorderAssetsWithinBeat with the new order', () => {
    const multiBeat: BeatDto = { id: 102, episodeId: 10, timecode: '01:00', purpose: 'Setup', ordinal: 2, durationSeconds: 20, startSeconds: 68, endSeconds: 88, assetIds: [1000, 1001] };
    component.assets = [linkedAsset, unlinkedAsset];
    apiSpy.reorderAssetsWithinBeat.and.returnValue(of(undefined));

    component.onAssetDrop(multiBeat, { previousIndex: 0, currentIndex: 1, isPointerOverContainer: true } as any);

    expect(multiBeat.assetIds).toEqual([1001, 1000]);
    expect(apiSpy.reorderAssetsWithinBeat).toHaveBeenCalledWith(102, [1001, 1000]);
  });

  it('reloads the episode after a successful beat reorder', () => {
    const secondBeat: BeatDto = { id: 101, episodeId: 10, timecode: '00:38', purpose: 'Next', ordinal: 1, durationSeconds: 30, startSeconds: 38, endSeconds: 68, assetIds: [] };
    component.beats = [beat, secondBeat];
    apiSpy.reorderBeats.and.returnValue(of(undefined));
    apiSpy.getBeats.calls.reset();
    apiSpy.getAssets.calls.reset();

    component.onBeatDrop({ previousIndex: 0, currentIndex: 1, isPointerOverContainer: true } as any);

    expect(apiSpy.getBeats).toHaveBeenCalledWith(10);
    expect(apiSpy.getAssets).toHaveBeenCalledWith(10);
  });
});
