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
      targetLengthSeconds: null, completedAtUtc: null, attributes: {}, beatIds: [],
    };
  }

  const beats: BeatDto[] = [
    { id: 1, episodeId: 1, timecode: '00:00', purpose: 'Cold open', assetIds: [2, 1] },
    { id: 2, episodeId: 1, timecode: '02:00', purpose: 'Graphic', assetIds: [3] },
  ];
  const assets: AssetDto[] = [
    asset(1, 'Shot', 'A-01'),
    asset(2, 'PieceToCamera', 'E-S'),
    asset(3, 'Animation', 'g1_gears'),
    asset(4, 'Title', 't1_title'),
    asset(5, 'Flyover', 'f1'),
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

  it('groups assets into lanes by asset type', () => {
    const laneNames = component.lanes.map((lane) => lane.name);
    expect(laneNames).toEqual(['A-Roll', 'B-Roll', 'Animations', 'Titles', 'Other']);
  });

  it('orders assets within the B-Roll lane by beat sequence, not asset id', () => {
    const bRoll = component.lanes.find((lane) => lane.name === 'B-Roll')!;
    expect(bRoll.assets.map((a) => a.code)).toEqual(['A-01']);
  });

  it('orders the A-Roll lane correctly when the beat lists the asset before others', () => {
    const aRoll = component.lanes.find((lane) => lane.name === 'A-Roll')!;
    expect(aRoll.assets.map((a) => a.code)).toEqual(['E-S']);
  });

  it('puts an asset type with no lane mapping into Other', () => {
    const other = component.lanes.find((lane) => lane.name === 'Other')!;
    expect(other.assets.map((a) => a.code)).toEqual(['f1']);
  });
});
