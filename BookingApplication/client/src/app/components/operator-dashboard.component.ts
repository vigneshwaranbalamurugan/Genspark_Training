import { Component, OnInit, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { AuthService } from '../services/auth.service';
import { OperatorService } from '../services/operator.service';

@Component({
  selector: 'app-operator-dashboard',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './operator-dashboard.component.html',
  styleUrl: './operator-dashboard.component.css'
})
export class OperatorDashboardComponent implements OnInit {
  operatorId = '';
  activeTab = signal<'dashboard' | 'buses' | 'routes' | 'trips' | 'bookings'>('dashboard');
  
  buses = signal<any[]>([]);
  bookings = signal<any[]>([]);
  preferredRoutes = signal<any[]>([]);
  allGlobalRoutes = signal<any[]>([]);
  revenue = signal<any | null>(null);
  dashboard = signal<any | null>(null);
  
  errorMessage = signal<string | null>(null);
  successMessage = signal<string | null>(null);

  // Search Filters
  busSearch = signal<string>('');
  bookingSearch = signal<string>('');
  routeSearch = signal<string>('');

  filteredBuses = computed(() => {
    const term = this.busSearch().toLowerCase();
    if (!term) return this.buses();
    return this.buses().filter(b => b.busName.toLowerCase().includes(term) || b.busNumber.toLowerCase().includes(term));
  });

  filteredBookings = computed(() => {
    const term = this.bookingSearch().toLowerCase();
    if (!term) return this.bookings();
    return this.bookings().filter(b => b.pnr.toLowerCase().includes(term) || b.source.toLowerCase().includes(term) || b.destination.toLowerCase().includes(term));
  });

  filteredRoutes = computed(() => {
    const term = this.routeSearch().toLowerCase();
    if (!term) return this.preferredRoutes();
    return this.preferredRoutes().filter(r => r.source.toLowerCase().includes(term) || r.destination.toLowerCase().includes(term));
  });

  busForm = {
    busNumber: '',
    busName: '',
    capacity: 40,
    layoutName: 'Default Layout'
  };

  routeForm = {
    routeId: '',
    pickupPoint: '',
    pickupAddress: '',
    dropPoint: '',
    dropAddress: ''
  };

  tripForm = {
    busId: '',
    routeId: '',
    departureTime: '',
    arrivalTime: '',
    basePrice: 0,
    source: '',
    destination: '',
    tripType: 1, // 1 for OneTime, 2 for Daily
    daysOfWeek: [] as string[]
  };

  daysList = ['Mon', 'Tue', 'Wed', 'Thu', 'Fri', 'Sat', 'Sun'];

  toggleDay(day: string) {
    const index = this.tripForm.daysOfWeek.indexOf(day);
    if (index > -1) {
      this.tripForm.daysOfWeek.splice(index, 1);
    } else {
      this.tripForm.daysOfWeek.push(day);
    }
  }

  disableRequest = {
    busId: '',
    reason: ''
  };

  constructor(
    private authService: AuthService,
    private operatorService: OperatorService,
    private router: Router
  ) {}

  ngOnInit(): void {
    const user = this.authService.getCurrentUser();
    if (!user || user.role !== 'Operator' || !user.operatorId) {
      this.router.navigate(['/operator/login']);
      return;
    }

    this.operatorId = user.operatorId;
    this.loadAll();
  }

  setTab(tab: 'dashboard' | 'buses' | 'routes' | 'trips' | 'bookings') {
    this.activeTab.set(tab);
    this.clearMessages();
  }

  clearMessages() {
    this.errorMessage.set(null);
    this.successMessage.set(null);
  }

  loadAll(): void {
    this.operatorService.getDashboard(this.operatorId).subscribe({
      next: (data) => this.dashboard.set(data),
      error: (err) => this.errorMessage.set(err.message)
    });

    this.operatorService.getBuses(this.operatorId).subscribe({
      next: (data) => {
        this.buses.set(data || []);
        if (data?.length && !this.tripForm.busId) {
          this.tripForm.busId = data[0].busId;
          this.disableRequest.busId = data[0].busId;
        }
      },
      error: (err) => this.errorMessage.set(err.message)
    });

    this.operatorService.getBookings(this.operatorId).subscribe({
      next: (data) => this.bookings.set(data || []),
      error: (err) => this.errorMessage.set(err.message)
    });

    this.operatorService.getPreferredRoutes(this.operatorId).subscribe({
      next: (data) => {
        this.preferredRoutes.set(data || []);
        if (data?.length && !this.tripForm.routeId) {
          this.tripForm.routeId = data[0].routeId;
          this.onTripRouteSelected();
        }
      },
      error: (err) => this.errorMessage.set(err.message)
    });

    this.operatorService.getRevenue(this.operatorId).subscribe({
      next: (data) => this.revenue.set(data),
      error: (err) => this.errorMessage.set(err.message)
    });

    this.operatorService.getRoutes().subscribe({
      next: (data) => this.allGlobalRoutes.set(data || []),
      error: (err) => console.error("Could not load global routes", err)
    });
  }

  // Bus Actions
  registerBus(): void {
    this.clearMessages();
    this.operatorService.registerBus(this.operatorId, this.busForm).subscribe({
      next: () => {
        this.successMessage.set('Bus registered and pending admin approval.');
        this.busForm = { busNumber: '', busName: '', capacity: 40, layoutName: 'Default Layout' };
        this.loadAll();
      },
      error: (err) => this.errorMessage.set(err.message)
    });
  }

  toggleTemporary(busId: string, unavailable: boolean): void {
    this.clearMessages();
    this.operatorService.setTemporaryUnavailable(this.operatorId, busId, unavailable).subscribe({
      next: () => this.loadAll(),
      error: (err) => this.errorMessage.set(err.message)
    });
  }

  removeBus(busId: string): void {
    if (!confirm('Are you sure you want to remove this bus?')) return;
    this.clearMessages();
    this.operatorService.removeBus(this.operatorId, busId).subscribe({
      next: () => {
        this.successMessage.set('Bus removed successfully.');
        this.loadAll();
      },
      error: (err) => this.errorMessage.set(err.message)
    });
  }

  requestDisableBus(): void {
    this.clearMessages();
    if (!this.disableRequest.busId || !this.disableRequest.reason) {
      this.errorMessage.set('Please select a bus and provide a reason.');
      return;
    }
    this.operatorService.requestDisableBus(this.operatorId, this.disableRequest.busId, this.disableRequest.reason).subscribe({
      next: () => {
        this.successMessage.set('Disable request sent to admin.');
        this.disableRequest.reason = '';
      },
      error: (err) => this.errorMessage.set(err.message)
    });
  }

  // Route Actions
  addPreferredRoute(): void {
    this.clearMessages();
    if (!this.routeForm.routeId) return;
    this.operatorService.addPreferredRoute(this.operatorId, this.routeForm.routeId).subscribe({
      next: () => {
        this.successMessage.set('Preferred route added successfully.');
        this.loadAll();
      },
      error: (err) => this.errorMessage.set(err.message)
    });
  }

  addPickupDropPoints(): void {
    this.clearMessages();
    if (!this.routeForm.routeId || !this.routeForm.pickupPoint || !this.routeForm.dropPoint) {
      this.errorMessage.set('Please provide route, pickup point, and drop point.');
      return;
    }
    this.operatorService.addPickupPoint(this.operatorId, this.routeForm.routeId, this.routeForm.pickupPoint, this.routeForm.pickupAddress).subscribe({
      next: () => {
        this.operatorService.addDropPoint(this.operatorId, this.routeForm.routeId, this.routeForm.dropPoint, this.routeForm.dropAddress).subscribe({
          next: () => {
            this.successMessage.set('Pickup and drop points added.');
            this.routeForm.pickupPoint = '';
            this.routeForm.pickupAddress = '';
            this.routeForm.dropPoint = '';
            this.routeForm.dropAddress = '';
          },
          error: (err) => this.errorMessage.set(err.message)
        });
      },
      error: (err) => this.errorMessage.set(err.message)
    });
  }

  // Trip Actions
  onTripRouteSelected() {
    const route = this.preferredRoutes().find(r => r.routeId === this.tripForm.routeId);
    if (route) {
      this.tripForm.source = route.source;
      this.tripForm.destination = route.destination;
    }
  }

  createTrip(): void {
    this.clearMessages();
    if (!this.tripForm.busId || !this.tripForm.routeId || !this.tripForm.departureTime || !this.tripForm.arrivalTime) {
      this.errorMessage.set('Please fill out all required trip fields.');
      return;
    }

    const payload = {
      busId: this.tripForm.busId,
      routeId: this.tripForm.routeId,
      departureTime: this.tripForm.departureTime,
      arrivalTime: this.tripForm.arrivalTime,
      basePrice: Number(this.tripForm.basePrice),
      source: this.tripForm.source,
      destination: this.tripForm.destination,
      isVariablePrice: false,
      useFixedSeatPricing: true,
      tripType: Number(this.tripForm.tripType),
      daysOfWeek: this.tripForm.daysOfWeek.join(',')
    };

    this.operatorService.createTrip(this.operatorId, payload).subscribe({
      next: () => {
        this.successMessage.set('Trip created successfully.');
        this.tripForm.departureTime = '';
        this.tripForm.arrivalTime = '';
        this.loadAll();
      },
      error: (err) => this.errorMessage.set(err.message)
    });
  }

  logout(): void {
    this.authService.logout();
    this.router.navigate(['/']);
  }
}
