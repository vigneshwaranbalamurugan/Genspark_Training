import { Component, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { OperatorService } from '../services/operator.service';

@Component({
  selector: 'app-operator-register',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterLink],
  templateUrl: './operator-register.component.html',
  styleUrl: './operator-register.component.css'
})
export class OperatorRegisterComponent {
  form: FormGroup;
  loading = signal(false);
  errorMessage = signal<string | null>(null);
  successMessage = signal<string | null>(null);
  submitted = signal(false);

  constructor(
    private fb: FormBuilder,
    private operatorService: OperatorService,
    private router: Router
  ) {
    this.form = this.fb.group({
      companyName: ['', [Validators.required, Validators.minLength(2)]],
      email: ['', [Validators.required, Validators.email]],
      password: ['', [Validators.required, Validators.minLength(8)]],
      confirmPassword: ['', Validators.required]
    }, {
      validators: this.passwordMatchValidator
    });
  }

  get f() { return this.form.controls; }

  passwordMatchValidator(g: FormGroup) {
    return g.get('password')?.value === g.get('confirmPassword')?.value
      ? null : { mismatch: true };
  }

  onSubmit() {
    this.submitted.set(true);
    if (this.form.invalid) return;

    this.loading.set(true);
    this.errorMessage.set(null);

    const { companyName, email, password } = this.form.value;
    this.operatorService.register({ companyName, email, password }).subscribe({
      next: () => {
        this.successMessage.set('Registration successful! Please wait for admin approval before logging in.');
        this.loading.set(false);
        setTimeout(() => this.router.navigate(['/operator/login']), 3000);
      },
      error: (err) => {
        this.errorMessage.set(err.message);
        this.loading.set(false);
      }
    });
  }
}
