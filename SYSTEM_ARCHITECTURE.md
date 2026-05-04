# System Architecture Diagram

## Component Overview

```
┌─────────────────────────────────────────────────────────────┐
│                        YOUR GAME                             │
│                     (Program.cs, etc.)                       │
└────────────┬────────────────────────────┬───────────────────┘
             │                            │
             │ Uses                       │ Uses
             ▼                            ▼
┌────────────────────────┐   ┌──────────────────────────┐
│    AuthHandler         │   │     SQLHandler           │
│  (Authentication)      │   │   (Save/Load Game)       │
├────────────────────────┤   ├──────────────────────────┤
│ - RegisterUser()       │   │ - SetCurrentUser()       │
│ - AuthenticateUser()   │   │ - SaveGame()             │
│ - ChangePassword()     │   │ - LoadGame()             │
│ - UserExists()         │   │ - ResetSave()            │
└────────┬───────────────┘   └────────┬─────────────────┘
         │                            │
         │ Stores/Reads               │ Stores/Reads
         ▼                            ▼
┌────────────────────┐       ┌────────────────────────┐
│    users.db        │       │    savefile.db         │
├────────────────────┤       ├────────────────────────┤
│ [Users]            │       │ [SaveData]             │
│ - UserId (PK)      │       │ - Id (PK)              │
│ - Username (UQ)    │       │ - UserId (UQ, FK)      │
│ - PasswordHash     │       │ - PlayerHP             │
│ - CreatedAt        │       │ - Money                │
│                    │       │ - RoundNumber          │
│                    │       │                        │
│                    │       │ [PlayerUpgrades]       │
│                    │       │ - Id (PK)              │
│                    │       │ - UserId (FK)          │
│                    │       │ - UpgradeName          │
│                    │       │ - UpgradeLevel         │
│                    │       │ UNIQUE(UserId, Name)   │
└────────────────────┘       └────────────────────────┘
```

## Authentication Flow

```
┌──────────┐
│  START   │
└────┬─────┘
     │
     ▼
┌─────────────────┐
│ Initialize      │
│ AuthHandler     │
└────┬────────────┘
     │
     ▼
┌─────────────────┐      No      ┌──────────────┐
│ User Exists?    ├──────────────►│  Register    │
└────┬────────────┘               │  New User    │
     │ Yes                        └──────┬───────┘
     │                                   │
     ▼                                   │
┌─────────────────┐                     │
│ Enter Username  │◄────────────────────┘
│ and Password    │
└────┬────────────┘
     │
     ▼
┌─────────────────┐
│ AuthHandler     │
│ .Authenticate   │
│ User()          │
└────┬────────────┘
     │
     ├─── Success ──►┌──────────────┐
     │               │ Return User  │
     │               │ Object       │
     │               └──────┬───────┘
     │                      │
     │                      ▼
     │               ┌──────────────┐
     │               │ Set Current  │
     │               │ User in SQL  │
     │               │ Handler      │
     │               └──────┬───────┘
     │                      │
     │                      ▼
     │               ┌──────────────┐
     │               │ User Logged  │
     │               │ In - Ready   │
     │               │ to Play      │
     │               └──────────────┘
     │
     └─── Failed ───►┌──────────────┐
                     │ Show Error   │
                     │ Try Again    │
                     └──────────────┘
```

## Save/Load Flow

```
                    ┌──────────────────┐
                    │ User Logged In?  │
                    └────┬────────┬────┘
                         │ No     │ Yes
                         │        │
              ┌──────────┘        └──────────┐
              │                              │
              ▼                              ▼
    ┌─────────────────┐          ┌──────────────────┐
    │ Show Error:     │          │  SAVE OPERATION  │
    │ "Not Logged In" │          │  or              │
    └─────────────────┘          │  LOAD OPERATION  │
                                 └────┬─────────────┘
                                      │
                    ┌─────────────────┼─────────────────┐
                    │                 │                 │
                    ▼                 ▼                 ▼
        ┌───────────────────┐ ┌──────────────┐ ┌──────────────┐
        │ SaveGame()        │ │ LoadGame()   │ │ ResetSave()  │
        │                   │ │              │ │              │
        │ 1. Get UserId     │ │ 1. Get User  │ │ 1. Get User  │
        │ 2. Update Save    │ │    Id        │ │    Id        │
        │    Data           │ │ 2. Query     │ │ 2. Reset to  │
        │ 3. Upsert         │ │    SaveData  │ │    Defaults  │
        │    Upgrades       │ │ 3. Query     │ │ 3. Delete    │
        │ 4. Commit         │ │    Upgrades  │ │    Upgrades  │
        └───────────────────┘ └──────────────┘ └──────────────┘
```

## Multi-User Data Isolation

```
users.db                         savefile.db
┌──────────────────┐            ┌─────────────────────────┐
│ Users            │            │ SaveData                │
├──────────────────┤            ├─────────────────────────┤
│ UserId: 1        │───────────►│ UserId: 1               │
│ Username: Alice  │            │ HP: 100, Money: 500     │
│ PasswordHash: XX │            │                         │
│                  │            │ PlayerUpgrades:         │
│ UserId: 2        │            │ - Health Upgrade: Lvl 2 │
│ Username: Bob    │───┐        │ - Speed Upgrade: Lvl 1  │
│ PasswordHash: YY │   │        └─────────────────────────┘
└──────────────────┘   │        ┌─────────────────────────┐
                       └───────►│ UserId: 2               │
                                │ HP: 75, Money: 1000     │
                                │                         │
                                │ PlayerUpgrades:         │
                                │ - Damage Upgrade: Lvl 3 │
                                └─────────────────────────┘

Note: Each user's data is completely isolated.
Alice cannot access Bob's saves, and vice versa.
```

## Password Security Flow

```
┌─────────────────┐
│ User Enters:    │
│ "mypassword123" │
└────┬────────────┘
     │
     ▼
┌─────────────────────────────────┐
│ AuthHandler.HashPassword()      │
│                                 │
│ 1. Convert to bytes             │
│ 2. Apply SHA256 hashing         │
│ 3. Convert to hex string        │
└────┬────────────────────────────┘
     │
     ▼
┌─────────────────────────────────┐
│ Hashed Password:                │
│ "8d969eef6ecad3c29a3a629280e68..." │
│ (64 character hex string)       │
└────┬────────────────────────────┘
     │
     ▼
┌─────────────────────────────────┐
│ Stored in Database              │
│ users.db -> Users.PasswordHash  │
│                                 │
│ Original password is NEVER      │
│ stored or retrievable!          │
└─────────────────────────────────┘

When user logs in:
┌─────────────────┐
│ User enters     │
│ password again  │
└────┬────────────┘
     │
     ▼
┌─────────────────┐
│ Hash the input  │
└────┬────────────┘
     │
     ▼
┌─────────────────────┐       ┌─────────────┐
│ Compare hashes:     │  Yes  │ Login       │
│ Input == Stored?    ├──────►│ Success!    │
└────┬────────────────┘       └─────────────┘
     │ No
     ▼
┌─────────────────┐
│ Login Failed    │
└─────────────────┘
```

## Class Relationships

```
┌────────────────────────────────────────────────────────────┐
│                         Models                             │
├────────────────────────────────────────────────────────────┤
│                                                            │
│  ┌─────────────┐                  ┌──────────────┐        │
│  │    User     │                  │   SaveData   │        │
│  ├─────────────┤                  ├──────────────┤        │
│  │ + UserId    │                  │ + UserId     │        │
│  │ + Username  │                  │ + PlayerHP   │        │
│  │ + PasswordH.│                  │ + Money      │        │
│  │ + CreatedAt │                  │ + RoundNo    │        │
│  └─────────────┘                  │ + Upgrades   │        │
│                                   └──────────────┘        │
└────────────────────────────────────────────────────────────┘
                  ▲                           ▲
                  │ Creates/Uses              │ Creates/Uses
                  │                           │
┌─────────────────┴───────────────────────────┴──────────────┐
│                        Handlers                             │
├────────────────────────────────────────────────────────────┤
│                                                            │
│  ┌─────────────────┐              ┌───────────────────┐   │
│  │  AuthHandler    │              │   SQLHandler      │   │
│  ├─────────────────┤              ├───────────────────┤   │
│  │ - connectionStr │              │ - connectionStr   │   │
│  │                 │              │ - currentUser     │   │
│  │ + Initialize()  │              │                   │   │
│  │ + Register()    │              │ + SetCurrentUser()│   │
│  │ + Authenticate()│              │ + SaveGame()      │   │
│  │ + ChangePass()  │              │ + LoadGame()      │   │
│  │ + UserExists()  │              │ + ResetSave()     │   │
│  │ - HashPassword()│              │ + PrintSaveData() │   │
│  └─────────────────┘              └───────────────────┘   │
│                                                            │
└────────────────────────────────────────────────────────────┘
```

## Test Coverage

```
┌────────────────────────────────────────────────────────┐
│              UnitTestHandler Tests                     │
├────────────────────────────────────────────────────────┤
│                                                        │
│  ✓ TestUserAuthentication                             │
│    ├─ User registration                               │
│    ├─ Duplicate username rejection                    │
│    ├─ Valid login                                     │
│    └─ Invalid login rejection                         │
│                                                        │
│  ✓ TestDefaultSaveCreation                            │
│    └─ Default save values with user                   │
│                                                        │
│  ✓ TestSaveAndLoadBasicData                           │
│    └─ Save and load HP, money, round                  │
│                                                        │
│  ✓ TestSaveAndLoadUpgrades                            │
│    └─ Multiple upgrades save/load                     │
│                                                        │
│  ✓ TestResetSave                                      │
│    └─ Reset to defaults per user                      │
│                                                        │
│  ✓ TestUpgradeOverwrite                               │
│    └─ Update existing upgrade                         │
│                                                        │
│  ✓ TestMultipleUsers                                  │
│    ├─ User 1 data isolation                           │
│    ├─ User 2 data isolation                           │
│    └─ Cross-user data verification                    │
│                                                        │
└────────────────────────────────────────────────────────┘
```

## Legend

```
PK  = Primary Key
FK  = Foreign Key
UQ  = Unique Constraint
──► = Data Flow Direction
┌─┐ = Component/Process Box
│   = Connection/Relationship
```

## Quick Reference

### Database Files
- `users.db` - Stores user authentication data
- `savefile.db` - Stores game save data per user

### Key Classes
- `User` - User account model
- `SaveData` - Game save data model
- `AuthHandler` - Authentication operations
- `SQLHandler` - Save/load operations

### Relationships
- One User → One SaveData (1:1)
- One User → Many PlayerUpgrades (1:N)
- SaveData linked to User via UserId
- PlayerUpgrades linked to User via UserId
