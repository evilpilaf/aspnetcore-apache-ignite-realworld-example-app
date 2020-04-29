using System.Linq;
using System.Threading.Tasks;
using Apache.Ignite.Linq;
using Conduit.Features.Users;
using Conduit.Infrastructure.Security;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Conduit.IntegrationTests.Features.Users
{
    public class CreateTests : SliceFixture
    {
        [Fact]
        public async Task Expect_Create_User()
        {
            var command = new Create.Command()
            {
                User = new Create.UserData()
                {
                    Email = "email",
                    Password = "password",
                    Username = "username"
                }
            };

            await SendAsync(command);

            var created = await ExecuteDbContextAsync(db => db.Persons.AsCacheQueryable().Where(d => d.Value.Email == command.User.Email).SingleOrDefaultAsync());

            Assert.NotNull(created);
            Assert.Equal(created.Value.Hash, new PasswordHasher().Hash("password", created.Value.Salt));
        }
    }
}