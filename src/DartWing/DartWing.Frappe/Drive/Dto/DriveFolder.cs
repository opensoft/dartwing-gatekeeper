namespace DartWing.Frappe.Drive.Dto;

public sealed class DriveFolder
{
    
}

public sealed class CreateDriveFolderRequest
{
    public string Parent { get; set; }
    public string Title { get; set; }
}