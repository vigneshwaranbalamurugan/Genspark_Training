# Operator & Admin Features Documentation

## 🔐 Operator Authentication

### Operator Login
**Endpoint:** `POST /api/operator/login`

**Request:**
```json
{
  "email": "operator@company.com",
  "password": "SecurePassword123"
}
```

**Response:**
```json
{
  "operatorId": "uuid",
  "companyName": "Company Name",
  "email": "operator@company.com",
  "jwtToken": "eyJ0eXAiOiJKV1QiLCJhbGciOiJIUzI1NiJ9...",
  "approvalStatus": 2,
  "isDisabled": false,
  "message": "Login successful"
}
```

**Status Codes:**
- `200`: Login successful
- `404`: Operator not found
- `401`: Account not approved, disabled, or invalid password

---

## 🚌 Bus Management

### Register Bus with Unique Bus Number
**Endpoint:** `POST /api/operator/{operatorId}/buses/register`

**Request:**
```json
{
  "busNumber": "TN12AB1234",
  "busName": "AC Sleeper Volvo",
  "capacity": 40,
  "layoutName": "2+2",
  "layoutJson": "{\"rows\":10,\"columns\":4}"
}
```

**Response:**
```json
{
  "busId": "uuid",
  "busNumber": "TN12AB1234",
  "busName": "AC Sleeper Volvo",
  "capacity": 40,
  "layoutName": "2+2",
  "isApproved": false,
  "isTemporarilyUnavailable": false,
  "isActive": true
}
```

**Features:**
- Bus number must be unique across system
- Admin approval required before operations
- Operator can choose layout or upload custom JSON

---

## 📊 Operator Revenue & Analytics

### Get Operator Revenue Dashboard
**Endpoint:** `GET /api/operator/{operatorId}/revenue`

**Response:**
```json
{
  "operatorId": "uuid",
  "totalRevenue": 125000.00,
  "revenuePastMonth": 45000.00,
  "revenueThisMonth": 65000.00,
  "totalBookings": 250,
  "activeBuses": 5,
  "activeTrips": 12,
  "busRevenue": [
    {
      "busId": "uuid",
      "busName": "AC Sleeper",
      "busNumber": "TN12AB1234",
      "revenue": 35000.00,
      "bookings": 85
    }
  ]
}
```

**Data Includes:**
- Total revenue across all trips
- Monthly breakdown (current + past month)
- Revenue per bus
- Booking count statistics
- Active buses and trips count

---

## 👥 Operator Bookings View

### Get All Bookings for Operator
**Endpoint:** `GET /api/operator/{operatorId}/bookings?busId={busId (optional)}`

**Response:**
```json
[
  {
    "bookingId": "uuid",
    "pnr": "BT202604241234",
    "tripId": "uuid",
    "busName": "AC Sleeper",
    "busNumber": "TN12AB1234",
    "route": "Chennai → Bangalore",
    "departureTime": "2026-04-24T21:00:00Z",
    "passengers": [
      {
        "name": "John Doe",
        "age": 30,
        "gender": "Male",
        "seatNumber": 12
      }
    ],
    "seatNumbers": [12, 13],
    "totalAmount": 2000.00,
    "paymentStatus": "PAID_DUMMY",
    "isCancelled": false
  }
]
```

**Features:**
- View all bookings across all buses (or filter by bus)
- See passenger details with seat allocation
- Track payment status
- Identify cancellations

---

## 🛣️ Preferred Routes

### Add Preferred Route
**Endpoint:** `POST /api/operator/{operatorId}/preferred-routes`

**Request:**
```json
{
  "routeId": "uuid"
}
```

**Response:**
```json
{
  "routeId": "uuid",
  "source": "Chennai",
  "destination": "Bangalore"
}
```

### Get Operator's Preferred Routes
**Endpoint:** `GET /api/operator/{operatorId}/preferred-routes`

**Response:**
```json
[
  {
    "routeId": "uuid",
    "source": "Chennai",
    "destination": "Bangalore"
  }
]
```

---

## 📍 Pickup & Drop Points

### Add Pickup Point
**Endpoint:** `POST /api/operator/{operatorId}/routes/{routeId}/pickup-points`

**Request:**
```json
{
  "location": "Koyambedu",
  "address": "CMRL Station, Koyambedu, Chennai",
  "isDefault": true
}
```

**Response:**
```json
{
  "pointId": "uuid",
  "location": "Koyambedu",
  "address": "CMRL Station, Koyambedu, Chennai",
  "isDefault": true
}
```

### Add Drop Point
**Endpoint:** `POST /api/operator/{operatorId}/routes/{routeId}/drop-points`

Same request/response structure as pickup points.

### Get Pickup Points
**Endpoint:** `GET /api/operator/{operatorId}/routes/{routeId}/pickup-points`

### Get Drop Points
**Endpoint:** `GET /api/operator/{operatorId}/routes/{routeId}/drop-points`

**Features:**
- Define multiple pickup/drop locations per route
- Mark default locations
- Store address information
- Operator-specific configuration

---

## ✈️ Advanced Trip Creation

### Create Trip with All Details
**Endpoint:** `POST /api/operator/{operatorId}/trips/create`

**Request:**
```json
{
  "busId": "uuid",
  "routeId": "uuid",
  "departureTime": "2026-04-24T21:00:00Z",
  "arrivalTime": "2026-04-25T06:00:00Z",
  "basePrice": 750.00,
  "pickupPoints": "Koyambedu,Guindy",
  "dropPoints": "Madiwala,Majestic"
}
```

**Response:**
```json
{
  "tripId": "uuid",
  "busId": "uuid",
  "busName": "AC Sleeper",
  "source": "Chennai",
  "destination": "Bangalore",
  "departureTime": "2026-04-24T21:00:00Z",
  "arrivalTime": "2026-04-25T06:00:00Z",
  "capacity": 40,
  "seatsAvailable": 40,
  "basePrice": 750.00,
  "platformFee": 25.00,
  "isVariablePrice": false
}
```

**Features:**
- Set fixed pricing (no dynamic pricing)
- Include pickup/drop points
- Platform fee auto-added from admin settings
- All seats available at full capacity

---

## 🚫 Bus Management

### Request Bus Disable
**Endpoint:** `POST /api/operator/{operatorId}/buses/{busId}/request-disable`

**Request:**
```json
{
  "reason": "Bus needs maintenance"
}
```

**Response:**
```json
{
  "message": "Request submitted to admin"
}
```

**Features:**
- Operator submits disable request
- Admin receives notification
- Used for maintenance or decommissioning
- Permanent or temporary removal options

---

## 👨‍💼 Admin Features

### Set Platform Fee
**Endpoint:** `POST /api/admin/platform-fee`

**Request:**
```json
{
  "feeAmount": 25.00,
  "description": "Platform transaction fee"
}
```

**Response:**
```json
{
  "feeId": "uuid",
  "amount": 25.00,
  "description": "Platform transaction fee",
  "updatedAt": "2026-04-23T10:30:00Z"
}
```

### Get Current Platform Fee
**Endpoint:** `GET /api/admin/platform-fee`

**Response:**
```json
{
  "feeId": "uuid",
  "amount": 25.00,
  "description": "Platform transaction fee",
  "updatedAt": "2026-04-23T10:30:00Z"
}
```

**Features:**
- Admin controls platform fee globally
- Auto-applied to all trips
- Historical tracking of fee changes
- One active fee at any time

---

## 🔒 Security Features

### Bus Number Privacy
- Bus number is unique but **NOT** displayed to users in:
  - Search results
  - Trip listings
  - Public seat availability views
  - Booking confirmations
  
- Bus number is visible only to:
  - Operator in their dashboard
  - Admin panel
  - Operator booking management

### Operator Approval Workflow
1. Operator registers account
2. Admin reviews and approves
3. Only approved operators can:
   - Register buses
   - Create trips
   - Login
   - Manage bookings

### Bus Approval Workflow
1. Operator registers bus
2. Admin approves bus
3. Only approved buses can:
   - Be assigned to trips
   - Accept bookings
   - Appear in searches

---

## 📋 Complete Operator Workflow

### 1. Registration & Approval
```
POST /api/operator/register
  → Admin reviews via /api/admin/operators
  → Admin approves: POST /api/admin/operators/{id}/approval
```

### 2. Bus Management
```
POST /api/operator/{id}/buses/register (unique bus_number)
  → Admin approves: POST /api/admin/buses/{busId}/approval
  → Operator views: GET /api/operator/{id}/buses
```

### 3. Route & Point Configuration
```
POST /api/operator/{id}/preferred-routes
POST /api/operator/{id}/routes/{routeId}/pickup-points
POST /api/operator/{id}/routes/{routeId}/drop-points
```

### 4. Trip Creation
```
POST /api/operator/{id}/trips/create
  (Auto-includes current platform fee)
```

### 5. Monitoring
```
GET /api/operator/{id}/bookings
GET /api/operator/{id}/revenue
```

---

## 📊 Admin Control Panel

### Operator Management
- `GET /api/admin/operators` - List all operators
- `POST /api/admin/operators/{id}/approval` - Approve/reject
- `POST /api/admin/operators/{id}/disable` - Disable operator
- `POST /api/admin/operators/{id}/enable` - Re-enable operator

### System Configuration
- `POST /api/admin/platform-fee` - Set platform fee (applied to all trips)
- `GET /api/admin/platform-fee` - View current fee
- `POST /api/admin/routes` - Add new routes
- `GET /api/admin/routes` - View all routes

### Bus Approval
- `POST /api/admin/buses/{busId}/approval` - Approve/reject bus

---

## 🎯 Key Business Rules

### Operator Rules
- ✅ Can only manage their own buses
- ✅ Can set prices per trip (fixed, no dynamic pricing)
- ✅ Must choose existing routes (cannot create routes)
- ✅ Can define pickup/drop points per route
- ✅ Cannot set platform fee
- ✅ Can view all bookings and revenue
- ❌ Cannot modify user registrations
- ❌ Cannot view other operators' data

### Admin Rules
- ✅ Can approve/reject operators
- ✅ Can approve/reject buses
- ✅ Can set platform fees (global)
- ✅ Can create routes and sources/destinations
- ✅ Can disable operators and buses
- ✅ View all system data
- ❌ Cannot directly create trips
- ❌ Cannot modify bookings (only view)

### Bus Number Rules
- ✅ Must be unique
- ✅ Visible only to operator and admin
- ✅ Not shown to users/passengers
- ✅ Required for registration

---

## 💰 Pricing Model

### No Dynamic Pricing
- All operators use fixed pricing per trip
- No per-seat price variation
- Platform fee applied uniformly

### Revenue Calculation
- Trip Revenue = (Base Price + Platform Fee) × Seats Booked
- Refunds = 20% of total amount (cancellation within 12hrs of departure)
- Platform keeps platform fee; operator gets ticket revenue

---

## 🔄 Complete API Workflow Examples

### Example 1: New Operator Journey
```bash
# 1. Register
POST /api/operator/register
{
  "email": "new@operator.com",
  "password": "SecurePass123",
  "companyName": "New Travels"
}

# 2. Wait for admin approval

# 3. Login (once approved)
POST /api/operator/login
{
  "email": "new@operator.com",
  "password": "SecurePass123"
}

# 4. Register bus
POST /api/operator/{operatorId}/buses/register
{
  "busNumber": "TN99XX0001",
  "busName": "Volvo AC",
  "capacity": 40,
  "layoutName": "2+2"
}

# 5. Wait for admin bus approval

# 6. Add preferred routes
POST /api/operator/{operatorId}/preferred-routes
{ "routeId": "route-uuid" }

# 7. Create trip
POST /api/operator/{operatorId}/trips/create
{
  "busId": "bus-uuid",
  "routeId": "route-uuid",
  "departureTime": "2026-04-25T21:00:00Z",
  "arrivalTime": "2026-04-26T06:00:00Z",
  "basePrice": 750.00
}

# 8. Monitor
GET /api/operator/{operatorId}/bookings
GET /api/operator/{operatorId}/revenue
```

### Example 2: Admin Setting Platform Fee
```bash
POST /api/admin/platform-fee
{
  "feeAmount": 35.00,
  "description": "Updated platform fee for Q2 2026"
}

# All new trips created after this will include ₹35 fee
```

