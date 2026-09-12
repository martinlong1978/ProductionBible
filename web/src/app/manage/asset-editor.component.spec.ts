import { ComponentFixture, TestBed } from '@angular/core/testing';
import { of } from 'rxjs';
import { AssetEditorComponent } from './asset-editor.component';
import { ApiClientService } from '../core/api-client.service';
import { AssetDto, BeatDto } from '../core/models';

describe('AssetEditorComponent', () => {
  let fixture: ComponentFixture<AssetEditorComponent>;
  let component: AssetEditorComponent;
  let apiSpy: jasmine.SpyObj<ApiClientService>;

  const beat: BeatDto = {
    id: 100, episodeId: 10, timecode: '00:00', purpose: 'Cold open',
    ordinal: 0, durationSeconds: 38, startSeconds: 0, endSeconds: 38, assetIds: [1000],
  };
  const asset: AssetDto = {
    id: 1000, episodeId: 10, assetTypeId: 1, assetTypeName: 'Shot', code: 'A-01',
    title: 'Tool entering the work', scriptText: null, status: 'Planned', notes: null,
    sequenceNumber: 1, targetLengthSeconds: null, phaseId: 5, completedAtUtc: null,
    attributes: { Location: 'Workshop' }, beatIds: [100], orderInPhase: null,
  };

  beforeEach(async () => {
    apiSpy = jasmine.createSpyObj('ApiClientService', ['updateAsset']);

    await TestBed.configureTestingModule({
      imports: [AssetEditorComponent],
      providers: [{ provide: ApiClientService, useValue: apiSpy }],
    }).compileComponents();

    fixture = TestBed.createComponent(AssetEditorComponent);
    component = fixture.componentInstance;
    component.asset = asset;
    component.beats = [beat];
    component.ngOnChanges();
    fixture.detectChanges();
  });

  it('populates the full field set including attributes and beat links from the asset input', () => {
    expect(component.editCode).toBe('A-01');
    expect(component.editAttributes).toEqual([{ key: 'Location', value: 'Workshop' }]);
    expect(component.editBeatIds).toEqual([100]);
  });

  it('re-populates when the asset input changes', () => {
    const otherAsset: AssetDto = { ...asset, id: 1001, code: 'A-02', attributes: {} };
    component.asset = otherAsset;
    component.ngOnChanges();
    expect(component.editCode).toBe('A-02');
    expect(component.editAttributes).toEqual([]);
  });

  it('saves, rebuilding the attributes map from the editable rows, and emits the updated asset', () => {
    const updated = { ...asset, status: 'Shot' };
    apiSpy.updateAsset.and.returnValue(of(updated));
    component.editAttributes = [{ key: 'Location', value: 'Studio' }, { key: 'Notes', value: 'Reshoot' }];
    component.editBeatIds = [100];
    let emitted: AssetDto | undefined;
    component.saved.subscribe((a) => (emitted = a));

    component.save();

    expect(apiSpy.updateAsset).toHaveBeenCalledWith(1000, jasmine.objectContaining({
      attributes: { Location: 'Studio', Notes: 'Reshoot' },
      beatIds: [100],
    }));
    expect(emitted).toEqual(updated);
  });

  it('emits cancelled on cancel', () => {
    let cancelled = false;
    component.cancelled.subscribe(() => (cancelled = true));
    component.cancel();
    expect(cancelled).toBeTrue();
  });
});
