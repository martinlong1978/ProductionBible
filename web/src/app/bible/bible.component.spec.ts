import { ComponentFixture, TestBed } from '@angular/core/testing';
import { of } from 'rxjs';
import { BibleComponent } from './bible.component';
import { ApiClientService } from '../core/api-client.service';
import { AssetDto, BeatDto, EpisodeDto, ProjectDto } from '../core/models';

describe('BibleComponent', () => {
  let fixture: ComponentFixture<BibleComponent>;
  let component: BibleComponent;
  let apiSpy: jasmine.SpyObj<ApiClientService>;

  const project: ProjectDto = { id: 1, name: 'HalfNut ELS', description: null };
  const episode: EpisodeDto = { id: 10, projectId: 1, name: 'EP1', orderIndex: 1 };
  const beat: BeatDto = { id: 100, episodeId: 10, timecode: '00:00', purpose: 'Cold open', assetIds: [1000] };
  const linkedAsset: AssetDto = {
    id: 1000, episodeId: 10, assetTypeId: 1, assetTypeName: 'Shot', code: 'A-01',
    title: 'Tool entering the work', scriptText: null, status: 'Planned', notes: null,
    sequenceNumber: 1, targetLengthSeconds: null, completedAtUtc: null, attributes: {}, beatIds: [100],
  };
  const unlinkedAsset: AssetDto = { ...linkedAsset, id: 1001, code: 'A-02', beatIds: [] };

  beforeEach(async () => {
    apiSpy = jasmine.createSpyObj('ApiClientService', ['getProjects', 'getEpisodes', 'getBeats', 'getAssets', 'updateAsset']);
    apiSpy.getProjects.and.returnValue(of([project]));
    apiSpy.getEpisodes.and.returnValue(of([episode]));
    apiSpy.getBeats.and.returnValue(of([beat]));
    apiSpy.getAssets.and.returnValue(of([linkedAsset, unlinkedAsset]));

    await TestBed.configureTestingModule({
      imports: [BibleComponent],
      providers: [{ provide: ApiClientService, useValue: apiSpy }],
    }).compileComponents();

    fixture = TestBed.createComponent(BibleComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('loads the first episode of the first project on init', () => {
    expect(apiSpy.getEpisodes).toHaveBeenCalledWith(1);
    expect(component.selectedEpisodeId).toBe(10);
    expect(component.beats).toEqual([beat]);
  });

  it('groups assets under the beat that links to them', () => {
    expect(component.assetsForBeat(beat)).toEqual([linkedAsset]);
  });

  it('lists assets with no beat link as unassigned', () => {
    expect(component.unassignedAssets).toEqual([unlinkedAsset]);
  });

  it('toggles view mode between list and timeline', () => {
    expect(component.viewMode).toBe('list');
    component.toggleTimeline();
    expect(component.viewMode).toBe('timeline');
    component.toggleTimeline();
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
    }));
    expect(component.assets.find((a) => a.id === linkedAsset.id)?.status).toBe('Shot');
    expect(component.editingAssetId).toBeNull();
  });
});
