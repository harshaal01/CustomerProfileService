namespace CustomerProfileService.Infrastructure.Query;

public static class QueryGenerator
{
    public static string RegisterUser() =>
        @"INSERT INTO Users (Name, Email, Password) VALUES (@Name, @Email, @Password)";

    public static string GetUserByEmail() =>
        @"SELECT Id, Name, Email, Password FROM Users WHERE Email = @Email";
    public static string GetAll() =>
        "SELECT * FROM Customers";

    public static string Insert() =>
        @"INSERT INTO Customers (Name, Contact, City, Email)
          VALUES (@Name, @Contact, @City, @Email)";

    public static string Update() =>
        @"UPDATE Customers 
          SET Name=@Name, Contact=@Contact, City=@City, Email=@Email
          WHERE Id=@Id";

    public static string Delete() =>
        "DELETE FROM Customers WHERE Id=@Id";

    public static string GetById() => 
        "SELECT 1 FROM Customers WHERE Id = @Id LIMIT 1";
}
