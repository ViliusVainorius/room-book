using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RoomBook.Api.DTOs;
using RoomBook.Domain.Models;
using RoomBook.Infrastructure.Context;

namespace RoomBook.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UsersController(RoomBookDbContext context) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IEnumerable<UserDto>>> GetUsers()
    {
        var users = await context.Users
            .Select(user => new UserDto(
                user.Id,
                user.FirstName,
                user.LastName,
                user.Reservations.Count(
                    reservation =>
                        reservation.Status == ReservationStatus.Booked)
            ))
            .ToListAsync();

        return Ok(users);
    }
}
