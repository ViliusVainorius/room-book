using RoomBook.Domain.Models;

namespace RoomBook.Api.DTOs;

public record RoomDto(
    int Id,
    string Name,
    int Capacity,
    string Floor,
    List<EquipmentType> Equipment,
    bool IsActive
);
