import { Component } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { CustomerModel } from './customermodel';

@Component({
  selector: 'app-customer',
  imports: [FormsModule],
  templateUrl: './customer.html',
  styleUrl: './customer.css',
})
export class Customer {

  //customer:CustomerModel = new CustomerModel("johndoe", "John Doe", "john.doe@example.com", "123-456-7890", "active", new Date());
  customer:CustomerModel = new CustomerModel();
  styclass: string = "tableclass";

  handleChangeClick(){
    this.customer.name = "John the don. hh Doe";
    alert("Customer Name: " + this.customer.name);
  }

}