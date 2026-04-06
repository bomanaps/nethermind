// SPDX-FileCopyrightText: 2022 Demerzel Solutions Limited
// SPDX-License-Identifier: LGPL-3.0-only

using System.Collections.Generic;
using System.Linq;
using Nethermind.Core;
using Nethermind.Facade.Eth;
using Nethermind.Facade.Eth.RpcTransaction;
using Nethermind.TxPool;

namespace Nethermind.JsonRpc.Modules.TxPool
{
    public class TxPoolContentFrom
    {
        private static readonly Dictionary<ulong, TransactionForRpc> _emptyDictionary = [];

        public TxPoolContentFrom(TxPoolInfo info, ulong chainId, Address address)
        {
            TransactionForRpcContext extraData = new(chainId);
            Pending = info.Pending.TryGetValue(address, out var pending)
                ? pending.ToDictionary(v => v.Key, v => TransactionForRpc.FromTransaction(v.Value, extraData))
                : _emptyDictionary;
            Queued = info.Queued.TryGetValue(address, out var queued)
                ? queued.ToDictionary(v => v.Key, v => TransactionForRpc.FromTransaction(v.Value, extraData))
                : _emptyDictionary;
        }

        public Dictionary<ulong, TransactionForRpc> Pending { get; set; }
        public Dictionary<ulong, TransactionForRpc> Queued { get; set; }
    }
}
