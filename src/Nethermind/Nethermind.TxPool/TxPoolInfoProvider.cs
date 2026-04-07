// SPDX-FileCopyrightText: 2022 Demerzel Solutions Limited
// SPDX-License-Identifier: LGPL-3.0-only

using System;
using System.Collections.Generic;
using System.Linq;
using Nethermind.Core;
using Nethermind.Int256;

namespace Nethermind.TxPool
{
    public class TxPoolInfoProvider(IAccountStateProvider accountStateProvider, ITxPool txPool) : ITxPoolInfoProvider
    {
        private static readonly Dictionary<AddressAsKey, IDictionary<ulong, Transaction>> _emptyAddressMap = [];

        public TxPoolInfoProvider(IChainHeadInfoProvider chainHeadInfoProvider, ITxPool txPool) : this(chainHeadInfoProvider.ReadOnlyStateProvider, txPool) { }

        public TxPoolInfo GetInfo(Address address)
        {
            Transaction[] transactions = txPool.GetPendingTransactionsBySender(address);

            if (transactions.Length == 0)
                return new TxPoolInfo([], []);

            Array.Sort(transactions, static (a, b) => a.Nonce.CompareTo(b.Nonce));

            UInt256 accountNonce = accountStateProvider.GetNonce(address);
            UInt256 expectedNonce = accountNonce;
            Dictionary<ulong, Transaction>? pending = null;
            Dictionary<ulong, Transaction>? queued = null;

            foreach (Transaction transaction in transactions)
            {
                ulong transactionNonce = (ulong)transaction.Nonce;
                if (transaction.Nonce == expectedNonce)
                {
                    (pending ??= new()).Add(transactionNonce, transaction);
                    expectedNonce = transaction.Nonce + 1;
                }
                else
                {
                    (queued ??= new()).Add(transactionNonce, transaction);
                }
            }

            Dictionary<AddressAsKey, IDictionary<ulong, Transaction>> pendingTransactions = pending is not null
                ? new(1) { [address] = pending }
                : _emptyAddressMap;
            Dictionary<AddressAsKey, IDictionary<ulong, Transaction>> queuedTransactions = queued is not null
                ? new(1) { [address] = queued }
                : _emptyAddressMap;

            return new TxPoolInfo(pendingTransactions, queuedTransactions);
        }

        public TxPoolInfo GetInfo()
        {
            // only std txs are picked here. Should we add blobs?
            // BTW this class should be rewritten or removed - a lot of unnecessary allocations
            var groupedTransactions = txPool.GetPendingTransactionsBySender();
            var pendingTransactions = new Dictionary<AddressAsKey, IDictionary<ulong, Transaction>>();
            var queuedTransactions = new Dictionary<AddressAsKey, IDictionary<ulong, Transaction>>();
            foreach (KeyValuePair<AddressAsKey, Transaction[]> group in groupedTransactions)
            {
                Address address = group.Key;
                var accountNonce = accountStateProvider.GetNonce(address);
                var expectedNonce = accountNonce;
                var pending = new Dictionary<ulong, Transaction>();
                var queued = new Dictionary<ulong, Transaction>();
                var transactionsOrderedByNonce = group.Value.OrderBy(static t => t.Nonce);

                foreach (var transaction in transactionsOrderedByNonce)
                {
                    ulong transactionNonce = (ulong)transaction.Nonce;
                    if (transaction.Nonce == expectedNonce)
                    {
                        pending.Add(transactionNonce, transaction);
                        expectedNonce = transaction.Nonce + 1;
                    }
                    else
                    {
                        queued.Add(transactionNonce, transaction);
                    }
                }

                if (pending.Count != 0)
                {
                    pendingTransactions[address] = pending;
                }

                if (queued.Count != 0)
                {
                    queuedTransactions[address] = queued;
                }
            }

            return new TxPoolInfo(pendingTransactions, queuedTransactions);
        }
    }
}
