# Firebase + Backend Integration Guide

## Overview

This guide shows how to sync Firebase Authentication users with your backend database by storing the Firebase UID.

## How It Works

1. User registers on your React frontend using Firebase Authentication
2. Firebase creates the user and returns a Firebase UID
3. Your React app sends the registration data **including the Firebase UID** to your backend API
4. The backend stores the user with the Firebase UID in the database
5. Now both Firebase and your database are synced!

## React Frontend Example

### Step 1: Register User with Firebase

```javascript
import { createUserWithEmailAndPassword } from "firebase/auth";
import { auth } from "./firebase-config"; // your firebase config

const registerUser = async (formData) => {
  try {
    // 1. Create user in Firebase
    const userCredential = await createUserWithEmailAndPassword(
      auth,
      formData.email,
      formData.password
    );

    const firebaseUser = userCredential.user;
    const firebaseUid = firebaseUser.uid; // This is the Firebase UID

    // 2. Register user in your backend with Firebase UID
    const response = await fetch("http://localhost:5020/api/auth/register", {
      method: "POST",
      headers: {
        "Content-Type": "application/json",
      },
      body: JSON.stringify({
        firstName: formData.firstName,
        lastName: formData.lastName,
        email: formData.email,
        password: formData.password,
        confirmPassword: formData.confirmPassword,
        firebaseUid: firebaseUid, // ← Include Firebase UID here
        phoneNumber: formData.phoneNumber,
        companyName: formData.companyName,
        address: formData.address,
        city: formData.city,
        postalCode: formData.postalCode,
        country: formData.country,
        role: formData.role, // 1=ServiceProvider, 2=InsuranceAgent, 3=Admin
        acceptTerms: formData.acceptTerms,
      }),
    });

    if (!response.ok) {
      throw new Error("Failed to register user in backend");
    }

    const data = await response.json();

    // 3. Store the JWT token from backend
    localStorage.setItem("token", data.token);
    localStorage.setItem("refreshToken", data.refreshToken);
    localStorage.setItem("user", JSON.stringify(data.user));

    return data;
  } catch (error) {
    console.error("Registration error:", error);
    throw error;
  }
};
```

### Step 2: Login Flow

After registration, for login you can use either:

- **Firebase login** (if you want to use Firebase features)
- **Backend JWT login** (if you prefer your own auth)
- **Both** (Firebase for real-time features, JWT for API calls)

```javascript
// Option 1: Firebase + Backend verification
const loginWithFirebase = async (email, password) => {
  try {
    // Login with Firebase
    const userCredential = await signInWithEmailAndPassword(
      auth,
      email,
      password
    );
    const firebaseUid = userCredential.user.uid;

    // Get JWT token from your backend
    const response = await fetch("http://localhost:5020/api/auth/login", {
      method: "POST",
      headers: {
        "Content-Type": "application/json",
      },
      body: JSON.stringify({ email, password }),
    });

    const data = await response.json();
    localStorage.setItem("token", data.token);

    return data;
  } catch (error) {
    console.error("Login error:", error);
    throw error;
  }
};

// Option 2: Backend JWT only
const loginWithBackend = async (email, password) => {
  try {
    const response = await fetch("http://localhost:5020/api/auth/login", {
      method: "POST",
      headers: {
        "Content-Type": "application/json",
      },
      body: JSON.stringify({ email, password }),
    });

    const data = await response.json();
    localStorage.setItem("token", data.token);

    return data;
  } catch (error) {
    console.error("Login error:", error);
    throw error;
  }
};
```

### Step 3: Making Authenticated Requests

```javascript
// Use the JWT token for API calls
const getClaims = async () => {
  const token = localStorage.getItem("token");

  const response = await fetch("http://localhost:5020/api/claims", {
    method: "GET",
    headers: {
      Authorization: `Bearer ${token}`,
      "Content-Type": "application/json",
    },
  });

  return response.json();
};
```

## Backend API Endpoints

### Register (with Firebase UID)

```
POST http://localhost:5020/api/auth/register
Content-Type: application/json

{
  "firstName": "John",
  "lastName": "Doe",
  "email": "john@example.com",
  "password": "SecurePass123!",
  "confirmPassword": "SecurePass123!",
  "firebaseUid": "firebase-uid-here", // ← Include this!
  "phoneNumber": "+1234567890",
  "role": 1,
  "acceptTerms": true
}
```

### Login

```
POST http://localhost:5020/api/auth/login
Content-Type: application/json

{
  "email": "john@example.com",
  "password": "SecurePass123!"
}
```

## User Roles

- `1` - ServiceProvider
- `2` - InsuranceAgent
- `3` - Admin

## Response Format

```json
{
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "refreshToken": "guid-token-here",
  "expiry": "2025-10-06T17:30:00Z",
  "user": {
    "id": 1,
    "firstName": "John",
    "lastName": "Doe",
    "email": "john@example.com",
    "firebaseUid": "firebase-uid-here",
    "role": 1,
    "status": 1,
    "createdAt": "2025-10-06T15:30:00Z"
  }
}
```

## Database Schema

The `Users` table now includes:

- `Id` (int, primary key)
- `Email` (unique)
- `PasswordHash`
- **`FirebaseUid`** (nullable, stores Firebase UID)
- `FirstName`, `LastName`
- `PhoneNumber`, `CompanyName`, etc.
- `Role`, `Status`
- Timestamps

## Benefits of This Approach

✅ **Synced Authentication** - Users exist in both Firebase and your database  
✅ **Flexible Login** - Can use Firebase auth OR your own JWT auth  
✅ **Rich User Data** - Store additional user info in your database  
✅ **Firebase Features** - Can still use Firebase Realtime Database, Cloud Messaging, etc.  
✅ **Backend Control** - Full control over user data and business logic

## Testing

You can test the registration endpoint using the provided `InsuranceClaimsAPI.http` file or tools like Postman/Thunder Client.

## Notes

- The `firebaseUid` field is **optional** (nullable in the database)
- If you want to make it required, update the validation in `RegisterRequestDto.cs`
- Password is still stored in the backend for direct login (hashed with BCrypt)
- Firebase UID allows you to link the same user across both systems
