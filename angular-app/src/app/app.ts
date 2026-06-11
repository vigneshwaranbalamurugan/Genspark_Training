import { Component, signal } from '@angular/core';
import { Customer } from './customer/customer';
import { Product } from './product/product';
import { Profile } from './profile/profile';
import { Payment } from './payment/payment';

@Component({
  selector: 'app-root',
  imports: [Customer, Product, Profile, Payment],
  templateUrl: './app.html',
  styleUrl: './app.css'
})
export class App {
  protected readonly title = signal('angular-app');
}
