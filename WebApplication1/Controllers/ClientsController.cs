using Microsoft.AspNetCore.Mvc;
using WebApplication1.Models;
using WebApplication1.Services;

namespace WebApplication1.Controllers;

[ApiController]
[Route("api/clients")]
public class ClientsController : ControllerBase
{
    private readonly DbService _dbService;

    public ClientsController(DbService dbService)
    {
        _dbService = dbService;
    }

    // Tworzy nowego klienta i dodaje go do bazy danych
    [HttpPost]
    public async Task<IActionResult> AddClient([FromBody] Client client)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        if (client.Pesel.Length != 11)
            return BadRequest("PESEL musi mieć 11 znaków.");

        var id = await _dbService.AddClientAsync(client);
        client.IdClient = id;

        return CreatedAtAction(nameof(GetClientTrips), new { id }, client);
    }

    
    // Zwraca wszystkie wycieczki, na które zapisany jest dany klient
    [HttpGet("{id}/trips")]
    public async Task<IActionResult> GetClientTrips(int id)
    {
        var trips = await _dbService.GetClientTripsAsync(id);
        if (trips == null)
            return NotFound("Klient nie istnieje lub nie ma wycieczek.");
        
        return Ok(trips);
    }

    // Rejestruje klienta na konkretną wycieczkę
    [HttpPut("{id}/trips/{tripId}")]
    public async Task<IActionResult> RegisterClientToTrip(int id, int tripId)
    {
        var result = await _dbService.RegisterClientToTripAsync(id, tripId);
        if (!result.Success)
            return BadRequest(result.Message);
        
        return Ok(result.Message);
    }

    // Usuwa klienta z wycieczki
    [HttpDelete("{id}/trips/{tripId}")]
    public async Task<IActionResult> UnregisterClientFromTrip(int id, int tripId)
    {
        var result = await _dbService.UnregisterClientFromTripAsync(id, tripId);
        if (!result.Success)
            return NotFound(result.Message);
        
        return Ok(result.Message);
    }
}