import { ChangeDetectorRef, Component, EventEmitter, Input, OnChanges, Output } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ApiClientService } from '../core/api-client.service';
import { ASSET_STATUSES } from '../core/status-style';
import { AssetDto, AssetTypeDto, BeatDto, PhaseDto } from '../core/models';

interface AttributeRow {
  key: string;
  value: string;
}

@Component({
  selector: 'app-asset-editor',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './asset-editor.component.html',
})
export class AssetEditorComponent implements OnChanges {
  @Input({ required: true }) asset!: AssetDto;
  @Input() assetTypes: AssetTypeDto[] = [];
  @Input() phases: PhaseDto[] = [];
  @Input() beats: BeatDto[] = [];
  @Output() saved = new EventEmitter<AssetDto>();
  @Output() cancelled = new EventEmitter<void>();

  protected readonly assetStatuses = ASSET_STATUSES;

  editCode = '';
  editTitle = '';
  editScriptText = '';
  editAssetTypeId = 0;
  editStatus = '';
  editNotes = '';
  editSequenceNumber: number | null = null;
  editTargetLengthSeconds: number | null = null;
  editPhaseId: number | null = null;
  editAttributes: AttributeRow[] = [];
  editBeatIds: number[] = [];

  constructor(private readonly api: ApiClientService, private readonly cdr: ChangeDetectorRef) {}

  ngOnChanges(): void {
    this.editCode = this.asset.code;
    this.editTitle = this.asset.title;
    this.editScriptText = this.asset.scriptText ?? '';
    this.editAssetTypeId = this.asset.assetTypeId;
    this.editStatus = this.asset.status;
    this.editNotes = this.asset.notes ?? '';
    this.editSequenceNumber = this.asset.sequenceNumber;
    this.editTargetLengthSeconds = this.asset.targetLengthSeconds;
    this.editPhaseId = this.asset.phaseId;
    this.editAttributes = Object.entries(this.asset.attributes).map(([key, value]) => ({ key, value }));
    this.editBeatIds = [...this.asset.beatIds];
  }

  addAttributeRow(): void {
    this.editAttributes = [...this.editAttributes, { key: '', value: '' }];
  }

  removeAttributeRow(index: number): void {
    this.editAttributes = this.editAttributes.filter((_, i) => i !== index);
  }

  toggleBeatLink(beatId: number, linked: boolean): void {
    this.editBeatIds = linked
      ? [...this.editBeatIds, beatId]
      : this.editBeatIds.filter((id) => id !== beatId);
  }

  save(): void {
    const attributes: Record<string, string> = {};
    for (const row of this.editAttributes) {
      const key = row.key.trim();
      if (key) attributes[key] = row.value;
    }

    this.api.updateAsset(this.asset.id, {
      assetTypeId: this.editAssetTypeId,
      code: this.editCode,
      title: this.editTitle,
      scriptText: this.editScriptText || null,
      status: this.editStatus,
      notes: this.editNotes || null,
      sequenceNumber: this.editSequenceNumber,
      targetLengthSeconds: this.editTargetLengthSeconds,
      phaseId: this.editPhaseId,
      attributes,
      beatIds: this.editBeatIds,
    }).subscribe((updated) => {
      this.saved.emit(updated);
      this.cdr.markForCheck();
    });
  }

  cancel(): void {
    this.cancelled.emit();
  }
}
