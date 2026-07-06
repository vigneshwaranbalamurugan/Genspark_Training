import { Component, OnInit ,signal} from '@angular/core';
import {CommonModule} from '@angular/common';
import { WeatherForecast } from '../models/weather.model';
import { WeatherService } from '../services/weather.service';

@Component({
  selector: 'app-weather-dashboard',
  imports: [CommonModule],
  templateUrl: './weather-dashboard.html',
  styleUrls: ['./weather-dashboard.css']
})
export class WeatherDashboardComponent implements OnInit {

  forecasts: WeatherForecast[] = [];
  loading = signal(false);

  constructor(private weatherService: WeatherService) {}

  ngOnInit(): void {
    this.loadWeather();
  }

  loadWeather(): void {
    this.loading.set(true);

    this.weatherService.getWeatherForecast().subscribe({
      next: (data) => {
        this.forecasts = data;
        this.loading.set(false);
      },
      error: (err) => {
        console.error(err);
        this.loading.set(false);
      }
    });
  }

  get totalRecords(): number {
    return this.forecasts.length;
  }

  get latestTemperature(): number {
    return this.forecasts.length
      ? this.forecasts[this.forecasts.length - 1].temperatureC
      : 0;
  }

  get todayWeather(): WeatherForecast | undefined {
    return this.forecasts[0];
  }

  getWeatherIcon(summary: string): string {

    const weatherMap: { [key: string]: string } = {
      Freezing: 'ac_unit',
      Bracing: 'air',
      Chilly: 'cloud',
      Cool: 'cloud_queue',
      Mild: 'wb_cloudy',
      Warm: 'wb_sunny',
      Hot: 'sunny',
      Sweltering: 'whatshot'
    };

    return weatherMap[summary] || 'cloud';
  }
}