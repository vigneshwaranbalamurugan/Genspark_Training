import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';

export interface OperatorLoginRequest {
  email: string;
  password: string;
}

export interface OperatorLoginResponse {
  operatorId: string;
  companyName: string;
  email: string;
  jwtToken: string;
  approvalStatus: string;
  isDisabled: boolean;
  message: string;
}

export interface OperatorRegisterRequest {
  companyName: string;
  email: string;
  password: string;
}

export interface BusRegistrationRequest {
  busNumber: string;
  busName: string;
  capacity: number;
  layoutName: string;
  layoutJson?: string | null;
}

@Injectable({
  providedIn: 'root'
})
export class OperatorService {
  private readonly apiUrl = environment.apiBaseUrl;

  constructor(private http: HttpClient) {}

  register(request: OperatorRegisterRequest): Observable<any> {
    return this.http.post(`${this.apiUrl}/operator/register`, request);
  }

  login(request: OperatorLoginRequest): Observable<OperatorLoginResponse> {
    return this.http.post<OperatorLoginResponse>(`${this.apiUrl}/operator/login`, request);
  }

  getDashboard(operatorId: string): Observable<any> {
    return this.http.get(`${this.apiUrl}/operator/${operatorId}/dashboard`);
  }

  getBuses(operatorId: string): Observable<any[]> {
    return this.http.get<any[]>(`${this.apiUrl}/operator/${operatorId}/buses`);
  }

  getRoutes(): Observable<any[]> {
    return this.http.get<any[]>(`${this.apiUrl}/operator/routes`);
  }

  registerBus(operatorId: string, request: BusRegistrationRequest): Observable<any> {
    return this.http.post(`${this.apiUrl}/operator/${operatorId}/buses/register`, request);
  }

  setTemporaryUnavailable(operatorId: string, busId: string, unavailable: boolean): Observable<any> {
    return this.http.post(`${this.apiUrl}/operator/${operatorId}/buses/${busId}/temporary-unavailable`, null, {
      params: { unavailable }
    });
  }

  removeBus(operatorId: string, busId: string): Observable<any> {
    return this.http.delete(`${this.apiUrl}/operator/${operatorId}/buses/${busId}`);
  }

  getBookings(operatorId: string, busId?: string): Observable<any[]> {
    if (busId) {
      return this.http.get<any[]>(`${this.apiUrl}/operator/${operatorId}/bookings`, { params: { busId } });
    }

    return this.http.get<any[]>(`${this.apiUrl}/operator/${operatorId}/bookings`);
  }

  getRevenue(operatorId: string): Observable<any> {
    return this.http.get(`${this.apiUrl}/operator/${operatorId}/revenue`);
  }

  addPreferredRoute(operatorId: string, routeId: string): Observable<any> {
    return this.http.post(`${this.apiUrl}/operator/${operatorId}/preferred-routes`, { routeId });
  }

  getPreferredRoutes(operatorId: string): Observable<any[]> {
    return this.http.get<any[]>(`${this.apiUrl}/operator/${operatorId}/preferred-routes`);
  }

  addPickupPoint(operatorId: string, routeId: string, pointName: string, address?: string | null): Observable<any> {
    return this.http.post(`${this.apiUrl}/operator/${operatorId}/routes/${routeId}/pickup-points`, {
      location: pointName,
      address: address || 'Default pickup address',
      isDefault: true
    });
  }

  addDropPoint(operatorId: string, routeId: string, pointName: string, address?: string | null): Observable<any> {
    return this.http.post(`${this.apiUrl}/operator/${operatorId}/routes/${routeId}/drop-points`, {
      location: pointName,
      address: address || 'Default drop address',
      isDefault: true
    });
  }

  requestDisableBus(operatorId: string, busId: string, reason: string): Observable<any> {
    return this.http.post(`${this.apiUrl}/operator/${operatorId}/buses/${busId}/request-disable`, { reason });
  }

  createTrip(operatorId: string, payload: any): Observable<any> {
    return this.http.post(`${this.apiUrl}/operator/${operatorId}/trips/create`, payload);
  }

  getTrips(operatorId: string): Observable<any[]> {
    return this.http.get<any[]>(`${this.apiUrl}/operator/${operatorId}/trips`);
  }

  deleteTrip(operatorId: string, tripId: string): Observable<any> {
    return this.http.delete(`${this.apiUrl}/operator/${operatorId}/trips/${tripId}`);
  }
}
