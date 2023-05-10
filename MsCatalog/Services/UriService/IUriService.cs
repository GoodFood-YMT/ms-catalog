using MsCatalog.Models.Filters;

namespace MsCatalog.Services.UriService
{
    public interface IUriService
    {
        public Uri GetPageUri(PaginationFilter filter, string route);
    }
}
