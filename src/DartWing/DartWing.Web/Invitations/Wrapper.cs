namespace DartWing.Web.Invitations;

public class Wrapper<T>(List<T> data) where T : class
{
    public List<T> Data { get; set; } = data;
    public int? Page { get; set; }
    public int? PageSize { get; set; }

    public static Wrapper<T> Create(List<T> data, int? page = null, int? pageSize = null)
    {
        var result = new Wrapper<T>(data)
        {
            Page = page,
            PageSize = pageSize
        };
        return result;
    }
}