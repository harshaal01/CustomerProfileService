using Microsoft.AspNetCore.Mvc;
using CustomerProfileService.Domain.Entities;
using CustomerProfileService.Domain.Interfaces;
using MySql.Data.MySqlClient;
using System.Net;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;

namespace CustomerProfileService.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class CustomersController : ControllerBase
{
    private readonly ICustomerService _service;
    

    public CustomersController(ICustomerService service)
    {
        _service = service;
    }

    [HttpGet]
    [Route("GetAllCustomers")]
    public async Task<IActionResult> Get()
    {
        try
        {
            var customers = await _service.GetAllAsync();

            if (!customers.Any())
                return NoContent(); // 204

            return Ok(customers); // 200
        }
        catch (ValidationException ex)
        {
            return BadRequest(ex.Message); // 400
        }
        catch (ApplicationException ex)
        {
            return StatusCode(500, ex.Message); // 500
        }
    }

    [HttpPost]
    [Route("AddCustomer")]
    public async Task<IActionResult> Create([FromBody] Customer customer)
    {
        try
        {
            await _service.CreateAsync(customer);

            return Ok(new
            {
                message = "Customer created successfully"
            });
        }
        catch (ValidationException ex)
        {
            return BadRequest(ex.Message); // 400
        }
        catch (ApplicationException ex)
        {
            return StatusCode(500, ex.Message); // 500
        }
    }

    [HttpPost]
    [Route("UpdateCustomer")]
    public async Task<IActionResult> Update([FromBody] Customer customer)
    {
        try
        {
            await _service.UpdateAsync(customer);

            return Ok(new
            {
                message = "Customer updated successfully"
            });
        }
        catch (ValidationException ex)
        {
            return BadRequest(ex.Message); // 400
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ex.Message); // 404
        }
        catch (ApplicationException ex)
        {
            return StatusCode(500, ex.Message); // 500
        }
    }

    [HttpDelete("DeleteCustomer/{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        try
        {
            await _service.DeleteAsync(id);

            return Ok(new
            {
                message = "Customer deleted successfully"
            });
        }
        catch (ValidationException ex)
        {
            return BadRequest(ex.Message); // 400
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ex.Message); // 404
        }
        catch (ApplicationException ex)
        {
            return StatusCode(500, ex.Message); // 500
        }
    }
}
