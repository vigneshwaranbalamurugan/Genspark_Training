
# Bus Booking System - Backend API Documentation

## New Features Implemented

### 1. Authentication & Login
**Endpoint:** `POST /api/registration/login`
```json
{
  "email": "user@example.com",
  "password": "password123"
}
```
**Response:**
```json
{
  "userId": "uuid",
  "email": "user@example.com",
  "fullName": "User Name",
  "jwtToken": "eyJhbGciOiJIUzI1NiJ9...",
  "message": "Login successful"
}
```

---

### 2. Fuzzy Search for Buses
**Endpoint:** `GET /api/public/buses/search-fuzzy?source=Chenn&destination=Bng&date=2026-04-24&returnDate=2026-04-25`

Supports partial/fuzzy matching for city names (e.g., "Chenn" matches "Chennai", "Bng" matches "Bangalore")

**Response:** Same as `/buses/search` - returns list of trips with available seats

---

### 3. Seat Layout with Gender Information
**Endpoint:** `GET /api/public/trips/{tripId}/layout`

**Response:**
```json
{
  "tripId": "uuid",
  "busName": "AC Sleeper",
  "layoutName": "2x2",
  "capacity": 40,
  "seatsAvailableLeft": 12,
  "seats": {
    "1": {
      "seatNumber": 1,
      "isAvailable": true,
      "bookedByGender": null,
      "bookedByName": null
    },
    "2": {
      "seatNumber": 2,
      "isAvailable": false,
      "bookedByGender": "Female",
      "bookedByName": "Priya"
    },
    "3": {
      "seatNumber": 3,
      "isAvailable": false,
      "bookedByGender": "locked"
    }
  },
  "ladiesSeatsAvailable": [2, 4, 6, 8]
}
```

**Features:**
- Shows all seats with availability status
- Displays gender of booked passengers
- Identifies locked seats (reserved in cart, 5-min TTL)
- Lists available ladies seats for easy identification

---

### 4. Enhanced Booking History with Filters
**Endpoint:** `GET /api/bookings/{userEmail}/enhanced-history?type=Past|Present|Future|Cancelled|All`

**Query Parameters:**
- `type`: Filter booking status
  - `All` (0): Show all bookings
  - `Past` (1): Completed trips
  - `Present` (2): Trips within next 7 days
  - `Future` (3): Trips beyond 7 days
  - `Cancelled` (4): Cancelled bookings

**Response:**
```json
[
  {
    "bookingId": "uuid",
    "pnr": "BT202604241234",
    "tripId": "uuid",
    "busName": "AC Sleeper",
    "source": "Chennai",
    "destination": "Bangalore",
    "departureTime": "2026-04-24T21:00:00Z",
    "arrivalTime": "2026-04-25T05:30:00Z",
    "seatNumbers": [12, 13],
    "passengers": [
      {
        "name": "John Doe",
        "age": 30,
        "gender": "Male",
        "seatNumber": 12
      }
    ],
    "totalAmount": 2000,
    "refundAmount": 0,
    "isCancelled": false,
    "paymentStatus": "PAID_DUMMY",
    "ticketUrl": "/api/bookings/{bookingId}/ticket",
    "bookedAt": "2026-04-23T10:00:00Z",
    "status": "Upcoming"
  }
]
```

---

### 5. Get Single Booking Details
**Endpoint:** `GET /api/bookings/{bookingId}/enhanced?userEmail=user@example.com`

Returns full booking details including all passengers and trip information.

---

### 6. Ticket Download/Retrieval
**Endpoint:** `GET /api/bookings/{bookingId}/ticket?userEmail=user@example.com`

**Response:**
```json
{
  "bookingId": "uuid",
  "pnr": "BT202604241234",
  "busName": "AC Sleeper",
  "source": "Chennai",
  "destination": "Bangalore",
  "departureTime": "2026-04-24T21:00:00Z",
  "arrivalTime": "2026-04-25T05:30:00Z",
  "passengers": [
    {
      "name": "John Doe",
      "age": 30,
      "gender": "Male",
      "seatNumber": 12
    }
  ],
  "totalAmount": 2000,
  "paymentStatus": "PAID_DUMMY",
  "ticketUrl": "/api/bookings/{bookingId}/ticket"
}
```

---

### 7. Payment Initiation (Razorpay & Dummy)
**Endpoint:** `POST /api/bookings/payment/initiate`

**Request:**
```json
{
  "bookingId": "uuid",
  "paymentMode": 1,
  "amount": 2000
}
```

**PaymentMode Values:**
- `1`: Dummy (test payment, auto-approved)
- `2`: Razorpay (production integration)

**Response:**
```json
{
  "paymentId": "uuid",
  "bookingId": "uuid",
  "amount": 2000,
  "paymentMode": 1,
  "razorpayOrderId": null,
  "message": "Payment initiated. Proceed to payment gateway."
}
```

---

### 8. Payment Verification
**Endpoint:** `POST /api/bookings/payment/verify`

**Request:**
```json
{
  "paymentId": "uuid",
  "razorpayPaymentId": "pay_29QQoUBi66xm2f",
  "razorpaySignature": "9ef4dffbfd84f1318f6739a3ce19f9d85851857ae648f114332d8401e0949a3d"
}
```

**Response:**
```json
{
  "paymentId": "uuid",
  "bookingId": "uuid",
  "verified": true,
  "message": "Payment verified successfully."
}
```

---

## Feature Summary

### ✅ Completed Features
1. **User Login** - Email-based authentication with JWT token generation
2. **Fuzzy Search** - Partial city name matching using LIKE and pattern matching
3. **Seat Layout Visualization** - Real-time seat availability with gender indicators
4. **Ladies-Only Seat Highlighting** - Identifies and displays ladies-booked seats
5. **Enhanced Booking History** - Filter by Past/Present/Future/Cancelled
6. **Ticket Management** - Retrieve detailed ticket information
7. **Payment Gateway** - Dummy and Razorpay integration stubs
8. **Concurrency Control** - 5-minute seat locking mechanism
9. **Gender-Aware Booking** - Captures and displays passenger gender

### 📋 Key Models Added

**LoginResponse** - User authentication response with JWT
**SeatWithGenderInfo** - Seat details with gender and booking information
**SeatLayoutResponse** - Complete bus layout with real-time availability
**EnhancedBookingResponse** - Comprehensive booking details with trip and passenger info
**BookingsHistoryFilter** - Filter bookings by status (Past/Present/Future/Cancelled)
**TicketResponse** - Complete ticket information for download
**PaymentInitiateResponse** - Payment gateway initialization response
**PaymentVerifyResponse** - Payment verification confirmation

---

## Example Workflow

### 1. User Registration & Login
```
POST /api/registration/start → Start registration
POST /api/registration/verify-otp → Verify OTP
POST /api/registration/set-password → Set password
POST /api/registration/personal-details → Complete profile
POST /api/registration/login → Login and get JWT token
```

### 2. Search & Browse
```
GET /api/public/buses/search-fuzzy → Find buses (fuzzy search)
GET /api/public/trips/{tripId}/layout → View seat layout with ladies seats
```

### 3. Book & Pay
```
POST /api/bookings/lock-seats → Lock seats for 5 minutes
POST /api/bookings/payment/initiate → Initiate payment
POST /api/bookings/payment/verify → Verify payment
POST /api/bookings → Create booking
```

### 4. View & Download
```
GET /api/bookings/{userEmail}/enhanced-history → View all bookings
GET /api/bookings/{bookingId}/enhanced → Get booking details
GET /api/bookings/{bookingId}/ticket → Download ticket
```

### 5. Manage Bookings
```
POST /api/bookings/{bookingId}/cancel → Cancel booking (12-hour window)
```

---

## Special Features

### Concurrency & Seat Blocking
- Seats are locked for 5 minutes when user clicks "Continue"
- Locked seats are displayed to other users as unavailable
- Automatic cleanup of expired locks on each operation

### Gender-Specific Booking
- Passenger gender captured during booking
- Seat layout shows which seats are booked by females
- Ladies seats are clearly identified

### Booking Cancellation Rules
- Allowed only 12+ hours before departure
- 20% refund on cancellation
- Automatic notification to user

### Payment Modes
- **Dummy**: Simulates payment, auto-approved for testing
- **Razorpay**: Production-ready integration stub (requires API credentials)

---

## Testing with Postman/API Client

Import these endpoints into your API client:

```
# Login
POST http://localhost:5226/api/registration/login
Content-Type: application/json

{
  "email": "user@example.com",
  "password": "testpass123"
}

# Fuzzy Search
GET http://localhost:5226/api/public/buses/search-fuzzy?source=che&destination=ban&date=2026-04-24

# Seat Layout
GET http://localhost:5226/api/public/trips/{tripId}/layout

# Booking History
GET http://localhost:5226/api/bookings/user@example.com/enhanced-history?type=0

# Payment Initiate
POST http://localhost:5226/api/bookings/payment/initiate
Content-Type: application/json

{
  "bookingId": "uuid",
  "paymentMode": 1,
  "amount": 2000
}
```

---

## Database Changes

New columns/tables supporting these features:
- `booking_passengers.gender` - Stores passenger gender
- `payments` table - Tracks payment transactions (optional, for production)
- Seat lock expiry tracking for concurrency control

All features use existing database schema - no schema migrations required!
