using System;
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

        public ICache<Guid, Article> Articles { get; set; }
        public ICache<Guid, Comment> Comments { get; set; }
        public ICache<Guid, Person> Persons { get; set; }
        public ICache<string, byte> Tags { get; set; }
        public ICache<(Guid, string), byte> ArticleTags { get; set; }
        public ICache<(Guid, Guid), byte> ArticleFavorites { get; set; }
        public ICache<(Guid, Guid), byte> FollowedPeople { get; set; }

        public void EnsureCreated()
        {
            CreateModel(null);
        }

        private void CreateModel(ModelBuilder modelBuilder)
        {
            // TODO: Create caches and SQL indexes.
            Articles = _ignite.GetOrCreateCache<Guid, Article>(nameof(Articles));
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
