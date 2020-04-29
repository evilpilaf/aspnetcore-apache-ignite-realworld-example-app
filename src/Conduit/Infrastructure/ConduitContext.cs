using System.Collections.Generic;
using Conduit.Domain;
using Microsoft.EntityFrameworkCore;
using Apache.Ignite.Core;
using Apache.Ignite.Core.Cache;
using Apache.Ignite.Core.Discovery.Tcp;
using Apache.Ignite.Core.Discovery.Tcp.Static;
using Apache.Ignite.Core.Transactions;

namespace Conduit.Infrastructure
{
    public class ConduitContext
    {
        private ITransaction _currentTransaction;

        private readonly IIgnite _ignite;

        public ConduitContext()
        {
            _ignite = Ignition.TryGetIgnite() ?? Ignition.Start(new IgniteConfiguration
            {
                // TODO: Move config to startup.
                Localhost = "127.0.0.1",
                DiscoverySpi = new TcpDiscoverySpi
                {
                    IpFinder = new TcpDiscoveryStaticIpFinder
                    {
                        Endpoints = new[] {"127.0.0.1:47500"}
                    }
                }
            });
        }

        public ICache<int, Article> Articles { get; set; }
        public ICache<int, Comment> Comments { get; set; }
        public ICache<int, Person> Persons { get; set; }
        public ICache<string, Tag> Tags { get; set; }
        public ICache<(int, string), ArticleTag> ArticleTags { get; set; }
        public ICache<(int, int), ArticleFavorite> ArticleFavorites { get; set; }
        public ICache<(int, int), FollowedPeople> FollowedPeople { get; set; }

        public void EnsureCreated()
        {
            CreateModel(null);
        }

        private void CreateModel(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<ArticleTag>(b =>
            {
                b.HasKey(t => new { t.ArticleId, t.TagId });

                b.HasOne(pt => pt.Article)
                .WithMany(p => p.ArticleTags)
                .HasForeignKey(pt => pt.ArticleId);

                b.HasOne(pt => pt.Tag)
                .WithMany(t => t.ArticleTags)
                .HasForeignKey(pt => pt.TagId);
            });

            modelBuilder.Entity<ArticleFavorite>(b =>
            {
                b.HasKey(t => new { t.ArticleId, t.PersonId });

                b.HasOne(pt => pt.Article)
                    .WithMany(p => p.ArticleFavorites)
                    .HasForeignKey(pt => pt.ArticleId);

                b.HasOne(pt => pt.Person)
                    .WithMany(t => t.ArticleFavorites)
                    .HasForeignKey(pt => pt.PersonId);
            });

            modelBuilder.Entity<FollowedPeople>(b =>
            {
                b.HasKey(t => new { t.ObserverId, t.TargetId });

                // we need to add OnDelete RESTRICT otherwise for the SqlServer database provider, 
                // app.ApplicationServices.GetRequiredService<ConduitContext>().Database.EnsureCreated(); throws the following error:
                // System.Data.SqlClient.SqlException
                // HResult = 0x80131904
                // Message = Introducing FOREIGN KEY constraint 'FK_FollowedPeople_Persons_TargetId' on table 'FollowedPeople' may cause cycles or multiple cascade paths.Specify ON DELETE NO ACTION or ON UPDATE NO ACTION, or modify other FOREIGN KEY constraints.
                // Could not create constraint or index. See previous errors.
                b.HasOne(pt => pt.Observer)
                    .WithMany(p => p.Followers)
                    .HasForeignKey(pt => pt.ObserverId)
                    .OnDelete(DeleteBehavior.Restrict);

                // we need to add OnDelete RESTRICT otherwise for the SqlServer database provider, 
                // app.ApplicationServices.GetRequiredService<ConduitContext>().Database.EnsureCreated(); throws the following error:
                // System.Data.SqlClient.SqlException
                // HResult = 0x80131904
                // Message = Introducing FOREIGN KEY constraint 'FK_FollowingPeople_Persons_TargetId' on table 'FollowedPeople' may cause cycles or multiple cascade paths.Specify ON DELETE NO ACTION or ON UPDATE NO ACTION, or modify other FOREIGN KEY constraints.
                // Could not create constraint or index. See previous errors.
                b.HasOne(pt => pt.Target)
                    .WithMany(t => t.Following)
                    .HasForeignKey(pt => pt.TargetId)
                    .OnDelete(DeleteBehavior.Restrict);
            });
        }

        #region Transaction Handling
        public void BeginTransaction()
        {
            if (_currentTransaction != null)
            {
                return;
            }

            _currentTransaction = _ignite.GetTransactions().TxStart();
        }

        public void CommitTransaction()
        {
            try
            {
                _currentTransaction?.Commit();
            }
            catch
            {
                RollbackTransaction();
                throw;
            }
            finally
            {
                if (_currentTransaction != null)
                {
                    _currentTransaction.Dispose();
                    _currentTransaction = null;
                }
            }
        }

        public void RollbackTransaction()
        {
            try
            {
                _currentTransaction?.Rollback();
            }
            finally
            {
                if (_currentTransaction != null)
                {
                    _currentTransaction.Dispose();
                    _currentTransaction = null;
                }
            }
        }
        #endregion
    }
}
