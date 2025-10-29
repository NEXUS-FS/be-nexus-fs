namespace Application.UseCases.Users.Queries
{
    public class GetAllUsersQuery
    {
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }
}