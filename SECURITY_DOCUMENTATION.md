# Authentication and Save System Security

This document explains the security layer added to the save file system.

## Overview

The game now features a comprehensive user authentication system where each save file is tied to a specific user account. This provides:

- **User Authentication**: Secure login with username and password
- **Password Security**: Passwords are hashed using SHA256 before storage
- **User-Specific Saves**: Each user has their own independent save data
- **Data Isolation**: Users cannot access or modify other users' save files

## Architecture

### Components

1. **AuthHandler** (`Handlers/AuthHandler.cs`)
   - Manages user registration, authentication, and password changes
   - Stores user credentials in a separate `users.db` database
   - Uses SHA256 hashing for password security

2. **SQLHandler** (`Handlers/SQLHandler.cs`)
   - Updated to support user-specific save files
   - Links save data to user IDs
   - Requires a logged-in user to save/load game data

3. **User Model** (`Models/User.cs`)
   - Represents a user account
   - Contains UserId, Username, PasswordHash, and CreatedAt fields

4. **SaveData Model** (`Models/SaveData.cs`)
   - Updated to include UserId field
   - Links save data to specific users

## Database Schema

### Users Database (`users.db`)

```sql
CREATE TABLE Users (
    UserId INTEGER PRIMARY KEY AUTOINCREMENT,
    Username TEXT NOT NULL UNIQUE,
    PasswordHash TEXT NOT NULL,
    CreatedAt TEXT NOT NULL
);
```

### Save Data Database (`savefile.db`)

```sql
-- Player stats table (one row per user)
CREATE TABLE SaveData (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    UserId INTEGER NOT NULL UNIQUE,
    PlayerHP INTEGER NOT NULL,
    Money INTEGER NOT NULL,
    RoundNumber INTEGER NOT NULL
);

-- Player upgrades table (multiple rows per user)
CREATE TABLE PlayerUpgrades (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    UserId INTEGER NOT NULL,
    UpgradeName TEXT NOT NULL,
    UpgradeLevel INTEGER NOT NULL,
    UNIQUE(UserId, UpgradeName)
);
```

## Usage Guide

### 1. Initialize the System

```csharp
// Initialize authentication handler
AuthHandler authHandler = new AuthHandler();
authHandler.InitializeDatabase();

// Initialize SQL handler for saves
SQLHandler sqlHandler = new SQLHandler();
sqlHandler.InitializeDatabase();
```

### 2. Register a New User

```csharp
bool success = authHandler.RegisterUser("username", "password");
if (success)
{
    Console.WriteLine("User registered successfully!");
}
```

**Requirements:**
- Username must be at least 3 characters long
- Username must be unique
- Password must be at least 6 characters long

### 3. Login (Authenticate)

```csharp
User? user = authHandler.AuthenticateUser("username", "password");
if (user != null)
{
    Console.WriteLine($"Welcome, {user.Username}!");
    // Set the current user for save operations
    sqlHandler.SetCurrentUser(user);
}
else
{
    Console.WriteLine("Invalid credentials.");
}
```

### 4. Save Game Data

```csharp
// Make sure a user is logged in first
Dictionary<string, int> upgrades = new Dictionary<string, int>
{
    { "Health Upgrade", 2 },
    { "Damage Upgrade", 1 }
};

sqlHandler.SaveGame(
    playerHP: 85,
    money: 500,
    roundNumber: 3,
    upgrades: upgrades
);
```

### 5. Load Game Data

```csharp
SaveData saveData = sqlHandler.LoadGame();
Console.WriteLine($"HP: {saveData.PlayerHP}");
Console.WriteLine($"Money: {saveData.Money}");
Console.WriteLine($"Round: {saveData.RoundNumber}");

foreach (var upgrade in saveData.Upgrades)
{
    Console.WriteLine($"{upgrade.Key}: Level {upgrade.Value}");
}
```

### 6. Change Password

```csharp
bool changed = authHandler.ChangePassword(
    username: "username",
    oldPassword: "oldpass",
    newPassword: "newpass"
);
```

### 7. Reset Save (User-Specific)

```csharp
sqlHandler.ResetSave(); // Resets save for currently logged-in user
```

### 8. Logout

```csharp
sqlHandler.ClearCurrentUser();
```

## Security Features

### Password Hashing

- Passwords are **never** stored in plain text
- SHA256 hashing algorithm is used
- Each password is converted to a 64-character hexadecimal hash
- Even database administrators cannot see user passwords

### User Isolation

- Each user's save data is completely isolated
- UserId is used as the key for all save operations
- No user can access another user's save data
- Attempting to save/load without login returns an error

### Input Validation

- Username length validation (minimum 3 characters)
- Password length validation (minimum 6 characters)
- Empty username/password rejection
- Duplicate username prevention via UNIQUE constraint

### Database Transactions

- All save operations use transactions
- Either all data is saved, or nothing changes
- Prevents partial saves that could corrupt data
- Automatic rollback on errors

## Migration from Old System

The old system used a single save file (Id = 1) without user authentication. To migrate:

1. **Backup existing saves**: Copy `savefile.db` before updating
2. **Create user accounts**: Register all players who need access
3. **Manual migration**: If needed, manually associate old save data with new user IDs

## Testing

The `UnitTestHandler` has been updated with comprehensive tests:

- `TestUserAuthentication()`: Tests registration and login
- `TestDefaultSaveCreation()`: Verifies default save creation for users
- `TestSaveAndLoadBasicData()`: Tests save/load with user context
- `TestSaveAndLoadUpgrades()`: Tests upgrade persistence per user
- `TestResetSave()`: Tests user-specific save reset
- `TestUpgradeOverwrite()`: Tests upgrade updates per user
- `TestMultipleUsers()`: Tests data isolation between users

Run tests with:
```csharp
UnitTestHandler testHandler = new UnitTestHandler();
testHandler.RunAllTests();
```

## Example Usage

See `Examples/AuthenticationExample.cs` for a complete working example demonstrating all features.

## Best Practices

1. **Always check login status** before saving/loading
2. **Handle null returns** from authentication (failed login)
3. **Logout users** when switching accounts
4. **Validate input** before calling authentication methods
5. **Use try-catch blocks** for database operations in production
6. **Keep user sessions secure** - don't expose User objects unnecessarily

## Troubleshooting

### "Cannot save game: No user logged in"
- Solution: Call `sqlHandler.SetCurrentUser(user)` after successful authentication

### "Username is already taken"
- Solution: Choose a different username or authenticate with existing account

### "Invalid username or password"
- Solution: Verify credentials are correct and user is registered

### Database locked errors
- Solution: Ensure database connections are properly disposed with `using` statements

## Future Enhancements

Potential improvements for the security system:

- **Salt**: Add salt to password hashing for extra security
- **bcrypt/Argon2**: Use more secure hashing algorithms
- **Session tokens**: Implement token-based authentication
- **Account recovery**: Add password reset functionality
- **Role-based access**: Add user roles (admin, player, etc.)
- **Audit logging**: Log all authentication attempts
- **2FA**: Two-factor authentication support

## Security Considerations

⚠️ **Important Notes:**

1. SHA256 is secure for basic use but consider bcrypt for production
2. No salt is used - same password = same hash
3. No brute-force protection - add rate limiting for production
4. No password complexity requirements beyond length
5. Database files are not encrypted - consider file encryption for sensitive data
6. User sessions are in-memory only - no persistent session management

For production use, consider implementing additional security measures based on your specific requirements.
