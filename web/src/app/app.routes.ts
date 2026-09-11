import { Routes } from '@angular/router';
import { BibleComponent } from './bible/bible.component';
import { ProductionPlanComponent } from './production-plan/production-plan.component';
import { ManageComponent } from './manage/manage.component';

export const routes: Routes = [
  { path: '', redirectTo: 'storyboard', pathMatch: 'full' },
  { path: 'storyboard', component: BibleComponent },
  { path: 'bible', redirectTo: 'storyboard', pathMatch: 'full' },
  { path: 'production-plan', component: ProductionPlanComponent },
  { path: 'manage', component: ManageComponent },
];
