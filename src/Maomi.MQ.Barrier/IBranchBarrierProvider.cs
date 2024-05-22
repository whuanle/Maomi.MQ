// <copyright file="Class1.cs" company="Maomi">
// Copyright (c) Maomi. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// Github link: https://github.com/whuanle/Maomi.MQ
// </copyright>

namespace Maomi.MQ.Barrier;

public interface IBranchBarrierProvider
{
    BranchBarrier CreateBranchBarrier(string transType, string gid, string branchID, string op);
}