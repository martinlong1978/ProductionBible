import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ProjectsTabComponent } from './projects-tab.component';
import { EpisodesTabComponent } from './episodes-tab.component';
import { PhasesTabComponent } from './phases-tab.component';
import { BeatsTabComponent } from './beats-tab.component';
import { AssetsTabComponent } from './assets-tab.component';

export type ManageTab = 'projects' | 'episodes' | 'phases' | 'beats' | 'assets';

@Component({
  selector: 'app-manage',
  standalone: true,
  imports: [CommonModule, ProjectsTabComponent, EpisodesTabComponent, PhasesTabComponent, BeatsTabComponent, AssetsTabComponent],
  templateUrl: './manage.component.html',
})
export class ManageComponent {
  activeTab: ManageTab = 'projects';

  setTab(tab: ManageTab): void {
    this.activeTab = tab;
  }
}
