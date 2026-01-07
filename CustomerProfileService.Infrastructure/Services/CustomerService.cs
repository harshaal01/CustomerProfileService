using System.ComponentModel.DataAnnotations;
using CustomerProfileService.Domain.Entities;
using CustomerProfileService.Domain.Interfaces;
using CustomerProfileService.Infrastructure.Query;
using Microsoft.Extensions.Logging;
using MySql.Data.MySqlClient;

namespace CustomerProfileService.Infrastructure.Services;

public class CustomerService : ICustomerService
{
    private readonly SQLHelper _db;
    private readonly ILogger<CustomerService> _logger;

    public CustomerService(SQLHelper db, ILogger<CustomerService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<List<Customer>> GetAllAsync()
    {
        var customers = new List<Customer>();

        try
        {
            // 🔹 Business validation (example)
            var query = QueryGenerator.GetAll();

            if (string.IsNullOrWhiteSpace(query))
            {
                _logger.LogWarning("GetAll query is empty");
                throw new ValidationException("Query cannot be empty.");
            }

            using var reader = await _db.ExecuteQueryAsync(query);

            while (await reader.ReadAsync())
            {
                customers.Add(MapCustomer(reader));
            }

            _logger.LogInformation(
                "GetAllAsync completed successfully. Records fetched: {Count}",
                customers.Count
            );
            return customers;
        }
        catch (ValidationException ex)
        {
            _logger.LogWarning(ex, "Validation error in GetAllAsync");
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled error in GetAllAsync");
            throw;
        }
        finally
        {
            _db.CloseSqlConnection();   // ✅ close connection
        }

        // 1. Connection is opened
        // 2. Command is executed
        // 3. Database starts streaming rows
        // 4. MySqlDataReader is created
        // 5. Reader is returned to caller
        // 6. Caller reads data row-by-row
        // 7. Caller disposes reader (using block)
        // 8. Connection is automatically closed
    }

    public async Task CreateAsync(Customer customer)
    {
        try
        {
            // 🔹 Validate input
            ValidateCustomer(customer);

            var rowsAffected = await _db.ExecuteNonQuery(
                QueryGenerator.Insert(),
                cmd =>
                {
                    cmd.Parameters.AddWithValue("@Name", customer.Name.Trim());
                    cmd.Parameters.AddWithValue("@Contact", customer.Contact.Trim());
                    cmd.Parameters.AddWithValue("@City", customer.City.Trim());
                    cmd.Parameters.AddWithValue("@Email", customer.Email.Trim());
                });

            // 🔹 Business validation
            if (rowsAffected <= 0)
            {
                _logger.LogError(
                    "Customer creation failed. No rows affected. Email: {Email}",
                    customer.Email
                );
                throw new Exception("Customer creation failed.");
            }
            _logger.LogInformation(
                "Customer created successfully. Email: {Email}",
                customer.Email
            );
        }
        catch (ValidationException)
        {
            _logger.LogWarning("Validation failed while creating customer");
            throw;
        }
        catch (MySqlException ex)
        {
            if (ex.Number == 1062)
            {
                _logger.LogWarning(
                    "Duplicate email detected (MySQL Error 1062). Email already exists."
                );

                throw new ValidationException("Email already exists.");
            }


            throw new ApplicationException("Database error occurred.", ex);
        }
        catch (Exception)
        {
            _logger.LogError("Unhandled error in CreateAsync");
            throw;
        }
        finally
        {
            _db.CloseSqlConnection();   // ✅ close connection
        }
    }

    public async Task UpdateAsync(Customer customer)
    {
        try
        {
            // 🔹 Step 1: Basic validation
            ValidateCustomer(customer);

            // 🔹 Step 2: Check if customer exists
            bool exists = await CustomerExistsAsync(customer.Id);
            if (!exists)
            {
                _logger.LogWarning(
                    "Update failed. Customer not found. Id: {Id}",
                    customer.Id
                );
                throw new Exception(
                    $"Customer with Id {customer.Id} does not exist.");
            }

            // 🔹 Step 3: Perform update
            var rowsAffected = await _db.ExecuteNonQuery(
                QueryGenerator.Update(),
                cmd =>
                {
                    cmd.Parameters.AddWithValue("@Id", customer.Id);
                    cmd.Parameters.AddWithValue("@Name", customer.Name.Trim());
                    cmd.Parameters.AddWithValue("@Contact", customer.Contact.Trim());
                    cmd.Parameters.AddWithValue("@City", customer.City.Trim());
                    cmd.Parameters.AddWithValue("@Email", customer.Email.Trim());
                });

            // 🔹 Safety check (should not happen if exists check passed)
            if (rowsAffected == 0)
            {
                _logger.LogError(
                    "Update failed unexpectedly for CustomerId: {Id}",
                    customer.Id
                );
                throw new Exception("Update failed unexpectedly.");
            }
            _logger.LogInformation(
                "Customer updated successfully. Id: {Id}",
                customer.Id
            );
        }
        catch (ValidationException)
        {
            _logger.LogWarning("Validation error in UpdateAsync");
            throw;
        }
        catch (MySqlException ex)
        {
            throw new ApplicationException("Database error occurred.", ex);
        }
        catch (Exception)
        {
            _logger.LogError("Unhandled error in UpdateAsync");
            throw;
        }
        finally
        {
            _db.CloseSqlConnection();   // ✅ close connection
        }
    }

    // public async Task<UpdateCustomerResponse> UpdateAsync(Customer customer)
    // {
    //     var response = new UpdateCustomerResponse();

    //     if (customer.Id <= 0)
    //     {
    //         response.ResponseMessage = "Invalid customer id. Update failed.";
    //         response.ResponseCode = 400;
    //         response.RecordCount = 0;
    //         return response;
    //     }

    //     try
    //     {
    //         await _db.ExecuteNonQuery(QueryGenerator.Update(), cmd =>
    //         {
    //             cmd.Parameters.AddWithValue("@Id", customer.Id);
    //             cmd.Parameters.AddWithValue("@Name", customer.Name);
    //             cmd.Parameters.AddWithValue("@Contact", customer.Contact);
    //             cmd.Parameters.AddWithValue("@City", customer.City);
    //             cmd.Parameters.AddWithValue("@Email", customer.Email);
    //         });

    //         response.ResponseMessage = "Customer updated successfully.";
    //         response.ResponseCode = 200;
    //         response.RecordCount = 1;
    //     }
    //     catch (Exception)
    //     {
    //         response.ResponseMessage = "An error occurred while updating the customer.";
    //         response.ResponseCode = 500;
    //         response.RecordCount = 0;
    //     }

    //     return response;
    // }

    public async Task DeleteAsync(int id)
    {
        try
        {
            // 🔹 Step 1: Validate Id
            if (id <= 0)
            {
                _logger.LogWarning("Invalid CustomerId provided for delete: {Id}", id);
                throw new ValidationException("Valid customer Id is required.");
            }

            // 🔹 Step 2: Check existence
            bool exists = await CustomerExistsAsync(id);
            if (!exists)
            {
                _logger.LogWarning("Delete failed. Customer not found. Id: {Id}", id);
                throw new KeyNotFoundException(
                    $"Customer with Id {id} does not exist.");
            }

            // 🔹 Step 3: Delete
            var rowsAffected = await _db.ExecuteNonQuery(
                QueryGenerator.Delete(),
                cmd =>
                {
                    cmd.Parameters.AddWithValue("@Id", id);
                });

            if (rowsAffected == 0)
            {
                _logger.LogError(
                    "Delete failed unexpectedly. CustomerId: {Id}",
                    id
                );
                throw new ApplicationException("Customer deletion failed.");
            }
             _logger.LogInformation("Customer deleted successfully. Id: {Id}", id);
        }
        catch (Exception)
        {
            _logger.LogError("Unhandled error in DeleteAsync");
            throw;
        }
        finally
        {
            _db.CloseSqlConnection();   // ✅ close connection
        }
    }

    private static Customer MapCustomer(MySqlDataReader reader)
    {
        return new Customer
        {
            Id = SQLHelper.GetIntValue(reader, "Id"),
            Name = SQLHelper.GetStringValue(reader, "Name"),
            Contact = SQLHelper.GetStringValue(reader, "Contact"),
            City = SQLHelper.GetStringValue(reader, "City"),
            Email = SQLHelper.GetStringValue(reader, "Email")
        };
    }

    private void ValidateCustomer(Customer customer)
    {
        if (customer == null)
            throw new ValidationException("Customer data is required.");

        if (string.IsNullOrWhiteSpace(customer.Name))
            throw new ValidationException("Customer name is required.");

        if (customer.Name.Length < 3)
            throw new ValidationException("Customer name must be at least 3 characters.");

        if (string.IsNullOrWhiteSpace(customer.Contact))
            throw new ValidationException("Contact number is required.");

        if (!customer.Contact.All(char.IsDigit))
            throw new ValidationException("Contact must contain only digits.");

        if (customer.Contact.Length < 10)
            throw new ValidationException("Contact must be at least 10 digits.");

        if (string.IsNullOrWhiteSpace(customer.City))
            throw new ValidationException("City is required.");

        if (string.IsNullOrWhiteSpace(customer.Email))
            throw new ValidationException("Email is required.");

        if (!IsValidEmail(customer.Email))
            throw new ValidationException("Invalid email format.");
    }

    private bool IsValidEmail(string email)
    {
        try
        {
            var addr = new System.Net.Mail.MailAddress(email);
            return addr.Address == email;
        }
        catch
        {
            return false;
        }
    }

    private async Task<bool> CustomerExistsAsync(int customerId)
    {
        try
        {
            using var reader = await _db.ExecuteQueryAsync(
                QueryGenerator.GetById(),
                cmd =>
                {
                    cmd.Parameters.AddWithValue("@Id", customerId);
                });

            return await reader.ReadAsync(); // true if record exists
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error while checking customer existence. Id: {Id}",
                customerId
            );
            throw;
        }
        finally
        {
            _db.CloseSqlConnection();   // ✅ close connection
        }
    }

}