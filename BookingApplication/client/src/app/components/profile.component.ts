import { CommonModule } from '@angular/common';
import { Component, OnInit, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { AuthService } from '../services/auth.service';
import { BookingService } from '../services/booking.service';

@Component({
  selector: 'app-profile',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterLink],
  templateUrl: './profile.component.html',
  styleUrl: './profile.component.css'
})
export class ProfileComponent implements OnInit {
  readonly loading = signal(false);
  readonly errorMessage = signal<string | null>(null);
  readonly successMessage = signal<string | null>(null);
  readonly profile = signal<any>(null);
  readonly profileForm;

  constructor(
    private readonly authService: AuthService,
    private readonly bookingService: BookingService,
    private readonly formBuilder: FormBuilder
  ) {
    this.profileForm = this.formBuilder.group({
      email: ['', [Validators.required, Validators.email]],
      fullName: ['', [Validators.required, Validators.minLength(3)]],
      ssoProvider: ['']
    });
  }

  get currentUser() {
    return this.authService.getCurrentUser();
  }

  ngOnInit(): void {
    const user = this.currentUser;
    if (!user) {
      this.errorMessage.set('Login required to view profile.');
      return;
    }

    this.profileForm.patchValue({
      email: user.email,
      fullName: `${user.firstName || ''} ${user.lastName || ''}`.trim() || user.email
    });

    this.loadProfile();
  }

  loadProfile(): void {
    const user = this.currentUser;
    if (!user) {
      return;
    }

    this.bookingService.getProfile(user.email).subscribe({
      next: (profile) => {
        this.profile.set(profile);
        this.profileForm.patchValue(profile);
      },
      error: () => {
        // profile may not exist yet; keep editable form
      }
    });
  }

  save(): void {
    if (this.profileForm.invalid) {
      this.profileForm.markAllAsTouched();
      return;
    }

    this.loading.set(true);
    this.bookingService.updateProfile(this.profileForm.value as any).subscribe({
      next: (profile) => {
        this.profile.set(profile);
        this.successMessage.set('Profile saved successfully.');
        this.loading.set(false);
      },
      error: (error) => {
        this.errorMessage.set(error.message);
        this.loading.set(false);
      }
    });
  }
}
