using System.ComponentModel.DataAnnotations;
namespace WebApplication1.Models;

public class Client
{
    public int IdClient { get; set; }

    [Required]
    public string FirstName { get; set; }

    [Required]
    public string LastName { get; set; }

    [Required, EmailAddress]
    public string Email { get; set; }

    [Required]
    public string Telephone { get; set; }

    [Required, StringLength(11, MinimumLength = 11)]
    public string Pesel { get; set; }
}