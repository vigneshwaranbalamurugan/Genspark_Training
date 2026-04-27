import { Component, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { OperatorService } from '../services/operator.service';
import { AuthService } from '../services/auth.service';

@Component({
  selector: 'app-operator-login',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterLink],
  templateUrl: './operator-login.component.html',
  styleUrl: './operator-login.component.css'
})
export class OperatorLoginComponent {
  loading = signal(false);
  submitted = signal(false);
  errorMessage = signal<string | null>(null);

  form: FormGroup;

  constructor(
    private fb: FormBuilder,
    private operatorService: OperatorService,
    private authService: AuthService,
    private router: Router
  ) {
    this.form = this.fb.group({
      email: ['', [Validators.required, Validators.email]],
      password: ['', [Validators.required, Validators.minLength(8)]]
    });
  }

  get f() {
    return this.form.controls;
  }

  onSubmit(): void {
    this.submitted.set(true);
    this.errorMessage.set(null);

    if (this.form.invalid) {
      return;
    }

    this.loading.set(true);
    const request = this.form.getRawValue() as { email: string; password: string };

    this.operatorService.login(request).subscribe({
      next: (response) => {
        this.authService.setSession({
          email: response.email,
          role: 'Operator',
          profileCompleted: true,
          operatorId: response.operatorId,
          companyName: response.companyName
        }, response.jwtToken);
        this.router.navigate(['/operator/dashboard']);
      },
      error: (error) => {
        this.errorMessage.set(error.message || 'Operator login failed');
        this.loading.set(false);
      }
    });
  }
}
