import { Component, OnInit, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { AuthService } from '../services/auth.service';
import { AdminService } from '../services/admin.service';

@Component({
  selector: 'app-admin-dashboard',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './admin-dashboard.component.html',
  styleUrl: './admin-dashboard.component.css'
})
export class AdminDashboardComponent implements OnInit {
  activeTab = signal<'dashboard' | 'operators' | 'buses' | 'routes' | 'settings'>('dashboard');
  
  operators = signal<any[]>([]);
  routes = signal<any[]>([]);
  buses = signal<any[]>([]);
  platformFee = signal<any | null>(null);
  revenue = signal<any | null>(null);
  
  errorMessage = signal<string | null>(null);
  successMessage = signal<string | null>(null);

  // Search Filters
  operatorSearch = signal<string>('');
  busSearch = signal<string>('');
  routeSearch = signal<string>('');

  filteredOperators = computed(() => {
    const term = this.operatorSearch().toLowerCase();
    if (!term) return this.operators();
    return this.operators().filter(op => 
      op.companyName.toLowerCase().includes(term) || 
      op.email.toLowerCase().includes(term)
    );
  });

  filteredBuses = computed(() => {
    const term = this.busSearch().toLowerCase();
    if (!term) return this.buses();
    return this.buses().filter(b => 
      b.busName.toLowerCase().includes(term)
    );
  });

  filteredRoutes = computed(() => {
    const term = this.routeSearch().toLowerCase();
    if (!term) return this.routes();
    return this.routes().filter(r => 
      r.source.toLowerCase().includes(term) || 
      r.destination.toLowerCase().includes(term)
    );
  });

  routeForm = { source: '', destination: '' };
  feeForm = { feeAmount: 0, description: '' };

  constructor(
    private authService: AuthService,
    private adminService: AdminService,
    private router: Router
  ) {}

  ngOnInit(): void {
    const user = this.authService.getCurrentUser();
    if (!user || user.role !== 'Admin') {
      this.router.navigate(['/admin/login']);
      return;
    }
    this.loadAll();
  }

  setTab(tab: 'dashboard' | 'operators' | 'buses' | 'routes' | 'settings') {
    this.activeTab.set(tab);
    this.clearMessages();
  }

  clearMessages() {
    this.errorMessage.set(null);
    this.successMessage.set(null);
  }

  loadAll(): void {
    this.adminService.getOperators().subscribe({
      next: (data) => this.operators.set(data || []),
      error: (err) => this.errorMessage.set(err.message)
    });

    this.adminService.getRoutes().subscribe({
      next: (data) => this.routes.set(data || []),
      error: (err) => this.errorMessage.set(err.message)
    });

    this.adminService.getBuses().subscribe({
      next: (data) => this.buses.set(data || []),
      error: (err) => this.errorMessage.set(err.message)
    });

    this.adminService.getPlatformFee().subscribe({
      next: (data) => {
        this.platformFee.set(data);
        this.feeForm.feeAmount = Number(data?.amount || 0);
        this.feeForm.description = data?.description || '';
      },
      error: (err) => this.errorMessage.set(err.message)
    });

    this.adminService.getRevenue().subscribe({
      next: (data) => this.revenue.set(data),
      error: (err) => this.errorMessage.set(err.message)
    });
  }

  // Operator Actions
  updateOperatorStatus(operatorId: string, approve: boolean) {
    this.adminService.approveOperator(operatorId, approve, '').subscribe({
      next: () => {
        this.successMessage.set(`Operator ${approve ? 'approved' : 'rejected'} successfully.`);
        this.loadAll();
      },
      error: (err) => this.errorMessage.set(err.message)
    });
  }

  toggleOperatorDisabled(operatorId: string, isDisabled: boolean) {
    const reason = prompt(`Reason for ${isDisabled ? 'enabling' : 'disabling'} this operator:`);
    if (reason === null) return;
    
    const request = isDisabled ? 
      this.adminService.enableOperator(operatorId, reason || 'Admin action') :
      this.adminService.disableOperator(operatorId, reason || 'Admin action');

    request.subscribe({
      next: () => {
        this.successMessage.set(`Operator ${isDisabled ? 'enabled' : 'disabled'} successfully.`);
        this.loadAll();
      },
      error: (err) => this.errorMessage.set(err.message)
    });
  }

  // Bus Actions
  updateBusStatus(busId: string, approve: boolean) {
    this.adminService.approveBus(busId, approve, '').subscribe({
      next: () => {
        this.successMessage.set(`Bus ${approve ? 'approved' : 'rejected'} successfully.`);
        this.loadAll();
      },
      error: (err) => this.errorMessage.set(err.message)
    });
  }

  disableBus(busId: string) {
    const reason = prompt('Reason for disabling this bus:');
    if (reason === null) return;
    
    this.adminService.disableBus(busId, reason || 'Admin action').subscribe({
      next: () => {
        this.successMessage.set('Bus disabled successfully.');
        this.loadAll();
      },
      error: (err) => this.errorMessage.set(err.message)
    });
  }

  // Route Actions
  addRoute(): void {
    if (!this.routeForm.source || !this.routeForm.destination) {
      this.errorMessage.set('Source and destination are required.');
      return;
    }
    this.adminService.addRoute(this.routeForm.source, this.routeForm.destination).subscribe({
      next: () => {
        this.successMessage.set('Route added successfully.');
        this.routeForm = { source: '', destination: '' };
        this.loadAll();
      },
      error: (err) => this.errorMessage.set(err.message)
    });
  }

  // Settings Actions
  setPlatformFee(): void {
    if (this.feeForm.feeAmount < 0) {
      this.errorMessage.set('Fee amount cannot be negative.');
      return;
    }
    this.adminService.setPlatformFee(this.feeForm.feeAmount, this.feeForm.description).subscribe({
      next: () => {
        this.successMessage.set('Platform fee updated successfully.');
        this.loadAll();
      },
      error: (err) => this.errorMessage.set(err.message)
    });
  }

  logout(): void {
    this.authService.logout();
    this.router.navigate(['/']);
  }

  getStatusClass(status: any): string {
    if (status === undefined || status === null) return 'pending';
    const s = status.toString().toLowerCase();
    if (s === '1' || s === 'pending') return 'pending';
    if (s === '2' || s === 'approved') return 'approved';
    if (s === '3' || s === 'rejected') return 'rejected';
    return s;
  }

  getStatusText(status: any): string {
    if (status === undefined || status === null) return 'Pending';
    const s = status.toString().toLowerCase();
    if (s === '1' || s === 'pending') return 'Pending';
    if (s === '2' || s === 'approved') return 'Approved';
    if (s === '3' || s === 'rejected') return 'Rejected';
    return status;
  }

  isPending(status: any): boolean {
    if (status === undefined || status === null) return true;
    const s = status.toString().toLowerCase();
    return s === '1' || s === 'pending';
  }
}
