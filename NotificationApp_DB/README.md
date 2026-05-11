# Notification Application

## Overview

- Multi-layered C# console application for managing users and sending notifications
- Supports email and SMS notification types
- Input validation and exception handling throughout the application

## Architecture

### Layers

- **FE Application Layer (NotificationApp.FEApplication)**
  - Console-based user interface
  - Menu-driven interaction
  - Input validation and user prompts
  
- **BAL - Business Logic Layer (NotificationApp.BALLibrary)**
  - Service classes for business operations
  - User management service
  - Notification creation and sending service
  - Notification sender implementations
  
- **DAL - Data Access Layer (NotificationApp.DALLibrary)**
  - Repository pattern implementation
  - Generic repository interface
  - User and notification repositories
  - In-memory data storage
  
- **Model Library (NotificationApp.ModelLibrary)**
  - Domain models
  - Custom exceptions
  - Data structures

## Project Structure

```
NotificationApp/
├── NotificationApp.FEApplication/
│   ├── Program.cs (Main entry point)
│   ├── Notification/
│   │   └── NotificationApp.cs (Notification menu handler)
│   ├── User/
│   │   └── UserApp.cs (User menu handler)
│   └── Validators/
│       ├── EmailValidator.cs
│       ├── MessageValidator.cs
│       ├── MobileNumberValidator.cs
│       └── NameValidator.cs
│
├── NotificationApp.BALLibrary/
│   ├── Interfaces/
│   │   ├── INotificationSender.cs
│   │   └── IUserInteract.cs
│   └── Services/
│       ├── Notification/
│       │   ├── EmailNotificationService.cs
│       │   ├── NotificationService.cs
│       │   └── SMSNotificationService.cs
│       └── Users/
│           └── UserService.cs
│
├── NotificationApp.DALLibrary/
│   ├── Interfaces/
│   │   └── IRepository.cs (Generic repository interface)
│   └── Repositories/
│       ├── AbstractRepository.cs (Base repository class)
│       ├── NotificationRepository.cs
│       └── UserRepository.cs
│
└── NotificationApp.ModelLibrary/
    ├── Models/
    │   ├── User.cs
    │   ├── Notification.cs
    │   ├── EmailNotification.cs
    │   └── SMSNotification.cs
    └── Exceptions/
        ├── InvalidEmailIdException.cs
        ├── InvalidMobileNumberException.cs
        ├── InvalidNameException.cs
        └── MessageException.cs
```

## Key Features

### User Management
- Create new users with name, email, and mobile number
- Retrieve users by email or mobile number
- Update user information
- Delete users
- View user details

### Notification Management
- Create notifications with message content
- Send SMS notifications to users by mobile number
- Send email notifications to users by email address
- View all notifications
- Retrieve notification by ID
- Track notification sending date and time

### Data Validation
- Email format validation using regex
- Mobile number validation
- User name validation
- Message content validation
- Custom exception handling for validation failures

### Notification Types
- Email notifications via EmailNotificationService
- SMS notifications via SMSNotificationService
- Polymorphic implementation using INotificationSender interface

## Core Classes and Interfaces

### Models
- `User` - Represents a user with ID, name, mobile number, email
- `Notification` - Base notification class with type, message, sent date
- `EmailNotification` - Extends Notification for email messages
- `SMSNotification` - Extends Notification for SMS messages

### Services
- `NotificationService` - Handles notification creation and sending logic
- `UserService` - Implements IUserInteract for user operations
- `EmailNotificationSender` - Sends email notifications
- `SMSNotificationSender` - Sends SMS notifications

### Repositories
- `UserRepository` - Manages user data persistence
- `NotificationRepository` - Manages notification data persistence
- `AbstractRepository<K,T>` - Generic base repository with CRUD operations

### Interfaces
- `IUserInteract` - Contract for user operations
- `INotificationSender` - Contract for notification delivery
- `INotificationService` - Contract for notification services
- `IRepository<K,T>` - Generic repository contract

## Application Flow

### User Menu
- Create new user
- View user by email
- View user by mobile number
- Update user information
- Delete user
- Return to main menu

### Notification Menu
- Send SMS notification
- Send email notification
- View all notifications
- View notification by ID
- Return to main menu

## Exception Handling

- InvalidEmailIdException - Invalid email format
- InvalidMobileNumberException - Invalid phone number
- InvalidNameException - Invalid user name
- MessageException - Invalid message content
- Custom exception messages for user guidance

## Running the Application

- Requires .NET 10.0 runtime
- Build: dotnet build
- Run: dotnet run --project NotificationApp.FEApplication
- Console-based interactive menu system

## Input Flow

- User selects menu option
- Input validation through dedicated validators
- Exception handling with user-friendly error messages
- Retry on validation failure
- Operation confirmation and feedback

## Design Patterns Used

- Repository pattern for data access
- Strategy pattern for notification sending
- Factory pattern concept in service initialization
- Generic programming for repository implementation
- Interface segregation for loose coupling
- Dependency injection through constructor parameters