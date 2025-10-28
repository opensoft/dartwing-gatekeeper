namespace DartWing.Frappe.Drive.Dto;

public sealed class DriveResponseDto<T> where T : class
{
    public bool Success { get; set; }
    public object Message { get; set; }
    public T? Data { get; set; }
}

public sealed class DriveMessageDto<T> where T : class
{
    public T Message { get; set; }
}