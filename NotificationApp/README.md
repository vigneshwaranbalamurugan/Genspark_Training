# Notification App

## Project Highlights

- Console-based notification application built with C#.
- Clean separation of models, interfaces, repositories, and services.
- Simple user management flow with create, read, update, and delete actions.
- Notification delivery support for SMS and Email.

## Data Flow

- Users are created and stored in memory with automatically assigned IDs.
- Users can be retrieved by mobile number or email address.
- Notifications are sent to an existing user after selection of the channel.
- Notification records capture the message, sent date, and notification type.

## Main Components

- `Program.cs` handles the interactive menu and application flow.
- `Models` contains the user and notification types.
- `Interfaces` defines the contracts for user interaction and repositories.
- `Repositories` stores and manages user data.
- `Services` coordinates user actions and notification delivery.

## Available Actions

- Create a new user.
- Send an SMS notification.
- Send an Email notification.
- View user details.
- Update user details.
- Delete a user.
- Exit the application.