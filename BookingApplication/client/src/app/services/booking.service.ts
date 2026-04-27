import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';

export interface TripSearchRequest {
  source: string;
  destination: string;
  date: string;
  returnDate?: string | null;
}

export interface TripSummary {
  tripId: string;
  busId: string;
  busName: string;
  source: string;
  destination: string;
  departureTime: string;
  arrivalTime: string;
  capacity: number;
  seatsAvailable: number;
  basePrice: number;
  platformFee: number;
  isVariablePrice: boolean;
  pickupPoints: PickupDropPointResponse[];
  dropPoints: PickupDropPointResponse[];
}

export interface PickupDropPointResponse {
  pointId: string;
  location: string;
  address?: string | null;
  isDefault: boolean;
  isPickup: boolean;
}

export interface TripSearchResponse {
  outboundTrips: TripSummary[];
  returnTrips: TripSummary[];
}

export interface SeatInfo {
  seatNumber: number;
  isAvailable: boolean;
  bookedByGender?: string | null;
  bookedByName?: string | null;
}

export interface SeatLayoutResponse {
  tripId: string;
  travelDate: string;
  busName: string;
  layoutName: string;
  capacity: number;
  seatsAvailableLeft: number;
  seats: Record<number, SeatInfo>;
  ladiesSeatsAvailable: number[];
  pickupPoints: PickupDropPointResponse[];
  dropPoints: PickupDropPointResponse[];
}

export interface LockSeatsRequest {
  tripId: string;
  travelDate: string;
  userEmail: string;
  seatNumbers: number[];
}

export interface SeatLockResponse {
  lockId: string;
  tripId: string;
  seatNumbers: number[];
  lockExpiresAt: string;
  message: string;
}

export interface PassengerRequest {
  name: string;
  age: number;
  gender: string;
}

export interface CreateBookingRequest {
  lockId: string;
  travelDate: string;
  userEmail: string;
  paymentMode: number;
  passengers: PassengerRequest[];
}

export interface BookingResponse {
  bookingId: string;
  pnr: string;
  tripId: string;
  seatNumbers: number[];
  totalAmount: number;
  refundAmount: number;
  isCancelled: boolean;
  ticketDownloadUrl: string;
  mailStatus: string;
  paymentStatus: string;
}

export interface EnhancedBookingResponse {
  bookingId: string;
  pnr: string;
  tripId: string;
  travelDate: string;
  busName: string;
  source: string;
  destination: string;
  departureTime: string;
  arrivalTime: string;
  seatNumbers: number[];
  passengers: PassengerRequest[];
  totalAmount: number;
  refundAmount: number;
  isCancelled: boolean;
  paymentStatus: string;
  ticketUrl: string;
  bookedAt: string;
  status: string;
}

export interface UserProfileRequest {
  email: string;
  fullName: string;
  ssoProvider?: string | null;
}

export interface UserProfileResponse {
  userId: string;
  email: string;
  fullName: string;
  ssoProvider?: string | null;
}

export interface TicketResponse {
  bookingId: string;
  pnr: string;
  busName: string;
  source: string;
  destination: string;
  departureTime: string;
  arrivalTime: string;
  passengers: PassengerRequest[];
  totalAmount: number;
  paymentStatus: string;
  ticketUrl: string;
  pdfContent?: string | null;
}

@Injectable({
  providedIn: 'root'
})
export class BookingService {
  private readonly apiUrl = environment.apiBaseUrl;

  constructor(private http: HttpClient) {}

  searchTrips(source: string, destination: string, date: string, returnDate?: string | null): Observable<TripSearchResponse> {
    const params: Record<string, string> = { source, destination, date };
    if (returnDate) {
      params['returnDate'] = returnDate;
    }

    return this.http.get<TripSearchResponse>(`${this.apiUrl}/public/buses/search-fuzzy`, { params });
  }

  getSeatLayout(tripId: string, travelDate: string): Observable<SeatLayoutResponse> {
    return this.http.get<SeatLayoutResponse>(`${this.apiUrl}/public/trips/${tripId}/layout`, {
      params: { travelDate }
    });
  }

  lockSeats(request: LockSeatsRequest): Observable<SeatLockResponse> {
    return this.http.post<SeatLockResponse>(`${this.apiUrl}/bookings/lock-seats`, request);
  }

  createBooking(request: CreateBookingRequest): Observable<BookingResponse> {
    return this.http.post<BookingResponse>(`${this.apiUrl}/bookings`, request);
  }

  getBookingHistory(email: string, type: string = 'All'): Observable<EnhancedBookingResponse[]> {
    return this.http.get<EnhancedBookingResponse[]>(`${this.apiUrl}/bookings/${email}/enhanced-history`, {
      params: { type }
    });
  }

  cancelBooking(bookingId: string, userEmail: string): Observable<any> {
    return this.http.post(`${this.apiUrl}/bookings/${bookingId}/cancel`, null, {
      params: { userEmail }
    });
  }

  getTicket(bookingId: string, userEmail: string): Observable<TicketResponse> {
    return this.http.get<TicketResponse>(`${this.apiUrl}/bookings/${bookingId}/ticket`, {
      params: { userEmail }
    });
  }

  getProfile(email: string): Observable<UserProfileResponse> {
    return this.http.get<UserProfileResponse>(`${this.apiUrl}/profile/${email}`);
  }

  updateProfile(request: UserProfileRequest): Observable<UserProfileResponse> {
    return this.http.post<UserProfileResponse>(`${this.apiUrl}/profile`, request);
  }
}
