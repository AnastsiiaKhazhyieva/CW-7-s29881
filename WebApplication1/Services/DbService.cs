using WebApplication1.Models;
using System.Data.SqlClient;

namespace WebApplication1.Services;

public class DbService
{
    private readonly IConfiguration _configuration;
    private readonly string _connectionString;

    public DbService(IConfiguration configuration)
    {
        _configuration = configuration;
        _connectionString = _configuration.GetConnectionString("Default")!;
    }

    // Pobiera wszystkie wycieczki i dołącza do nich listę krajów
    public async Task<List<TripWithCountries>> GetAllTripsAsync()
    {
        var trips = new List<TripWithCountries>();

        using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();

        var tripCmd = new SqlCommand("SELECT * FROM Trip", connection);
        using var tripReader = await tripCmd.ExecuteReaderAsync();

        while (await tripReader.ReadAsync())
        {
            trips.Add(new TripWithCountries
            {
                IdTrip = (int)tripReader["IdTrip"],
                Name = tripReader["Name"].ToString(),
                Description = tripReader["Description"].ToString(),
                DateFrom = (DateTime)tripReader["DateFrom"],
                DateTo = (DateTime)tripReader["DateTo"],
                MaxPeople = (int)tripReader["MaxPeople"]
            });
        }
        tripReader.Close();
        
        foreach (var trip in trips)
        {
            var countryCmd = new SqlCommand(@"
            SELECT c.Name
            FROM Country_Trip ct
            JOIN Country c ON c.IdCountry = ct.IdCountry
            WHERE ct.IdTrip = @IdTrip", connection);

            countryCmd.Parameters.AddWithValue("@IdTrip", trip.IdTrip);

            using var countryReader = await countryCmd.ExecuteReaderAsync();
            while (await countryReader.ReadAsync())
            {
                trip.Countries.Add(countryReader["Name"].ToString()!);
            }
            countryReader.Close();
        }

        return trips;
    }
    
    // Dodaje klienta do bazy danych i nadaje mu nowe ID
    public async Task<int> AddClientAsync(Client client)
    {
        using var connection = new SqlConnection(_connectionString);
        using var command = new SqlCommand(@"
                INSERT INTO Client (FirstName, LastName, Email, Telephone, Pesel)
                OUTPUT INSERTED.IdClient
                VALUES (@FirstName, @LastName, @Email, @Telephone, @Pesel)", connection);

        command.Parameters.AddWithValue("@FirstName", client.FirstName);
        command.Parameters.AddWithValue("@LastName", client.LastName);
        command.Parameters.AddWithValue("@Email", client.Email);
        command.Parameters.AddWithValue("@Telephone", client.Telephone);
        command.Parameters.AddWithValue("@Pesel", client.Pesel);

        await connection.OpenAsync();
        return (int)await command.ExecuteScalarAsync();
    }
    
    private int GetTodayInt()
    {
        return int.Parse(DateTime.Now.ToString("yyyyMMdd"));
    }
    
    // Pobiera listę wycieczek, na które zapisany jest podany klient
    public async Task<IEnumerable<object>?> GetClientTripsAsync(int clientId)
    {
        using var connection = new SqlConnection(_connectionString);
        var query = @"
                SELECT t.IdTrip, t.Name, t.Description, t.DateFrom, t.DateTo, t.MaxPeople,
                       ct.RegisteredAt, ct.PaymentDate
                FROM Client_Trip ct
                JOIN Trip t ON t.IdTrip = ct.IdTrip
                WHERE ct.IdClient = @IdClient";

        using var command = new SqlCommand(query, connection);
        command.Parameters.AddWithValue("@IdClient", clientId);

        await connection.OpenAsync();
        using var reader = await command.ExecuteReaderAsync();

        if (!reader.HasRows) return null;

        var results = new List<object>();
        while (await reader.ReadAsync())
        {
            results.Add(new
            {
                IdTrip = (int)reader["IdTrip"],
                Name = reader["Name"].ToString(),
                Description = reader["Description"].ToString(),
                DateFrom = (DateTime)reader["DateFrom"],
                DateTo = (DateTime)reader["DateTo"],
                MaxPeople = (int)reader["MaxPeople"],
                RegisteredAt = (int)reader["RegisteredAt"],
                PaymentDate = reader["PaymentDate"] != DBNull.Value ? (int?)reader["PaymentDate"] : null
            });
        }

        return results;
    }

    // Rejestruje klienta na daną wycieczkę, sprawdzając dostępność miejsc
    public async Task<(bool Success, string Message)> RegisterClientToTripAsync(int clientId, int tripId)
    {
        using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();

        var checkTrip = new SqlCommand("SELECT MaxPeople FROM Trip WHERE IdTrip = @TripId", connection);
        checkTrip.Parameters.AddWithValue("@TripId", tripId);
        var maxPeopleObj = await checkTrip.ExecuteScalarAsync();
        if (maxPeopleObj == null) return (false, "Wycieczka nie istnieje.");
        int maxPeople = (int)maxPeopleObj;

        var checkClient = new SqlCommand("SELECT COUNT(*) FROM Client WHERE IdClient = @ClientId", connection);
        checkClient.Parameters.AddWithValue("@ClientId", clientId);
        if ((int)await checkClient.ExecuteScalarAsync() == 0)
            return (false, "Klient nie istnieje.");

        var checkIfAlready =
            new SqlCommand("SELECT COUNT(*) FROM Client_Trip WHERE IdClient = @ClientId AND IdTrip = @TripId",
                connection);
        checkIfAlready.Parameters.AddWithValue("@ClientId", clientId);
        checkIfAlready.Parameters.AddWithValue("@TripId", tripId);
        if ((int)await checkIfAlready.ExecuteScalarAsync() > 0)
            return (false, "Klient już jest zapisany na tę wycieczkę.");

        var count = new SqlCommand("SELECT COUNT(*) FROM Client_Trip WHERE IdTrip = @TripId", connection);
        count.Parameters.AddWithValue("@TripId", tripId);
        int registered = (int)await count.ExecuteScalarAsync();
        if (registered >= maxPeople) return (false, "Brak miejsc na wycieczkę.");

        int nowInt = GetTodayInt();
        
        var insert = new SqlCommand(@"
        INSERT INTO Client_Trip (IdClient, IdTrip, RegisteredAt)
        VALUES (@ClientId, @TripId, @RegisteredAt)", connection);

        insert.Parameters.AddWithValue("@ClientId", clientId);
        insert.Parameters.AddWithValue("@TripId", tripId);
        insert.Parameters.AddWithValue("@RegisteredAt", nowInt);
        await insert.ExecuteNonQueryAsync();

        return (true, "Klient został zapisany na wycieczkę.");
    }
    
    // Usuwa klienta z listy uczestników danej wycieczki
    public async Task<(bool Success, string Message)> UnregisterClientFromTripAsync(int clientId, int tripId)
    {
        using var connection = new SqlConnection(_connectionString);

        var check = new SqlCommand("SELECT COUNT(*) FROM Client_Trip WHERE IdClient = @ClientId AND IdTrip = @TripId",
            connection);
        check.Parameters.AddWithValue("@ClientId", clientId);
        check.Parameters.AddWithValue("@TripId", tripId);

        await connection.OpenAsync();
        if ((int)await check.ExecuteScalarAsync() == 0)
            return (false, "Klient nie jest zapisany na tę wycieczkę.");

        var delete = new SqlCommand("DELETE FROM Client_Trip WHERE IdClient = @ClientId AND IdTrip = @TripId",
            connection);
        delete.Parameters.AddWithValue("@ClientId", clientId);
        delete.Parameters.AddWithValue("@TripId", tripId);
        await delete.ExecuteNonQueryAsync();

        return (true, "Klient został wypisany z wycieczki.");
    }
}