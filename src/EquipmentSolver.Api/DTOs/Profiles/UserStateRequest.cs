using System.ComponentModel.DataAnnotations;

namespace EquipmentSolver.Api.DTOs.Profiles;

public class SetEquipmentStateRequest
{
    [Required]
    public int EquipmentId { get; set; }

    [Required]
    public bool IsEnabled { get; set; }
}

public class BulkEquipmentStateRequest
{
    [Required]
    public bool IsEnabled { get; set; }
}

public class SetSlotStateRequest
{
    [Required]
    public int SlotId { get; set; }

    [Required]
    public bool IsEnabled { get; set; }
}

public class UserEquipmentStateResponse
{
    public int EquipmentId { get; set; }
    public bool IsEnabled { get; set; }
}

public class UserSlotStateResponse
{
    public int SlotId { get; set; }
    public bool IsEnabled { get; set; }
}
