import { Routes } from '@angular/router';
import { BibleComponent } from './bible/bible.component';
import { ProductionPlanComponent } from './production-plan/production-plan.component';

export const routes: Routes = [
  { path: '', redirectTo: 'bible', pathMatch: 'full' },
  { path: 'bible', component: BibleComponent },
  { path: 'production-plan', component: ProductionPlanComponent },
];
