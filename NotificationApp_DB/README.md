# Notification Application

## Overview

- Multi-layered C# console application for managing users and sending notifications
- Supports email and SMS notification types
- Input validation and exception handling throughout the application
# Notification Application

A layered C# console application for user management and notification delivery (SMS and Email), backed by PostgreSQL.

## Tech Stack

- .NET 10 console application
- PostgreSQL
- Npgsql
- Layered architecture (FE, BAL, DAL, Model)

## Solution Structure

- NotificationApp.FEApplication
  - Console menus and input flow
  - Validation classes for name, email, mobile number, and message
- NotificationApp.BALLibrary
  - Business services for users and notifications
  - Notification sender strategy implementations (SMS and Email)
- NotificationApp.DALLibrary
  - Repository implementations using SQL queries and Npgsql
  - Database connection wrapper
- NotificationApp.ModelLibrary
  - Domain models and custom validation exceptions

## Current Features

### User Features

- Create a user (name, email, mobile number)
- Get user details by mobile number
- Update user details by mobile number
- Delete user by mobile number

### Notification Features

- Send SMS notification to an existing user (by mobile number)
- Send Email notification to an existing user (by email)
- Get all notifications
- Get SMS notifications only
- Get Email notifications only

Note: If a recipient user is not found, notification creation is skipped with a message to create the user first.

## Menus

### Main Menu

1. User Menu
2. Notification Menu
3. Exit

### User Menu

1. Get User Details
2. Create User
3. Update User Details
4. Delete User
5. Back

### Notification Menu

1. Send Notification
2. Get All Notifications
3. Get SMS Notifications
4. Get Email Notifications
5. Back

## LINQ Usage

The notification filtering feature uses LINQ in FE layer:

- Where to filter by notification type (SMS or Email)
- OrderByDescending to show recent notifications first
- ToList to materialize filtered results

## Validation Rules

- Email
  - Cannot be empty
  - Must match a valid email format
- Mobile number
  - Cannot be empty
  - Must contain only digits
  - Must be exactly 10 digits
- Name
  - Cannot be empty
  - Must contain only letters and spaces
  - Minimum length is 3
- Message
  - Cannot be empty or whitespace
  - Length must be between 5 and 160 characters

## Data Access and Persistence

- Uses PostgreSQL tables:
  - users
  - notifications
- Notification read queries join notifications with users to include recipient contact details.
- Repository pattern is used for CRUD operations.

## Domain Models

- User
  - UserId, UserName, MobileNumber, EmailId
- Notification
  - Id, Message, UsertoNotify, SentDate, NotificationType
- EmailNotification (inherits Notification)
- SMSNotification (inherits Notification)
- NotiType enum
  - EmailNotification = 1
  - SMSNotification = 2

## Exception Types

- InvalidEmailIdException
- InvalidMobileNumberException (class name)
- InvalidNameException
- MessageException

## Run Instructions

From solution root:

1. Restore and build
   - dotnet build NotificationApp.sln
2. Run FE app
   - dotnet run --project NotificationApp.FEApplication
