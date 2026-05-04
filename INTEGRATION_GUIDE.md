# Integration Guide

This guide shows how to integrate the authentication system into your existing game flow.

## Quick Start Integration

### Option 1: Simple Console Menu (Recommended for Testing)

Add this to your `Program.cs` or create a new menu handler:

```csharp
using _2DREPOCODE.Handlers;
using _2DREPOCODE.Models;
using System;
using System.Collections.Generic;

public class GameMenu
{
    private AuthHandler authHandler;
    private SQLHandler sqlHandler;
    private User? currentUser = null;

    public GameMenu()
    {
        authHandler = new AuthHandler();
        authHandler.InitializeDatabase();

        sqlHandler = new SQLHandler();
        sqlHandler.InitializeDatabase();
    }

    public void Run()
    {
        while (true)
        {
            if (currentUser == null)
            {
                ShowLoginMenu();
            }
            else
            {
                ShowGameMenu();
            }
        }
    }

    private void ShowLoginMenu()
    {
        Console.Clear();
        Console.WriteLine("=== GAME LOGIN ===");
        Console.WriteLine("1. Login");
        Console.WriteLine("2. Register");
        Console.WriteLine("3. Exit");
        Console.Write("\nChoice: ");

        string choice = Console.ReadLine() ?? "";

        switch (choice)
        {
            case "1":
                Login();
                break;
            case "2":
                Register();
                break;
            case "3":
                Environment.Exit(0);
                break;
        }
    }

    private void Login()
    {
        Console.Clear();
        Console.WriteLine("=== LOGIN ===");
        Console.Write("Username: ");
        string username = Console.ReadLine() ?? "";

        Console.Write("Password: ");
        string password = ReadPassword();

        User? user = authHandler.AuthenticateUser(username, password);
        if (user != null)
        {
            currentUser = user;
            sqlHandler.SetCurrentUser(user);
            Console.WriteLine("\nLogin successful! Press any key to continue...");
            Console.ReadKey();
        }
        else
        {
            Console.WriteLine("\nLogin failed. Press any key to continue...");
            Console.ReadKey();
        }
    }

    private void Register()
    {
        Console.Clear();
        Console.WriteLine("=== REGISTER ===");
        Console.Write("Username (min 3 chars): ");
        string username = Console.ReadLine() ?? "";

        Console.Write("Password (min 6 chars): ");
        string password = ReadPassword();

        Console.Write("\nConfirm Password: ");
        string confirmPassword = ReadPassword();

        if (password != confirmPassword)
        {
            Console.WriteLine("\nPasswords don't match. Press any key to continue...");
            Console.ReadKey();
            return;
        }

        bool success = authHandler.RegisterUser(username, password);
        if (success)
        {
            Console.WriteLine("\nRegistration successful! You can now login. Press any key...");
        }
        else
        {
            Console.WriteLine("\nRegistration failed. Press any key to continue...");
        }
        Console.ReadKey();
    }

    private void ShowGameMenu()
    {
        Console.Clear();
        Console.WriteLine($"=== GAME MENU - Welcome {currentUser!.Username} ===");
        Console.WriteLine("1. New Game");
        Console.WriteLine("2. Load Game");
        Console.WriteLine("3. View Save Data");
        Console.WriteLine("4. Reset Save");
        Console.WriteLine("5. Change Password");
        Console.WriteLine("6. Logout");
        Console.Write("\nChoice: ");

        string choice = Console.ReadLine() ?? "";

        switch (choice)
        {
            case "1":
                StartNewGame();
                break;
            case "2":
                LoadGame();
                break;
            case "3":
                ViewSaveData();
                break;
            case "4":
                ResetSave();
                break;
            case "5":
                ChangePassword();
                break;
            case "6":
                Logout();
                break;
        }
    }

    private void StartNewGame()
    {
        Console.Clear();
        Console.WriteLine("=== NEW GAME ===");

        // Your game initialization logic here
        Dictionary<string, int> upgrades = new Dictionary<string, int>();
        sqlHandler.SaveGame(100, 0, 1, upgrades);

        Console.WriteLine("New game started!");
        Console.WriteLine("Press any key to continue...");
        Console.ReadKey();
    }

    private void LoadGame()
    {
        Console.Clear();
        Console.WriteLine("=== LOAD GAME ===");

        SaveData save = sqlHandler.LoadGame();
        Console.WriteLine($"HP: {save.PlayerHP}");
        Console.WriteLine($"Money: {save.Money}");
        Console.WriteLine($"Round: {save.RoundNumber}");

        // Your game loading logic here

        Console.WriteLine("\nPress any key to continue...");
        Console.ReadKey();
    }

    private void ViewSaveData()
    {
        Console.Clear();
        sqlHandler.PrintSaveData();
        Console.WriteLine("\nPress any key to continue...");
        Console.ReadKey();
    }

    private void ResetSave()
    {
        Console.Clear();
        Console.WriteLine("=== RESET SAVE ===");
        Console.Write("Are you sure? This cannot be undone! (yes/no): ");
        string confirm = Console.ReadLine() ?? "";

        if (confirm.ToLower() == "yes")
        {
            sqlHandler.ResetSave();
            Console.WriteLine("Save reset complete.");
        }
        else
        {
            Console.WriteLine("Reset cancelled.");
        }

        Console.WriteLine("Press any key to continue...");
        Console.ReadKey();
    }

    private void ChangePassword()
    {
        Console.Clear();
        Console.WriteLine("=== CHANGE PASSWORD ===");
        Console.Write("Current Password: ");
        string oldPassword = ReadPassword();

        Console.Write("\nNew Password (min 6 chars): ");
        string newPassword = ReadPassword();

        Console.Write("\nConfirm New Password: ");
        string confirmPassword = ReadPassword();

        if (newPassword != confirmPassword)
        {
            Console.WriteLine("\nPasswords don't match. Press any key...");
            Console.ReadKey();
            return;
        }

        bool success = authHandler.ChangePassword(
            currentUser!.Username, 
            oldPassword, 
            newPassword
        );

        Console.WriteLine(success ? "\nPassword changed successfully!" : "\nFailed to change password.");
        Console.WriteLine("Press any key to continue...");
        Console.ReadKey();
    }

    private void Logout()
    {
        currentUser = null;
        sqlHandler.ClearCurrentUser();
        Console.WriteLine("Logged out successfully.");
    }

    // Helper method to read password without displaying it
    private string ReadPassword()
    {
        string password = "";
        ConsoleKeyInfo key;

        do
        {
            key = Console.ReadKey(true);

            if (key.Key != ConsoleKey.Backspace && key.Key != ConsoleKey.Enter)
            {
                password += key.KeyChar;
                Console.Write("*");
            }
            else if (key.Key == ConsoleKey.Backspace && password.Length > 0)
            {
                password = password.Substring(0, password.Length - 1);
                Console.Write("\b \b");
            }
        }
        while (key.Key != ConsoleKey.Enter);

        return password;
    }
}
```

### Option 2: Integration with Existing Game Loop

If you already have a game loop, integrate authentication like this:

```csharp
public class YourGameClass
{
    private AuthHandler authHandler;
    private SQLHandler sqlHandler;
    private User? currentUser;

    public void Initialize()
    {
        // Initialize authentication
        authHandler = new AuthHandler();
        authHandler.InitializeDatabase();

        // Initialize save system
        sqlHandler = new SQLHandler();
        sqlHandler.InitializeDatabase();

        // Require login before starting game
        LoginRequired();
    }

    private void LoginRequired()
    {
        while (currentUser == null)
        {
            Console.WriteLine("Please login or register:");
            Console.WriteLine("1. Login");
            Console.WriteLine("2. Register");
            string choice = Console.ReadLine() ?? "";

            if (choice == "1")
            {
                currentUser = PerformLogin();
            }
            else if (choice == "2")
            {
                PerformRegistration();
            }
        }

        sqlHandler.SetCurrentUser(currentUser);
    }

    private User? PerformLogin()
    {
        Console.Write("Username: ");
        string username = Console.ReadLine() ?? "";
        Console.Write("Password: ");
        string password = Console.ReadLine() ?? "";

        return authHandler.AuthenticateUser(username, password);
    }

    private void PerformRegistration()
    {
        Console.Write("Username: ");
        string username = Console.ReadLine() ?? "";
        Console.Write("Password: ");
        string password = Console.ReadLine() ?? "";

        authHandler.RegisterUser(username, password);
    }

    public void SaveGameState(int hp, int money, int round, Dictionary<string, int> upgrades)
    {
        // This will automatically save for the current user
        sqlHandler.SaveGame(hp, money, round, upgrades);
    }

    public SaveData LoadGameState()
    {
        // This will automatically load for the current user
        return sqlHandler.LoadGame();
    }
}
```

### Option 3: Automatic Authentication (For Development/Testing)

If you want to skip the login process during development:

```csharp
public void AutoLogin()
{
    AuthHandler authHandler = new AuthHandler();
    authHandler.InitializeDatabase();

    SQLHandler sqlHandler = new SQLHandler();
    sqlHandler.InitializeDatabase();

    // Register if doesn't exist, then login
    string testUsername = "devuser";
    string testPassword = "devpass123";

    if (!authHandler.UserExists(testUsername))
    {
        authHandler.RegisterUser(testUsername, testPassword);
    }

    User? user = authHandler.AuthenticateUser(testUsername, testPassword);
    if (user != null)
    {
        sqlHandler.SetCurrentUser(user);
        Console.WriteLine($"Auto-logged in as {testUsername}");
    }
}
```

## Integration Checklist

- [ ] Initialize `AuthHandler` at application startup
- [ ] Initialize `SQLHandler` at application startup
- [ ] Add login/register UI before game starts
- [ ] Call `sqlHandler.SetCurrentUser(user)` after successful authentication
- [ ] Replace old `SaveGame()` calls with new user-aware version
- [ ] Replace old `LoadGame()` calls with new user-aware version
- [ ] Add logout functionality
- [ ] Handle null user scenarios
- [ ] Test with multiple users
- [ ] Update unit tests if you have custom ones
- [ ] Backup existing save files before deployment

## Common Integration Patterns

### Pattern 1: Session Management

```csharp
public class GameSession
{
    private static GameSession? instance;
    public User? CurrentUser { get; private set; }
    public SQLHandler SaveHandler { get; private set; }

    private GameSession()
    {
        SaveHandler = new SQLHandler();
        SaveHandler.InitializeDatabase();
    }

    public static GameSession Instance
    {
        get
        {
            if (instance == null)
                instance = new GameSession();
            return instance;
        }
    }

    public void Login(User user)
    {
        CurrentUser = user;
        SaveHandler.SetCurrentUser(user);
    }

    public void Logout()
    {
        CurrentUser = null;
        SaveHandler.ClearCurrentUser();
    }

    public bool IsLoggedIn => CurrentUser != null;
}
```

### Pattern 2: Dependency Injection

```csharp
public class GameController
{
    private readonly AuthHandler authHandler;
    private readonly SQLHandler sqlHandler;

    public GameController(AuthHandler auth, SQLHandler sql)
    {
        authHandler = auth;
        sqlHandler = sql;
    }

    // Use authHandler and sqlHandler throughout the class
}

// In your main/startup:
AuthHandler auth = new AuthHandler();
auth.InitializeDatabase();

SQLHandler sql = new SQLHandler();
sql.InitializeDatabase();

GameController game = new GameController(auth, sql);
```

## Troubleshooting Integration Issues

### Issue: "Cannot save game: No user logged in"
**Solution**: Make sure to call `sqlHandler.SetCurrentUser(user)` after authentication:
```csharp
User? user = authHandler.AuthenticateUser(username, password);
if (user != null)
{
    sqlHandler.SetCurrentUser(user);  // Don't forget this!
}
```

### Issue: Save data not persisting
**Solution**: Check that the user remains logged in throughout the session:
```csharp
User? currentUser = sqlHandler.GetCurrentUser();
if (currentUser == null)
{
    // User was logged out, need to re-authenticate
}
```

### Issue: Multiple database connections
**Solution**: Create handlers once and reuse them:
```csharp
// Good - create once
AuthHandler auth = new AuthHandler();
SQLHandler sql = new SQLHandler();

// Bad - creating new instances each time
void SaveGame() 
{
    SQLHandler sql = new SQLHandler(); // Don't do this!
    sql.SaveGame(...);
}
```

## Example: Full Program.cs Integration

```csharp
using _2DREPOCODE.Handlers;
using _2DREPOCODE.Models;
using System;

namespace _2DREPOCODE
{
    internal class Program
    {
        static void Main(string[] args)
        {
            // Option 1: Run unit tests
            Console.WriteLine("Running unit tests...\n");
            UnitTestHandler unitTestHandler = new UnitTestHandler();
            unitTestHandler.RunAllTests();

            Console.WriteLine("\n\nPress any key to start game menu...");
            Console.ReadKey();

            // Option 2: Run game with authentication
            GameMenu menu = new GameMenu();
            menu.Run();
        }
    }
}
```

## Next Steps

1. Review the `SECURITY_DOCUMENTATION.md` file for detailed security information
2. Check `Examples/AuthenticationExample.cs` for code examples
3. Run the unit tests to verify everything works: `UnitTestHandler.RunAllTests()`
4. Integrate authentication into your existing game flow
5. Test with multiple user accounts
6. Consider adding additional security features for production

## Support

For questions or issues with the authentication system:
1. Check the `SECURITY_DOCUMENTATION.md` file
2. Review the example code in `Examples/AuthenticationExample.cs`
3. Run unit tests to ensure system is working: `UnitTestHandler.RunAllTests()`
4. Check troubleshooting sections in this guide
