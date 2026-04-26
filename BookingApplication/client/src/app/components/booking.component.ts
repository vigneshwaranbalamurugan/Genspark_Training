import { CommonModule } from '@angular/common';
import { Component, OnInit, signal } from '@angular/core';
import { FormArray, FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { AuthService } from '../services/auth.service';
import {
  BookingResponse,
  BookingService,
  SeatInfo,
  SeatLayoutResponse,
  SeatLockResponse
} from '../services/booking.service';
import { environment } from '../../environments/environment';

@Component({
  selector: 'app-booking',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterLink],
  templateUrl: './booking.component.html',
  styleUrl: './booking.component.css'
})
export class BookingComponent implements OnInit {
  readonly tripId = signal<string>('');
  readonly seatLayout = signal<SeatLayoutResponse | null>(null);
  readonly loading = signal(false);
  readonly actionLoading = signal(false);
  readonly errorMessage = signal<string | null>(null);
  readonly successMessage = signal<string | null>(null);
  readonly lockResponse = signal<SeatLockResponse | null>(null);
  readonly bookingResponse = signal<BookingResponse | null>(null);
  readonly selectedSeats = signal<number[]>([]);
  readonly paymentMode = signal<'Dummy' | 'Razorpay'>('Dummy');
  readonly seatForm;
  readonly paymentForm;

  constructor(
    private readonly route: ActivatedRoute,
    private readonly router: Router,
    private readonly bookingService: BookingService,
    private readonly authService: AuthService,
    private readonly formBuilder: FormBuilder
  ) {
    this.seatForm = this.formBuilder.group({
      seats: this.formBuilder.array([])
    });

    this.paymentForm = this.formBuilder.group({
      paymentMode: ['Dummy', Validators.required]
    });
  }

  get currentUser() {
    return this.authService.getCurrentUser();
  }

  ngOnInit(): void {
    const tripId = this.route.snapshot.paramMap.get('tripId');
    if (!tripId) {
      this.errorMessage.set('Trip not found.');
      return;
    }

    this.tripId.set(tripId);
    this.fetchLayout();
  }

  fetchLayout(): void {
    this.loading.set(true);
    this.bookingService.getSeatLayout(this.tripId()).subscribe({
      next: (layout) => {
        this.seatLayout.set(layout);
        this.loading.set(false);
      },
      error: (error) => {
        this.errorMessage.set(error.message);
        this.loading.set(false);
      }
    });
  }

  toggleSeat(seatNumber: number): void {
    const layout = this.seatLayout();
    if (!layout) {
      return;
    }

    const seat = layout.seats[seatNumber];
    if (!seat || !seat.isAvailable) {
      return;
    }

    const current = [...this.selectedSeats()];
    const index = current.indexOf(seatNumber);
    if (index >= 0) {
      current.splice(index, 1);
    } else {
      current.push(seatNumber);
    }

    this.selectedSeats.set(current.sort((a, b) => a - b));
    this.syncPassengerForms();
  }

  syncPassengerForms(): void {
    const targetCount = this.selectedSeats().length;
    const passengers = this.seatPassengerArray;

    while (passengers.length < targetCount) {
      passengers.push(this.formBuilder.group({
        name: ['', [Validators.required, Validators.minLength(2)]],
        age: [18, [Validators.required, Validators.min(1)]],
        gender: ['Female', Validators.required]
      }));
    }

    while (passengers.length > targetCount) {
      passengers.removeAt(passengers.length - 1);
    }
  }

  get seatPassengerArray(): FormArray {
    return this.seatForm.get('seats') as FormArray;
  }

  isSeatSelected(seatNumber: number): boolean {
    return this.selectedSeats().includes(seatNumber);
  }

  seatState(seat: SeatInfo): string {
    if (!seat.isAvailable) {
      if ((seat.bookedByGender || '').toLowerCase() === 'female') {
        return 'booked-female';
      }
      return 'booked';
    }
    if (this.isSeatSelected(seat.seatNumber)) {
      return 'selected';
    }
    return 'available';
  }

  seatLabel(seat: SeatInfo): string {
    if (!seat.isAvailable && (seat.bookedByGender || '').toLowerCase() === 'female') {
      return 'L';
    }
    return String(seat.seatNumber);
  }

  lockSeats(): void {
    const user = this.currentUser;
    if (!user) {
      this.router.navigate(['/login'], { queryParams: { redirect: `/book/${this.tripId()}` } });
      return;
    }

    if (this.selectedSeats().length === 0) {
      this.errorMessage.set('Select one or more seats first.');
      return;
    }

    this.actionLoading.set(true);
    this.bookingService.lockSeats({
      tripId: this.tripId(),
      userEmail: user.email,
      seatNumbers: this.selectedSeats()
    }).subscribe({
      next: (response) => {
        this.lockResponse.set(response);
        this.successMessage.set(response.message);
        this.actionLoading.set(false);
      },
      error: (error) => {
        this.errorMessage.set(error.message);
        this.actionLoading.set(false);
      }
    });
  }

  confirmBooking(): void {
    const user = this.currentUser;
    const lock = this.lockResponse();
    if (!user || !lock) {
      this.errorMessage.set('Lock seats before confirming booking.');
      return;
    }

    if (this.seatPassengerArray.invalid || this.seatPassengerArray.length !== this.selectedSeats().length) {
      this.errorMessage.set('Traveler details must match the number of selected seats.');
      return;
    }

    const paymentMode = this.paymentForm.value.paymentMode === 'Razorpay' ? 2 : 1;
    const passengers = this.seatPassengerArray.controls.map((control: any) => control.value);

    this.actionLoading.set(true);
    this.bookingService.createBooking({
      lockId: lock.lockId,
      userEmail: user.email,
      paymentMode,
      passengers
    }).subscribe({
      next: (response) => {
        this.bookingResponse.set(response);
        this.successMessage.set('Booking confirmed. Ticket is ready for download and email confirmation will be sent.');
        this.actionLoading.set(false);
      },
      error: (error) => {
        this.errorMessage.set(error.message);
        this.actionLoading.set(false);
      }
    });
  }

  downloadTicket(): void {
    const booking = this.bookingResponse();
    const user = this.currentUser;
    if (!booking || !user) {
      return;
    }

    this.bookingService.getTicket(booking.bookingId, user.email).subscribe({
      next: (ticket) => {
        window.open(`${environment.serverBaseUrl}${ticket.ticketUrl}`, '_blank');
      },
      error: (error) => {
        this.errorMessage.set(error.message);
      }
    });
  }

  get seatGrid(): SeatInfo[] {
    const layout = this.seatLayout();
    if (!layout) {
      return [];
    }
    return Object.values(layout.seats).sort((a, b) => a.seatNumber - b.seatNumber);
  }

  goToHistory(): void {
    this.router.navigate(['/history']);
  }
}
