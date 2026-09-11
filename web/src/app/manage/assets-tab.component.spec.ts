import { ComponentFixture, TestBed } from '@angular/core/testing';
import { signal } from '@angular/core';
import { of } from 'rxjs';
import { AssetsTabComponent } from './assets-tab.component';
import { ApiClientService } from '../core/api-client.service';
import { ProjectContextService } from '../core/project-context.service';
import { AssetDto, AssetTypeDto, BeatDto, EpisodeDto, PhaseDto } from '../core/models';

describe('AssetsTabComponent', () => {
  let fixture: ComponentFixture<AssetsTabComponent>;
  let component: AssetsTabComponent;
  let apiSpy: jasmine.SpyObj<ApiClientService>;

  const episode: EpisodeDto = { id: 10, projectId: 1, name: 'EP1', orderIndex: 1 };
  const assetType: AssetTypeDto = { id: 1, name: 'Shot' };
  const phase: PhaseDto = { id: 5, projectId: 1, name: 'Setup A', orderIndex: 0 };
  const beat: BeatDto = {
    id: 100, episodeId: 10, timecode: '00:00', purpose: 'Cold open',
    ordinal: 0, durationSeconds: 38, startSeconds: 0, endSeconds: 38, assetIds: [1000],
  };
  const asset: AssetDto = {
    id: 1000, episodeId: 10, assetTypeId: 1, assetTypeName: 'Shot', code: 'A-01',
    title: 'Tool entering the work', scriptText: null, status: 'Planned', notes: null,
    sequenceNumber: 1, targetLengthSeconds: null, phaseId: 5, completedAtUtc: null,
    attributes: { Location: 'Workshop' }, beatIds: [100],
  };

  beforeEach(async () => {
    apiSpy = jasmine.createSpyObj('ApiClientService', [
      'getEpisodes', 'getAssets', 'getAssetTypes', 'getPhases', 'getBeats',
      'createAsset', 'updateAsset', 'deleteAsset',
    ]);
    apiSpy.getEpisodes.and.returnValue(of([episode]));
    apiSpy.getAssets.and.returnValue(of([asset]));
    apiSpy.getAssetTypes.and.returnValue(of([assetType]));
    apiSpy.getPhases.and.returnValue(of([phase]));
    apiSpy.getBeats.and.returnValue(of([beat]));

    const contextStub = { selectedProjectId: signal<number | null>(1) };

    await TestBed.configureTestingModule({
      imports: [AssetsTabComponent],
      providers: [
        { provide: ApiClientService, useValue: apiSpy },
        { provide: ProjectContextService, useValue: contextStub },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(AssetsTabComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('loads episode, asset type, phase, and asset data for the selected project', () => {
    expect(apiSpy.getEpisodes).toHaveBeenCalledWith(1);
    expect(apiSpy.getAssetTypes).toHaveBeenCalled();
    expect(apiSpy.getPhases).toHaveBeenCalledWith(1);
    expect(component.selectedEpisodeId).toBe(10);
    expect(apiSpy.getAssets).toHaveBeenCalledWith(10);
    expect(component.assets).toEqual([asset]);
    expect(component.beats).toEqual([beat]);
  });

  it('starts an edit with the full field set including attributes and beat links', () => {
    component.startEdit(asset);
    expect(component.editCode).toBe('A-01');
    expect(component.editAttributes).toEqual([{ key: 'Location', value: 'Workshop' }]);
    expect(component.editBeatIds).toEqual([100]);
  });

  it('saves an edit, rebuilding the attributes map from the editable rows', () => {
    apiSpy.updateAsset.and.returnValue(of(asset));
    component.startEdit(asset);
    component.editAttributes = [{ key: 'Location', value: 'Studio' }, { key: 'Notes', value: 'Reshoot' }];
    component.editBeatIds = [100];

    component.saveEdit(asset);

    expect(apiSpy.updateAsset).toHaveBeenCalledWith(1000, jasmine.objectContaining({
      attributes: { Location: 'Studio', Notes: 'Reshoot' },
      beatIds: [100],
    }));
  });

  it('creates an asset for the selected episode and reloads', () => {
    apiSpy.createAsset.and.returnValue(of({ ...asset, id: 1001, code: 'A-02' }));
    component.newCode = 'A-02';
    component.newTitle = 'New shot';
    component.newAssetTypeId = 1;

    component.create();

    expect(apiSpy.createAsset).toHaveBeenCalledWith(10, jasmine.objectContaining({
      code: 'A-02', title: 'New shot', assetTypeId: 1, status: 'Planned',
    }));
    expect(apiSpy.getAssets).toHaveBeenCalledTimes(2);
  });

  it('deletes on the second confirm click', () => {
    apiSpy.deleteAsset.and.returnValue(of(undefined));

    component.confirmDelete(asset.id);
    expect(apiSpy.deleteAsset).not.toHaveBeenCalled();

    component.confirmDelete(asset.id);
    expect(apiSpy.deleteAsset).toHaveBeenCalledWith(asset.id);
  });
});
