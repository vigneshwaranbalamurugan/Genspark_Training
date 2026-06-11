import { Component } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { CommonModule } from '@angular/common';

declare var Razorpay: any;
@Component({
  selector: 'app-payment',
  imports: [FormsModule, CommonModule],
  templateUrl: './payment.html',
  styleUrl: './payment.css'
})
export class Payment {
  razorpayKey: string = '';
  orderId: string = '';
  paymentId: string | null = null;
  signature: string | null = null;

  constructor() {
    this.loadRazorpayScript();
  }

  loadRazorpayScript() {
    return new Promise((resolve) => {
      const script = document.createElement('script');
      script.src = 'https://checkout.razorpay.com/v1/checkout.js';
      script.onload = () => resolve(true);
      script.onerror = () => resolve(false);
      document.body.appendChild(script);
    });
  }

  proceedPayment() {
    if (!this.razorpayKey || !this.orderId) {
      alert('Please enter Razorpay Key and Order ID');
      return;
    }

    const options = {
      key: this.razorpayKey,
      name: "LMS App Premium Course Payment",
      description: "Test Transaction",
      order_id: this.orderId,
      handler: (response: any) => {
        this.paymentId = response.razorpay_payment_id;
        this.signature = response.razorpay_signature;
        
        // Popup the user with the details
        alert(`Payment Successful!\nPayment ID: ${this.paymentId}\nSignature: ${this.signature}`);
      },
      prefill: {
        name: "Test User",
        email: "test.user@example.com",
        contact: "9999999999"
      },
      theme: {
        color: "#3399cc"
      }
    };

    const rzp = new (window as any).Razorpay(options);
    
    rzp.on('payment.failed', function (response: any){
      alert(`Payment Failed!\nReason: ${response.error.description}\nPayment ID: ${response.error.metadata.payment_id}`);
    });

    rzp.open();
  }
}
