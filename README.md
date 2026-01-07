# CustomerProfileService

CustomerProfileService is a small REST API built with .NET (ASP.NET Core) that provides CRUD operations for customer profiles and simple user authentication using JWT.

**Overview**
- **Purpose**: Manage customers (create, read, update, delete) and provide user registration/login with JWT-based authentication.
- **Projects**: solution contains three projects: `CustomerProfileService.API`, `CustomerProfileService.Domain`, and `CustomerProfileService.Infrastructure`.

**Tech Stack**
- **Framework**: .NET 8 (ASP.NET Core)
- **Database**: MySQL (via `MySql.Data`)
- **Authentication**: JWT (symmetric key) configured in `Startup.cs`
- **Password hashing**: `BCrypt` (used in `AuthService`)
- **Logging**: Serilog (file sink configured in `appsettings.json`)
- **API docs**: Swagger UI available at `/swagger`

**Quick Start**
- **Restore & build**: `dotnet restore` then `dotnet build`
- **Run API**: `dotnet run --project CustomerProfileService.API`
- **Swagger**: open `http://localhost:5000/swagger` (port may vary) after the API runs

**Configuration**
- **App settings**: edit `CustomerProfileService.API/appsettings.Development.json` or `appsettings.json`.
- **Connection string keys**:
	- `ConnectionStrings:MySql` : MySQL connection string used by `SQLHelper`.
	- `ConnectionStrings:AuthSecretKey` : symmetric secret key used to sign JWT tokens (keep this secret in production).
- **Logs**: Serilog writes to `Logs/app-.txt` (rolling daily) by default.

**Database schema (minimal)**
Run these SQL snippets to create the required tables:

```sql
CREATE TABLE Users (
	Id INT AUTO_INCREMENT PRIMARY KEY,
	Name VARCHAR(255) NOT NULL,
	Email VARCHAR(255) NOT NULL UNIQUE,
	Password VARCHAR(255) NOT NULL
);

CREATE TABLE Customers (
	Id INT AUTO_INCREMENT PRIMARY KEY,
	Name VARCHAR(255) NOT NULL,
	Contact VARCHAR(50) NOT NULL,
	City VARCHAR(100) NOT NULL,
	Email VARCHAR(255) NOT NULL
);
```

**API Endpoints**
- **Authentication** (no token required)
	- `POST /api/Auth/registerUser` : register a new user. Body: `{"name":"...","email":"...","password":"..."}`
	- `POST /api/Auth/loginUser` : login. Body: `{"email":"...","password":"..."}` → returns `{ "token": "..." }`

- **Customer endpoints** (require `Authorization: Bearer {token}` header)
	- `GET  /api/Customers/GetAllCustomers` : returns list of customers (204 No Content if none)
	- `POST /api/Customers/AddCustomer` : add a customer. Body: `Customer` JSON
	- `POST /api/Customers/UpdateCustomer` : update a customer. Body: `Customer` JSON (must include `Id`)
	- `DELETE /api/Customers/DeleteCustomer/{id}` : delete a customer by id

Example `curl` (register, login, call protected endpoint):

```bash
# Register
curl -X POST http://localhost:5000/api/Auth/registerUser \
	-H "Content-Type: application/json" \
	-d '{"name":"Alice","email":"alice@example.com","password":"secret123"}'

# Login -> get token
TOKEN=$(curl -s -X POST http://localhost:5000/api/Auth/loginUser \
	-H "Content-Type: application/json" \
	-d '{"email":"alice@example.com","password":"secret123"}' | jq -r .token)

# Call protected endpoint
curl -H "Authorization: Bearer $TOKEN" http://localhost:5000/api/Customers/GetAllCustomers
```

**Project structure (high level)**
- `CustomerProfileService.API/` : web API, controllers, `Program.cs`, `Startup.cs`, `appsettings`.
- `CustomerProfileService.Domain/` : domain entities and interfaces (e.g., `Customer`, `User`, `IAuthService`, `ICustomerService`).
- `CustomerProfileService.Infrastructure/` : database helpers (`SQLHelper`), query generator, and service implementations (`AuthService`, `CustomerService`).

**Notes & Caveats**
- JWT secret is currently read from connection strings (`AuthSecretKey`) in `appsettings.Development.json`. For production, store secrets securely (environment variables or secret stores).
- `SQLHelper` expects connection string named `MySql`.
- Error handling in services returns appropriate HTTP status codes (400, 401, 404, 500).

**Contributing**
- Please open issues or pull requests with tests and clear descriptions. Maintain existing coding style and keep changes focused.

**License**
- This repository does not include a license file. Add a `LICENSE` if you intend to make it public.

If you want, I can also:
- add a `docker-compose.yml` for a local MySQL + API setup
- add example Postman collection or automated tests
- add CI workflow to run builds and tests

---
Generated: updated README to reflect the current codebase and usage.