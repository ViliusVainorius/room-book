namespace RoomBook.Domain.Models;

public class Room
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Capacity { get; set; }
    public string Floor { get; set; } = string.Empty;
    public List<EquipmentType> Equipment { get; set; } = [];
    public bool IsActive { get; set; } = true;


    public ICollection<Reservation> Reservations { get; set; } = [];
}