using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Apache.Ignite.Linq;
using Conduit.Domain;
using Conduit.Infrastructure;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Conduit.Features.Articles
{
    public class Create
    {
        public class ArticleData
        {
            public string Title { get; set; }

            public string Description { get; set; }

            public string Body { get; set; }

            public string[] TagList { get; set; }
        }

        public class ArticleDataValidator : AbstractValidator<ArticleData>
        {
            public ArticleDataValidator()
            {
                RuleFor(x => x.Title).NotNull().NotEmpty();
                RuleFor(x => x.Description).NotNull().NotEmpty();
                RuleFor(x => x.Body).NotNull().NotEmpty();
            }
        }

        public class Command : IRequest<ArticleEnvelope>
        {
            public ArticleData Article { get; set; }
        }

        public class CommandValidator : AbstractValidator<Command>
        {
            public CommandValidator()
            {
                RuleFor(x => x.Article).NotNull().SetValidator(new ArticleDataValidator());
            }
        }

        public class Handler : IRequestHandler<Command, ArticleEnvelope>
        {
            private readonly ConduitContext _context;
            private readonly ICurrentUserAccessor _currentUserAccessor;

            public Handler(ConduitContext context, ICurrentUserAccessor currentUserAccessor)
            {
                _context = context;
                _currentUserAccessor = currentUserAccessor;
            }

            public async Task<ArticleEnvelope> Handle(Command message, CancellationToken cancellationToken)
            {
                var authorId = await _context.Persons.AsCacheQueryable()
                    .Where(x => x.Value.Username == _currentUserAccessor.GetCurrentUsername())
                    .Select(x => x.Key)
                    .FirstAsync(cancellationToken);
                
                var article = new Article
                {
                    ArticleId = Guid.NewGuid(),
                    AuthorId = authorId,
                    Body = message.Article.Body,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    Description = message.Article.Description,
                    Title = message.Article.Title,
                    Slug = message.Article.Title.GenerateSlug()
                };
                
                await _context.Articles.PutAsync(article.ArticleId, article);

                var tags = message.Article.TagList;
                if (tags != null)
                {
                    await _context.Tags.PutAllAsync(tags.Select(t =>
                        new KeyValuePair<string, Tag>(t, new Tag {TagId = t})));

                    await _context.ArticleTags.PutAllAsync(tags.Select(t =>
                        new KeyValuePair<(Guid, string), byte>((article.ArticleId, t), default)));
                }

                return new ArticleEnvelope(article);
            }
        }
    }
}
