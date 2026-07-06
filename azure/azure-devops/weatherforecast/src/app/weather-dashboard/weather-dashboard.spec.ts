import { ComponentFixture, TestBed } from '@angular/core/testing';

import { WeatherDashboardComponent } from './weather-dashboard';

describe('WeatherDashboardComponent', () => {
  let component: WeatherDashboardComponent;
  let fixture: ComponentFixture<WeatherDashboardComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [WeatherDashboardComponent],
    }).compileComponents();

    fixture = TestBed.createComponent(WeatherDashboardComponent);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
