namespace EquipmentSolver.Api.DTOs;

public class ErrorResponse
{
    public List<string> Errors { get; set; } = [];

    public ErrorResponse(params string[] errors)
    {
        Errors = [.. errors];
    }
}
