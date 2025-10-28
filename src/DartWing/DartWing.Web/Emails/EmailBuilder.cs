namespace DartWing.Web.Emails;

public static class EmailBuilder
{
    public static EmailMessage Invite(string email, string companyName, string invoiceCode)
    {
        ArgumentNullException.ThrowIfNull(email);
        ArgumentNullException.ThrowIfNull(companyName);
        ArgumentNullException.ThrowIfNull(invoiceCode);
        
        var emailMessage = new EmailMessage
        (
            EmailTemplates.Invite, email, new Dictionary<string, string>
            {
                {"company_name", companyName},
                {"invite_code", invoiceCode},
            }
        );

        return emailMessage;
    }   
    
    public static EmailMessage Added(string email, string companyName)
    {
        ArgumentNullException.ThrowIfNull(email);
        ArgumentNullException.ThrowIfNull(companyName);
        
        var emailMessage = new EmailMessage
        (
            EmailTemplates.Added, email, new Dictionary<string, string>
            {
                {"company_name", companyName},
            }
        );

        return emailMessage;
    }
    
    public static EmailMessage ConnectionLink(string email, string companyName, string link)
    {
        ArgumentNullException.ThrowIfNull(link);
        
        var emailMessage = new EmailMessage
        (
            EmailTemplates.ConnectionLink, email, new Dictionary<string, string>
            {
                {"company_name", companyName},
                {"link", link},
            }
        );

        return emailMessage;
    }
}

public record EmailMessage(string TransactionalId, string Email, Dictionary<string, string> DataVariables);

public static class EmailTemplates
{
    public const string Invite = "cm205bm5e01d93m0f8wt44gda";
    public const string Added = "cm915yyb41po4bgjceulfol5s";
    public const string ConnectionLink = "cm2k9ixub00fd10m69v87k0vk";
}