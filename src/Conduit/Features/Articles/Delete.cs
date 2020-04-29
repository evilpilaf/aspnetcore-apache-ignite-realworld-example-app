using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Apache.Ignite.Linq;
using Conduit.Infrastructure;
using Conduit.Infrastructure.Errors;
using FluentValidation;
using MediatR;

namespace Conduit.Features.Articles
{
    public class Delete
    {
        public class Command : IRequest
        {
            public Command(string slug)
            {
                Slug = slug;
            }

            public string Slug { get; set; }
        }

        public class CommandValidator : AbstractValidator<Command>
        {
            public CommandValidator()
            {
                RuleFor(x => x.Slug).NotNull().NotEmpty();
            }
        }

        public class QueryHandler : IRequestHandler<Command>
        {
            private readonly ConduitContext _context;

            public QueryHandler(ConduitContext context)
            {
                _context = context;
            }

            public Task<Unit> Handle(Command message, CancellationToken cancellationToken)
            {
                var removedCount = _context.Articles.AsCacheQueryable()
                    .RemoveAll(a => a.Value.Slug == message.Slug);

                if (removedCount == 0)
                {
                    throw new RestException(HttpStatusCode.NotFound, new { Article = Constants.NOT_FOUND });
                }
                
                return Task.FromResult(Unit.Value);
            }
        }
    }
}