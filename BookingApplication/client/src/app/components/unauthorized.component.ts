import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';

@Component({
  selector: 'app-unauthorized',
  standalone: true,
  imports: [CommonModule, RouterLink],
  template: `
    <div class="min-h-screen bg-gradient-to-br from-red-50 to-orange-50 flex items-center justify-center px-4">
      <div class="text-center max-w-md">
        <div class="mb-6">
          <div class="text-6xl font-bold text-red-600">403</div>
        </div>
        <h1 class="text-4xl font-bold text-gray-900 mb-2">Access Denied</h1>
        <p class="text-gray-600 mb-8">You don't have permission to access this resource.</p>
        <a
          routerLink="/dashboard"
          class="inline-block px-6 py-3 bg-blue-600 text-white font-semibold rounded-lg hover:bg-blue-700 transition"
        >
          Go Back to Dashboard
        </a>
      </div>
    </div>
  `
})
export class UnauthorizedComponent {}
