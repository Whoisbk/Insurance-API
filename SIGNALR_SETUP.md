# SignalR Real-Time Communication Setup

## Overview

This API uses SignalR for real-time, bidirectional communication between the backend and frontend. No polling is needed - everything happens instantly via WebSocket connections.

## How It Works

### Backend Setup ✅

1. **SignalR Hubs** are registered in `Program.cs`:

   ```csharp
   builder.Services.AddSignalR();
   app.MapHub<MessageHub>("/messageHub");
   app.MapHub<NotificationHub>("/notificationHub");
   ```

2. **Authentication**: No JWT authentication required. User ID is passed via query string `?userId=<userId>` when connecting to SignalR hubs.

### Flow Examples

#### 1. Quote Status Changes → Real-Time Notification

```
Insurer rejects quote → Backend updates DB → NotificationService.CreateAsync()
→ Saves to DB → Immediately sends via SignalR to User_{userId} group
→ Frontend receives "ReceiveNotification" event → Shows alert/badge instantly
```

#### 2. New Message → Real-Time Delivery

```
User sends message → MessageService.SendAsync()
→ Saves to DB → Immediately sends via SignalR to:
   - Claim_{claimId} room (for claim context)
   - Quote_{quoteId} room (for quote-specific messages)
   - User_{receiverId} group (for personal notifications)
→ Frontend receives "ReceiveMessage" or "NewMessage" event → Updates UI instantly
```

## Frontend Connection

### Connect to SignalR Hubs

```javascript
import * as signalR from "@microsoft/signalr";

// For Messages
const messageConnection = new signalR.HubConnectionBuilder()
  .withUrl(
    `https://your-api.com/messageHub?userId=${userId}&username=${username}`
  )
  .withAutomaticReconnect()
  .build();

// For Notifications
const notificationConnection = new signalR.HubConnectionBuilder()
  .withUrl(`https://your-api.com/notificationHub?userId=${userId}`)
  .withAutomaticReconnect()
  .build();
```

### Listen for Real-Time Events

#### Notifications

```javascript
notificationConnection.on("ReceiveNotification", (notification) => {
  console.log("New notification:", notification);
  // notification contains:
  // - NotificationId
  // - UserId
  // - QuoteId (if related to a quote)
  // - Message
  // - Status
  // - DateSent

  // Show notification badge, toast, etc.
  showNotificationBadge();
});
```

#### Messages

```javascript
// Join a quote room when viewing a quote
messageConnection.invoke("JoinQuoteRoom", quoteId);

// Listen for new messages in that quote
messageConnection.on("ReceiveMessage", (message) => {
  console.log("New message:", message);
  // message contains:
  // - MessageId
  // - ClaimId
  // - QuoteId
  // - SenderId
  // - Content
  // - Subject
  // - Type
  // - CreatedAt

  // Update message list in real-time
  addMessageToUI(message);
});

// Personal message notifications
messageConnection.on("NewMessage", (message) => {
  // Show notification that user has a new message
  showMessageNotification(message);
});
```

### Starting Connections

```javascript
// Start connections
await messageConnection.start();
await notificationConnection.start();

// When viewing a quote page
await messageConnection.invoke("JoinQuoteRoom", quoteId);

// When leaving quote page
await messageConnection.invoke("LeaveQuoteRoom", quoteId);
```

## SignalR Events

### NotificationHub Events

- **Client receives**: `ReceiveNotification` - When a notification is created

### MessageHub Events

- **Client receives**:
  - `ReceiveMessage` - New message in claim/quote room
  - `NewMessage` - Personal message notification
- **Client can invoke**:
  - `JoinClaimRoom(int claimId)` - Join a claim's message room
  - `LeaveClaimRoom(int claimId)` - Leave a claim's message room
  - `JoinQuoteRoom(int quoteId)` - Join a quote's message room
  - `LeaveQuoteRoom(int quoteId)` - Leave a quote's message room

## Groups

Users are automatically added to groups:

- `User_{userId}` - For personal notifications and messages
- `Claim_{claimId}` - For claim-related messages (when joined)
- `Quote_{quoteId}` - For quote-specific messages (when joined)

## Key Features

✅ **No Polling**: Real-time updates via WebSocket
✅ **User ID Authentication**: User ID passed via query string
✅ **Automatic Reconnection**: Built-in reconnection handling
✅ **Group Management**: Users join/leave rooms as needed
✅ **Rich Payloads**: Full object data sent in real-time

## API Requests

All API endpoints now require the `X-User-Id` header to identify the user:

```javascript
fetch("/api/quotes/123/messages", {
  headers: {
    "X-User-Id": userId.toString(),
    "Content-Type": "application/json",
  },
});
```

## Example: Quote Rejection Flow

```
1. Insurer clicks "Reject" on quote
2. Frontend: POST /api/quotes/123/status { status: "Rejected" }
3. Backend: QuoteService.SetStatusAsync()
   → Updates quote in DB
   → NotificationService.CreateAsync()
     → Saves notification to DB
     → SignalR: Clients.Group("User_{providerId}").SendAsync("ReceiveNotification")
4. Provider's frontend: Instantly receives notification
   → Shows toast/alert
   → Updates notification badge
   → No polling needed!
```

## Testing

Test SignalR connections using:

- Browser DevTools Network tab (check WebSocket connections)
- SignalR JavaScript client logging
- Backend logs show connection/disconnection events
