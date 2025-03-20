using System.Text.Json.Serialization;

namespace DartWing.ErpNext.Dto;

public sealed class UserPermissionsDto
{
    public UserPermission[] Data { get; set; }
    
    [JsonPropertyName("_server_messages")]
    public string ServerMessages { get; set; }
}

public sealed class UserPermissionDto
{
    public UserPermission Data { get; set; }
    
    [JsonPropertyName("_server_messages")]
    public string ServerMessages { get; set; }
}

public sealed class UserPermission
{
    public string Name { get; set; }
    public string Owner { get; set; }
    public DateTime Creation { get; set; }
    public DateTime Modified { get; set; }
    public string ModifiedBy { get; set; }
    public int Docstatus { get; set; }
    public int Idx { get; set; }
    public string User { get; set; }
    public string Allow { get; set; }
    public string ForValue { get; set; }
    public int IsDefault { get; set; }
    public int ApplyToAllDoctypes { get; set; }
    public int HideDescendants { get; set; }
    public string Doctype { get; set; }
}