import { CommonModule } from '@angular/common';
import { Component, OnInit, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { AuthService } from '../services/auth.service';
import { BookingService, EnhancedBookingResponse } from '../services/booking.service';
import { environment } from '../../environments/environment';

@Component({
  selector: 'app-history',
  standalone: true,
  imports: [CommonModule, RouterLink],
  templateUrl: './history.component.html',
  styleUrl: './history.component.css'
})
export class HistoryComponent implements OnInit {
  readonly filters = ['All', 'Past', 'Present', 'Future', 'Cancelled'] as const;
  readonly loading = signal(false);
  readonly errorMessage = signal<string | null>(null);
  readonly bookings = signal<EnhancedBookingResponse[]>([]);
  readonly activeFilter = signal<'All' | 'Past' | 'Present' | 'Future' | 'Cancelled'>('All');

  constructor(
    private readonly bookingService: BookingService,
    private readonly authService: AuthService
  ) {}

  get currentUser() {
    return this.authService.getCurrentUser();
  }

  ngOnInit(): void {
    this.loadHistory();
  }

  loadHistory(filter: 'All' | 'Past' | 'Present' | 'Future' | 'Cancelled' = this.activeFilter()): void {
    const user = this.currentUser;
    if (!user) {
      this.errorMessage.set('Login required to view booking history.');
      return;
    }

    this.activeFilter.set(filter);
    this.loading.set(true);
    this.bookingService.getBookingHistory(user.email, filter).subscribe({
      next: (items) => {
        this.bookings.set(items);
        this.loading.set(false);
      },
      error: (error) => {
        this.errorMessage.set(error.message);
        this.loading.set(false);
      }
    });
  }

  cancel(bookingId: string): void {
    const user = this.currentUser;
    if (!user) {
      return;
    }

    this.bookingService.cancelBooking(bookingId, user.email).subscribe({
      next: () => this.loadHistory(),
      error: (error) => this.errorMessage.set(error.message)
    });
  }

  download(bookingId: string): void {
    const user = this.currentUser;
    if (!user) {
      return;
    }

    this.bookingService.getTicket(bookingId, user.email).subscribe({
      next: (ticket) => {
        const downloadUrl = `${environment.serverBaseUrl}${ticket.ticketUrl}?userEmail=${encodeURIComponent(user.email)}`;
        const link = document.createElement('a');
        link.href = downloadUrl;
        link.target = '_blank';
        link.download = ''; // The server will provide the filename via Content-Disposition
        document.body.appendChild(link);
        link.click();
        document.body.removeChild(link);
      },
      error: (error) => this.errorMessage.set(error.message)
    });
  }
}
