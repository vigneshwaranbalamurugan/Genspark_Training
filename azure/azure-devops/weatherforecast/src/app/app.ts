import { Component, signal } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { WeatherDashboardComponent} from './weather-dashboard/weather-dashboard';

@Component({
  selector: 'app-root',
  imports: [RouterOutlet,WeatherDashboardComponent],
  templateUrl: './app.html',
  styleUrl: './app.css'
})
export class App {
  protected readonly title = signal('weatherforecast');
}
