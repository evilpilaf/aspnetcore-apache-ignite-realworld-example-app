using System.Linq;
using Conduit.Domain;
using Conduit.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace Conduit.Features.Articles
{
    public static class ArticleExtensions
    {
        public static IQueryable<Article> GetAllArticleData(this ConduitContext ctx)
        {
            return articles
                .Include(x => x.Author)
                .Include(x => x.ArticleFavorites)
                .Include(x => x.ArticleTags)
                .AsNoTracking();
        }
    }
}