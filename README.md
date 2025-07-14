# ChatApp Backend - Production-Ready Real-Time Chat API

A production-ready real-time chat backend built with **ASP.NET Core 8**, featuring JWT authentication, SignalR WebSockets, PostgreSQL database, Redis caching, and comprehensive CI/CD pipeline.

## 🚀 Features

### Core Features
- **JWT Authentication** with access and refresh tokens
- **Secure Token Rotation** and expiry handling
- **Real-time Messaging** using SignalR WebSockets
- **Public and Private Chat Groups** with invite/approval system
- **Soft-deletable Messages** (edit/delete only your own)
- **Message Pagination** and full-text search
- **File Upload Support** with disk storage
- **User Status Management** (Online, Away, Busy, Offline)

### Technical Features
- **PostgreSQL** as the main relational database
- **Redis** for caching and session management
- **Clean Architecture** following best practices
- **Entity Framework Core** for database operations
- **Docker Support** with docker-compose
- **CI/CD Pipeline** using GitHub Actions
- **Comprehensive Testing** with xUnit
- **Health Checks** and monitoring
- **Structured Logging** with Serilog

## 📋 Prerequisites

- **.NET 8.0 SDK** or later
- **PostgreSQL 15+**
- **Redis 7+** (optional, for caching)
- **Docker & Docker Compose** (for containerized deployment)

## 🛠️ Quick Start

### 1. Clone the Repository
```bash
git clone <your-repo-url>
cd ChatApp.Backend
```

### 2. Configure Database
Update the connection string in `appsettings.Development.json`:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=chatappdb_dev;Username=your_user;Password=your_password"
  }
}
```

### 3. Configure JWT Settings
Update JWT settings in `appsettings.Development.json`:
```json
{
  "JwtSettings": {
    "SecretKey": "your-super-secure-secret-key-change-this-in-production-32-chars-minimum",
    "Issuer": "ChatApp.Backend",
    "Audience": "ChatApp.Frontend",
    "AccessTokenExpiryMinutes": 15,
    "RefreshTokenExpiryDays": 7
  }
}
```

### 4. Run Database Migrations
```bash
dotnet ef database update
```

### 5. Run the Application
```bash
dotnet run
```

The API will be available at:
- **HTTP**: `http://localhost:5000`
- **HTTPS**: `https://localhost:5001`
- **Swagger UI**: `http://localhost:5000` (in development)

## 🐳 Docker Deployment

### Using Docker Compose (Recommended)
```bash
# Start all services (PostgreSQL, Redis, ChatApp Backend)
docker-compose up -d

# View logs
docker-compose logs -f chatapp-backend

# Stop all services
docker-compose down
```

### Manual Docker Build
```bash
# Build the image
docker build -t chatapp-backend .

# Run with external database
docker run -d \
  --name chatapp-backend \
  -p 8080:80 \
  -e ConnectionStrings__DefaultConnection="Host=your-db-host;Database=chatapp;Username=user;Password=pass" \
  chatapp-backend
```

## 🏗️ Architecture

### Project Structure
```
ChatApp.Backend/
├── Controllers/          # API Controllers
├── DTOs/                # Data Transfer Objects
├── Hubs/                # SignalR Hubs
├── Models/              # Entity Models
├── Services/            # Business Logic Services
│   └── Interfaces/      # Service Interfaces
├── Migrations/          # EF Core Migrations
├── Properties/          # Launch settings
├── appsettings.json     # Configuration files
├── Program.cs           # Application entry point
└── Dockerfile           # Docker configuration

ChatApp.Backend.Tests/   # Unit and Integration Tests
└── Services/            # Service Tests
```

### Database Schema

#### Users Table
- `Id` (PK, UUID)
- `Username` (Unique)
- `Email` (Unique)
- `PasswordHash`
- `DisplayName`
- `ProfilePictureUrl`
- `Status` (Online, Away, Busy, Offline)
- `LastActiveAt`
- `CreatedAt`, `UpdatedAt`, `IsDeleted`

#### Groups Table
- `Id` (PK, UUID)
- `Name`
- `Description`
- `IsPrivate`
- `CreatedBy` (FK to Users)
- `InviteCode`
- `CreatedAt`, `UpdatedAt`, `IsDeleted`

#### Messages Table
- `Id` (PK, UUID)
- `Content`
- `UserId` (FK to Users)
- `GroupId` (FK to Groups)
- `ReplyToMessageId` (FK to Messages, self-referencing)
- `AttachmentUrls` (JSON array)
- `CreatedAt`, `UpdatedAt`, `IsDeleted`, `IsEdited`

#### GroupUsers Table (Many-to-Many)
- `UserId` (PK, FK to Users)
- `GroupId` (PK, FK to Groups)
- `Role` (Admin, Moderator, Member)
- `IsApproved`, `IsBanned`
- `JoinedAt`, `LeftAt`, `IsDeleted`

#### RefreshTokens Table
- `Id` (PK, UUID)
- `UserId` (FK to Users)
- `Token`
- `Jti` (JWT ID)
- `DeviceInfo`, `IpAddress`
- `ExpiresAt`, `RevokedAt`, `CreatedAt`

## 🔌 API Endpoints

### Authentication
- `POST /api/auth/register` - Register new user
- `POST /api/auth/login` - User login
- `POST /api/auth/refresh` - Refresh access token
- `POST /api/auth/revoke` - Revoke refresh token
- `POST /api/auth/revoke-all` - Revoke all tokens
- `GET /api/auth/me` - Get current user info

### Groups (Coming Soon)
- `GET /api/groups` - Get user's groups
- `POST /api/groups` - Create new group
- `GET /api/groups/{id}` - Get group details
- `PUT /api/groups/{id}` - Update group
- `DELETE /api/groups/{id}` - Delete group
- `POST /api/groups/{id}/join` - Join group
- `POST /api/groups/{id}/leave` - Leave group

### Messages (Coming Soon)
- `GET /api/groups/{id}/messages` - Get messages
- `POST /api/groups/{id}/messages` - Send message
- `PUT /api/messages/{id}` - Edit message
- `DELETE /api/messages/{id}` - Delete message
- `GET /api/messages/search` - Search messages

### Files (Coming Soon)
- `POST /api/files/upload` - Upload file
- `GET /api/files/{filename}` - Download file
- `DELETE /api/files/{filename}` - Delete file

## 🔄 SignalR Hub Events

### Client to Server
- `JoinGroup(groupId)` - Join a group
- `LeaveGroup(groupId)` - Leave a group
- `SendMessageToGroup(groupId, message)` - Send message
- `SendTypingIndicator(groupId, isTyping)` - Typing indicator
- `UpdateStatus(status)` - Update user status

### Server to Client
- `ReceiveMessage(message)` - New message received
- `MessageEdited(message)` - Message was edited
- `MessageDeleted(messageId)` - Message was deleted
- `UserJoined(userId, groupId)` - User joined group
- `UserLeft(userId, groupId)` - User left group
- `UserTyping(userId, groupId, isTyping)` - User typing
- `UserOnline(userId, username)` - User came online
- `UserOffline(userId, username)` - User went offline
- `UserStatusChanged(userId, status)` - User status changed

## 🧪 Testing

### Run All Tests
```bash
dotnet test
```

### Run with Coverage
```bash
dotnet test --collect:"XPlat Code Coverage"
```

### Test Structure
- **Unit Tests**: Service layer testing with mocked dependencies
- **Integration Tests**: End-to-end API testing
- **Test Database**: In-memory database for isolated testing

## 🚀 CI/CD Pipeline

The project includes a comprehensive GitHub Actions pipeline:

### Pipeline Stages
1. **Build & Test** - Compile, test, and generate coverage
2. **Security Scan** - Vulnerability scanning with Trivy
3. **Docker Build** - Build and push Docker images
4. **Deploy** - Deploy to Azure Container Apps (configurable)

### Pipeline Triggers
- **Push** to `main` or `develop` branches
- **Pull Requests** to `main` branch

### Required Secrets
- `AZURE_CREDENTIALS` - Azure service principal (for deployment)
- `GITHUB_TOKEN` - Automatically provided for package registry

## 🔒 Security Features

### Authentication & Authorization
- **JWT Bearer Token** authentication
- **Refresh Token Rotation** for enhanced security
- **Device Tracking** for token management
- **Role-Based Access Control** (Admin, Moderator, Member)

### Security Headers
- `X-Content-Type-Options: nosniff`
- `X-Frame-Options: DENY`
- `X-XSS-Protection: 1; mode=block`
- `Referrer-Policy: strict-origin-when-cross-origin`

### Data Protection
- **Password Hashing** with BCrypt
- **Soft Deletes** for audit trail
- **Input Validation** with Data Annotations
- **SQL Injection Protection** with EF Core

## 📊 Monitoring & Logging

### Health Checks
- **Database Connectivity**: `/health`
- **Application Status**: Built-in health monitoring

### Structured Logging
- **Serilog** for structured logging
- **File Logging** with daily rotation
- **Console Logging** for development
- **Log Levels**: Debug, Information, Warning, Error

### Metrics (Planned)
- Request/response times
- Active connections
- Message throughput
- Error rates

## 🔧 Configuration

### Environment Variables

#### Required
- `ConnectionStrings__DefaultConnection` - PostgreSQL connection
- `JwtSettings__SecretKey` - JWT signing key (32+ characters)

#### Optional
- `ConnectionStrings__Redis` - Redis connection
- `JwtSettings__Issuer` - JWT issuer
- `JwtSettings__Audience` - JWT audience
- `JwtSettings__AccessTokenExpiryMinutes` - Access token lifetime
- `JwtSettings__RefreshTokenExpiryDays` - Refresh token lifetime

### File Upload Configuration
```json
{
  "FileUpload": {
    "MaxFileSize": 104857600,
    "AllowedExtensions": [".jpg", ".jpeg", ".png", ".gif", ".pdf", ".doc", ".docx"],
    "UploadPath": "uploads"
  }
}
```

## 🌐 CORS Configuration

Configure allowed origins in `appsettings.json`:
```json
{
  "AllowedOrigins": [
    "http://localhost:3000",
    "https://localhost:3001",
    "https://your-frontend-domain.com"
  ]
}
```

## 📱 Client Integration

### WebSocket Connection (SignalR)
```javascript
const connection = new signalR.HubConnectionBuilder()
    .withUrl("/chathub", {
        accessTokenFactory: () => accessToken
    })
    .build();

// Join a group
await connection.invoke("JoinGroup", groupId);

// Send message
await connection.invoke("SendMessageToGroup", groupId, message);

// Listen for messages
connection.on("ReceiveMessage", (message) => {
    console.log("New message:", message);
});
```

### REST API Integration
```javascript
// Login
const response = await fetch('/api/auth/login', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ email, password })
});

// Authenticated requests
const response = await fetch('/api/groups', {
    headers: { 
        'Authorization': `Bearer ${accessToken}`,
        'Content-Type': 'application/json' 
    }
});
```

## 🚀 Deployment

### Azure Container Apps (Recommended)
1. Create Azure Container Apps environment
2. Configure container app with environment variables
3. Set up continuous deployment from GitHub Container Registry
4. Configure custom domain and SSL certificates

### Docker Swarm
```bash
docker stack deploy -c docker-compose.yml chatapp
```

### Kubernetes
```bash
kubectl apply -f k8s/
```

## 🛠️ Development

### Prerequisites
- .NET 8.0 SDK
- PostgreSQL (local or containerized)
- Redis (optional)
- Visual Studio 2022 or VS Code

### Development Workflow
1. **Fork** the repository
2. **Create** a feature branch
3. **Make** your changes
4. **Write** tests for new functionality
5. **Run** tests and ensure they pass
6. **Submit** a pull request

### Code Style
- Follow .NET naming conventions
- Use async/await for I/O operations
- Add XML documentation for public APIs
- Keep controllers thin, business logic in services
- Use dependency injection for loose coupling

## 📄 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## 🤝 Contributing

1. Fork the project
2. Create your feature branch (`git checkout -b feature/amazing-feature`)
3. Commit your changes (`git commit -m 'Add some amazing feature'`)
4. Push to the branch (`git push origin feature/amazing-feature`)
5. Open a Pull Request

## 📞 Support

- **Issues**: [GitHub Issues](https://github.com/your-repo/issues)
- **Discussions**: [GitHub Discussions](https://github.com/your-repo/discussions)
- **Email**: support@chatapp.com

## 🗺️ Roadmap

### Phase 1 (Current)
- ✅ JWT Authentication
- ✅ Real-time messaging
- ✅ Group management
- ✅ Docker support
- ✅ CI/CD pipeline

### Phase 2 (Planned)
- 📝 Message search functionality
- 📝 File upload/download
- 📝 User profiles
- 📝 Message reactions
- 📝 Push notifications

### Phase 3 (Future)
- 📝 Voice/video calls
- 📝 Screen sharing
- 📝 Bot integration
- 📝 Advanced moderation
- 📝 Analytics dashboard

---

**Built with ❤️ using ASP.NET Core 8**
