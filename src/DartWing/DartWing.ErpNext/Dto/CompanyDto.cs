namespace DartWing.ErpNext.Dto;

public sealed class CompanyDto
{
    public string Name { get; set; } // Company Name
    public string Abbr { get; set; } // Abbreviation
    public string DefaultCurrency { get; set; } // Default Currency
    public string Domain { get; set; } // Business Domain
    public bool IsEnabled { get; set; } = true; // Status
}

public sealed class CreateCompanyDto
{
    public required string CompanyName { get; set; }
    public required string Abbr { get; set; }
    public required string DefaultCurrency { get; set; }
    public string Domain { get; set; }
}

public sealed class UpdateCompanyDto
{
    public string? DefaultCurrency { get; set; }
    public string? Domain { get; set; }
    public bool? IsEnabled { get; set; }
}

public sealed class DeleteCompanyDto
{
    public required string CompanyName { get; set; }
}

public sealed class CompanyResponseDto<T>
{
    public bool Success { get; set; }
    public string Message { get; set; }
    public T? Data { get; set; }
}