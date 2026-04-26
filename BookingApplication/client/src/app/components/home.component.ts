import { CommonModule } from '@angular/common';
import { Component, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { AuthService } from '../services/auth.service';
import { BookingService, TripSearchResponse, TripSummary } from '../services/booking.service';

@Component({
  selector: 'app-home',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterLink],
  templateUrl: './home.component.html',
  styleUrl: './home.component.css'
})
export class HomeComponent {
  readonly loading = signal(false);
  readonly errorMessage = signal<string | null>(null);
  readonly searchResult = signal<TripSearchResponse | null>(null);
  readonly expandedTripId = signal<string | null>(null);
  readonly searchForm;

  constructor(
    private readonly formBuilder: FormBuilder,
    private readonly bookingService: BookingService,
    private readonly authService: AuthService,
    private readonly router: Router
  ) {
    this.searchForm = this.formBuilder.group({
      source: ['', [Validators.required, Validators.minLength(2)]],
      destination: ['', [Validators.required, Validators.minLength(2)]],
      date: ['', [Validators.required]],
      returnDate: ['']
    });
  }

  get currentUser() {
    return this.authService.getCurrentUser();
  }

  search(): void {
    this.errorMessage.set(null);
    if (this.searchForm.invalid) {
      this.searchForm.markAllAsTouched();
      return;
    }

    const { source, destination, date, returnDate } = this.searchForm.value;
    this.loading.set(true);
    this.bookingService.searchTrips(source!, destination!, date!, returnDate || null).subscribe({
      next: (response) => {
        this.searchResult.set(response);
        this.loading.set(false);
      },
      error: (error) => {
        this.errorMessage.set(error.message);
        this.loading.set(false);
      }
    });
  }

  clearResults(): void {
    this.searchResult.set(null);
    this.expandedTripId.set(null);
  }

  toggleTrip(tripId: string): void {
    this.expandedTripId.set(this.expandedTripId() === tripId ? null : tripId);
  }

  bookTrip(trip: TripSummary): void {
    if (!this.authService.isAuthenticated()) {
      this.router.navigate(['/login'], { queryParams: { redirect: `/book/${trip.tripId}` } });
      return;
    }

    this.router.navigate(['/book', trip.tripId]);
  }

  get totalTrips(): number {
    return (this.searchResult()?.outboundTrips?.length || 0) + (this.searchResult()?.returnTrips?.length || 0);
  }
}
