import { ComponentFixture, TestBed } from '@angular/core/testing';
import { TimelineViewComponent } from './timeline-view.component';
import { AssetDto, BeatDto } from '../core/models';

describe('TimelineViewComponent', () => {
  let fixture: ComponentFixture<TimelineViewComponent>;
  let component: TimelineViewComponent;

  function asset(id: number, assetTypeName: string, code: string): AssetDto {
    return {
      id, episodeId: 1, assetTypeId: 1, assetTypeName, code, title: code,
      scriptText: null, status: 'Planned', notes: null, sequenceNumber: null,
      targetLengthSeconds: null, phaseId: null, completedAtUtc: null, attributes: {}, beatIds: [],
    };
  }

  const beats: BeatDto[] = [
    { id: 1, episodeId: 1, timecode: '00:00', purpose: 'Cold open', assetIds: [1, 2] },
    { id: 2, episodeId: 1, timecode: '02:00', purpose: 'Graphic', assetIds: [3] },
  ];
  const assets: AssetDto[] = [
    asset(1, 'PieceToCamera', 'E-S'),
    asset(2, 'Shot', 'A-01'),
    asset(3, 'Animation', 'g1_gears'),
  ];

  beforeEach(async () => {
    await TestBed.configureTestingModule({ imports: [TimelineViewComponent] }).compileComponents();
    fixture = TestBed.createComponent(TimelineViewComponent);
    component = fixture.componentInstance;
    component.beats = beats;
    component.assets = assets;
    component.ngOnChanges();
    fixture.detectChanges();
  });

  it('builds one row per beat, in timecode order', () => {
    expect(component.timeline.rows.map((r) => r.beat.timecode)).toEqual(['00:00', '02:00']);
  });

  it('assigns assets to the correct tracks', () => {
    expect(component.timeline.rows[0].tracks['dialogue'].map((c) => c.asset.code)).toEqual(['E-S']);
    expect(component.timeline.rows[0].tracks['broll'].map((c) => c.asset.code)).toEqual(['A-01']);
    expect(component.timeline.rows[1].tracks['graphics'].map((c) => c.asset.code)).toEqual(['g1_gears']);
  });

  it('re-derives the timeline when inputs change via ngOnChanges', () => {
    component.beats = [{ id: 3, episodeId: 1, timecode: '10:00', purpose: 'Later', assetIds: [] }];
    component.assets = [];
    component.ngOnChanges();

    expect(component.timeline.rows.map((r) => r.beat.timecode)).toEqual(['10:00']);
  });
});
