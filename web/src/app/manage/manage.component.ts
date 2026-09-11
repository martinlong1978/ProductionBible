import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';

export type ManageTab = 'projects' | 'episodes' | 'phases' | 'beats' | 'assets';

@Component({
  selector: 'app-manage',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './manage.component.html',
})
export class ManageComponent {
  activeTab: ManageTab = 'projects';

  setTab(tab: ManageTab): void {
    this.activeTab = tab;
  }
}
