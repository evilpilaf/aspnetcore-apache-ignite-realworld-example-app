using System.Collections.Generic;

namespace Conduit.Domain
{
    // TODO: Tag is just a string, why bother with a class?
    public class Tag
    {
        public string TagId { get; set; }

        public List<ArticleTag> ArticleTags { get; set; }
    }
}