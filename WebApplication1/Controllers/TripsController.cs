using Microsoft.AspNetCore.Mvc;
using WebApplication1.Services;

namespace WebApplication1.Controllers;

[ApiController]
[Route("api/trips")]
public class TripsController : ControllerBase
{
    private readonly DbService _dbService;
    
    public TripsController(DbService dbService)
    {
        _dbService = dbService;
    }
    
    // Zwraca wszystkie dostępne wycieczki z bd wraz z przypisanymi do nich krajami
    [HttpGet]
    public async Task<IActionResult> GetTrips()
    {
        var trips = await _dbService.GetAllTripsAsync();
        return Ok(trips);
    }
}