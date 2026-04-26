import { Inject, Injectable, PLATFORM_ID } from '@angular/core';
import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { isPlatformBrowser } from '@angular/common';
import { Observable, BehaviorSubject, throwError } from 'rxjs';
import { catchError, tap } from 'rxjs/operators';
import { environment } from '../../environments/environment';

export interface AuthResponse {
  email: string;
  accessToken?: string;
  jwtToken?: string;
  tokenType?: string;
  expiresIn?: number;
  role?: string;
  profileCompleted?: boolean;
  fullName?: string;
  operatorId?: string;
  companyName?: string;
  firstName?: string;
  lastName?: string;
  phoneNumber?: string;
}

export interface LoginRequest {
  email: string;
  password: string;
}

export interface SetPasswordRequest {
  email: string;
  password: string;
}

export interface User {
  email: string;
  role: string;
  profileCompleted: boolean;
  operatorId?: string;
  companyName?: string;
  firstName?: string;
  lastName?: string;
}

@Injectable({
  providedIn: 'root'
})
export class AuthService {
  private readonly API_URL = environment.apiBaseUrl;
  private readonly isBrowser: boolean;
  private currentUserSubject: BehaviorSubject<User | null>;
  public currentUser$: Observable<User | null>;

  constructor(
    private http: HttpClient,
    @Inject(PLATFORM_ID) platformId: object
  ) {
    this.isBrowser = isPlatformBrowser(platformId);
    this.currentUserSubject = new BehaviorSubject<User | null>(this.getUserFromStorage());
    this.currentUser$ = this.currentUserSubject.asObservable();
  }

  login(email: string, password: string): Observable<AuthResponse> {
    return this.http.post<AuthResponse>(`${this.API_URL}/registration/login`, {
      email,
      password
    }).pipe(
      tap(response => this.handleAuthResponse(response, 'User')),
      catchError(this.handleError)
    );
  }

  startRegistration(email: string): Observable<any> {
    return this.http.post(`${this.API_URL}/registration/start`, { email })
      .pipe(catchError(this.handleError));
  }

  verifyOtp(email: string, otpCode: string): Observable<any> {
    return this.http.post(`${this.API_URL}/registration/verify-otp`, {
      email,
      otpCode
    }).pipe(catchError(this.handleError));
  }

  setPassword(email: string, password: string): Observable<AuthResponse> {
    return this.http.post<AuthResponse>(`${this.API_URL}/registration/set-password`, {
      email,
      password
    }).pipe(
      tap(response => this.handleAuthResponse(response, 'User')),
      catchError(this.handleError)
    );
  }

  setSession(user: User, token: string): void {
    if (!this.isBrowser) {
      return;
    }

    localStorage.setItem('auth_token', token);
    localStorage.setItem('user', JSON.stringify(user));
    this.currentUserSubject.next(user);
  }

  completeProfile(data: any): Observable<any> {
    return this.http.post(`${this.API_URL}/registration/personal-details`, data)
      .pipe(catchError(this.handleError));
  }

  getStatus(email: string): Observable<any> {
    return this.http.get(`${this.API_URL}/registration/status/${email}`)
      .pipe(catchError(this.handleError));
  }

  logout(): void {
    if (this.isBrowser) {
      localStorage.removeItem('auth_token');
      localStorage.removeItem('user');
    }
    this.currentUserSubject.next(null);
  }

  getToken(): string | null {
    if (!this.isBrowser) {
      return null;
    }

    return localStorage.getItem('auth_token');
  }

  isAuthenticated(): boolean {
    return !!this.getToken();
  }

  getCurrentUser(): User | null {
    return this.currentUserSubject.value;
  }

  hasRole(role: string): boolean {
    const user = this.currentUserSubject.value;
    return user?.role === role;
  }

  private handleAuthResponse(response: AuthResponse, defaultRole: string): void {
    if (!this.isBrowser) {
      return;
    }

    const token = response.accessToken || response.jwtToken;
    if (!token) {
      return;
    }

    const firstName = response.firstName || response.fullName?.split(' ')[0] || undefined;
    const lastName = response.lastName || response.fullName?.split(' ').slice(1).join(' ') || undefined;

    const user: User = {
      email: response.email,
      role: response.role || defaultRole,
      profileCompleted: response.profileCompleted ?? true,
      operatorId: response.operatorId,
      companyName: response.companyName,
      firstName,
      lastName
    };
    this.setSession(user, token);
  }

  private getUserFromStorage(): User | null {
    if (!this.isBrowser) {
      return null;
    }

    const user = localStorage.getItem('user');
    return user ? JSON.parse(user) : null;
  }

  private handleError(error: HttpErrorResponse): Observable<never> {
    let errorMessage = 'An error occurred';
    if (error.error instanceof ErrorEvent) {
      errorMessage = error.error.message;
    } else {
      errorMessage = error.error?.message || error.statusText;
    }
    return throwError(() => new Error(errorMessage));
  }
}
