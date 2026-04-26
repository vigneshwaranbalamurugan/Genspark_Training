import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';

export interface AdminLoginRequest {
  email: string;
  password: string;
}

export interface AdminLoginResponse {
  email: string;
  jwtToken: string;
  role: string;
  message: string;
}

@Injectable({
  providedIn: 'root'
})
export class AdminService {
  private readonly apiUrl = environment.apiBaseUrl;

  constructor(private http: HttpClient) {}

  login(request: AdminLoginRequest): Observable<AdminLoginResponse> {
    return this.http.post<AdminLoginResponse>(`${this.apiUrl}/admin/login`, request);
  }

  getOperators(): Observable<any[]> {
    return this.http.get<any[]>(`${this.apiUrl}/admin/operators`);
  }

  approveOperator(operatorId: string, approve: boolean, comment?: string | null): Observable<any> {
    return this.http.post(`${this.apiUrl}/admin/operators/${operatorId}/approval`, { approve, comment: comment || '' });
  }

  disableOperator(operatorId: string, reason: string): Observable<any> {
    return this.http.post(`${this.apiUrl}/admin/operators/${operatorId}/disable`, { reason });
  }

  enableOperator(operatorId: string, reason: string): Observable<any> {
    return this.http.post(`${this.apiUrl}/admin/operators/${operatorId}/enable`, { reason });
  }

  addRoute(source: string, destination: string): Observable<any> {
    return this.http.post(`${this.apiUrl}/admin/routes`, { source, destination });
  }

  getRoutes(): Observable<any[]> {
    return this.http.get<any[]>(`${this.apiUrl}/admin/routes`);
  }

  getBuses(): Observable<any[]> {
    return this.http.get<any[]>(`${this.apiUrl}/admin/buses`);
  }

  approveBus(busId: string, approve: boolean, comment?: string | null): Observable<any> {
    return this.http.post(`${this.apiUrl}/admin/buses/${busId}/approval`, { approve, comment: comment || '' });
  }

  setPlatformFee(feeAmount: number, description?: string | null): Observable<any> {
    return this.http.post(`${this.apiUrl}/admin/platform-fee`, { feeAmount, description: description || '' });
  }

  getPlatformFee(): Observable<any> {
    return this.http.get(`${this.apiUrl}/admin/platform-fee`);
  }
}
