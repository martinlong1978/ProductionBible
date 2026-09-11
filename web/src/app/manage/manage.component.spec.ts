import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ManageComponent } from './manage.component';

describe('ManageComponent', () => {
  let fixture: ComponentFixture<ManageComponent>;
  let component: ManageComponent;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [ManageComponent],
    }).compileComponents();

    fixture = TestBed.createComponent(ManageComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('defaults to the projects tab', () => {
    expect(component.activeTab).toBe('projects');
  });

  it('switches tabs via setTab', () => {
    component.setTab('assets');
    expect(component.activeTab).toBe('assets');
  });

  it('renders a button per tab', () => {
    const buttons = (fixture.nativeElement as HTMLElement).querySelectorAll('button[data-tab]');
    const tabs = Array.from(buttons).map((b) => b.getAttribute('data-tab'));
    expect(tabs).toEqual(['projects', 'episodes', 'phases', 'beats', 'assets']);
  });
});
