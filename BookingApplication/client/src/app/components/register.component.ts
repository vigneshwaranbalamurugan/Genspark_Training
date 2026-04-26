import { Component, signal } from '@angular/core';
import { FormBuilder, FormGroup, Validators, ReactiveFormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { CommonModule } from '@angular/common';
import { AuthService } from '../services/auth.service';

type RegistrationStep = 'email' | 'otp' | 'password' | 'profile' | 'success';

@Component({
  selector: 'app-register',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  templateUrl: './register.component.html',
  styleUrl: './register.component.css'
})
export class RegisterComponent {
  currentStep = signal<RegistrationStep>('email');
  loading = signal(false);
  errorMessage = signal<string | null>(null);
  successMessage = signal<string | null>(null);
  
  emailForm: FormGroup;
  otpForm: FormGroup;
  passwordForm: FormGroup;
  profileForm: FormGroup;
  
  userEmail = signal<string>('');
  otpExpiry = signal<Date | null>(null);
  developmentOtp = signal<string | null>(null);

  constructor(
    private fb: FormBuilder,
    private authService: AuthService,
    private router: Router
  ) {
    this.emailForm = this.fb.group({
      email: ['', [Validators.required, Validators.email]]
    });

    this.otpForm = this.fb.group({
      otp: ['', [Validators.required, Validators.minLength(6), Validators.maxLength(6)]]
    });

    this.passwordForm = this.fb.group({
      password: ['', [Validators.required, Validators.minLength(8)]],
      confirmPassword: ['', [Validators.required]]
    });

    this.profileForm = this.fb.group({
      firstName: ['', Validators.required],
      lastName: ['', Validators.required],
      phoneNumber: ['', [Validators.required, Validators.pattern(/^\d{10}$/)]],
      gender: ['', Validators.required],
      age: ['', [Validators.required, Validators.min(18), Validators.max(120)]],
      dateOfBirth: ['']
    });
  }

  // Step 1: Email Submission
  submitEmail(): void {
    this.errorMessage.set(null);
    if (this.emailForm.invalid) return;

    this.loading.set(true);
    const email = this.emailForm.value.email;
    this.userEmail.set(email);

    this.authService.startRegistration(email).subscribe({
      next: (response) => {
        this.otpExpiry.set(new Date(response.otpExpiresAt));
        this.developmentOtp.set(response.developmentOtp);
        this.successMessage.set('OTP sent to your email!');
        this.currentStep.set('otp');
        this.loading.set(false);
      },
      error: (error) => {
        this.errorMessage.set(error.message);
        this.loading.set(false);
      }
    });
  }

  // Step 2: OTP Verification
  submitOtp(): void {
    this.errorMessage.set(null);
    if (this.otpForm.invalid) return;

    this.loading.set(true);
    const otp = this.otpForm.value.otp;

    this.authService.verifyOtp(this.userEmail(), otp).subscribe({
      next: (response) => {
        this.successMessage.set('OTP verified successfully!');
        this.currentStep.set('password');
        this.loading.set(false);
      },
      error: (error) => {
        this.errorMessage.set(error.message);
        this.loading.set(false);
      }
    });
  }

  // Step 3: Password Setup
  submitPassword(): void {
    this.errorMessage.set(null);
    if (this.passwordForm.invalid || this.passwordForm.value.password !== this.passwordForm.value.confirmPassword) {
      this.errorMessage.set('Passwords do not match');
      return;
    }

    this.loading.set(true);
    const password = this.passwordForm.value.password;

    this.authService.setPassword(this.userEmail(), password).subscribe({
      next: (response) => {
        this.successMessage.set('Password set successfully!');
        this.currentStep.set('profile');
        this.loading.set(false);
      },
      error: (error) => {
        this.errorMessage.set(error.message);
        this.loading.set(false);
      }
    });
  }

  // Step 4: Profile Completion
  submitProfile(): void {
    this.errorMessage.set(null);
    if (this.profileForm.invalid) return;

    this.loading.set(true);
    const profileData = {
      email: this.userEmail(),
      ...this.profileForm.value
    };

    this.authService.completeProfile(profileData).subscribe({
      next: (response) => {
        this.successMessage.set('Profile completed successfully!');
        this.currentStep.set('success');
        this.loading.set(false);
      },
      error: (error) => {
        this.errorMessage.set(error.message);
        this.loading.set(false);
      }
    });
  }

  navigateToLogin(): void {
    this.router.navigate(['/login']);
  }

  get emailControl() {
    return this.emailForm.controls;
  }

  get otpControl() {
    return this.otpForm.controls;
  }

  get passwordControl() {
    return this.passwordForm.controls;
  }

  get profileControl() {
    return this.profileForm.controls;
  }

  getProgressWidth(): string {
    switch (this.currentStep()) {
      case 'email':
        return '25%';
      case 'otp':
        return '50%';
      case 'password':
        return '75%';
      case 'profile':
      case 'success':
        return '100%';
      default:
        return '0%';
    }
  }
}
