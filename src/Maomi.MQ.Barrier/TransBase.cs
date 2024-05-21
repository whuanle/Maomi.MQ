// <copyright file="TransBase.cs" company="Maomi">
// Copyright (c) Maomi. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// Github link: https://github.com/whuanle/Maomi.MQ
// </copyright>

using System.Text.Json.Serialization;

namespace Maomi.MQ.Barrier;

public class DBType
{
    public const string Mysql = "Mysql";
    public const string Postgres = "Postgres";
}

/// <summary>
/// BranchIDGen used to generate a sub branch id.
/// </summary>
public class BranchIDGen
{
    public string BranchID { get; set; }

    public int SubBranchID { get; private set; }

    public string NewSubBranchID()
    {
        if (SubBranchID >= 99)
        {
            throw new ArgumentOutOfRangeException($"Branch id({SubBranchID}) is larger than 99");
        }
        if (BranchID.Length >= 20)
        {
            throw new ArgumentOutOfRangeException($"total branch id(BranchID) is longer than 20");
        }
        SubBranchID++;
        return CurrentSubBranchID();
    }

    /// <summary>
    /// CurrentSubBranchID return current branchID.
    /// </summary>
    /// <returns></returns>
    public string CurrentSubBranchID()
    {
        return BranchID + string.Format("{0:00}", SubBranchID);
    }

}

public class TransOptions
{
    [JsonPropertyName("wait_result")]
    public bool WaitResult { get; set; }

    /// <summary>
    ///  for trans type: xa, tcc, unit: second.
    /// </summary>
    [JsonPropertyName("timeout_to_fail")]
    public long TimeoutToFail { get; set; }

    /// <summary>
    /// for global trans resets request timeout, unit: second.
    /// </summary>
    [JsonPropertyName("request_timeout")]
    public long RequestTimeout { get; set; }

    /// <summary>
    /// for trans type: msg saga xa tcc, unit: second.
    /// </summary>
    [JsonPropertyName("retry_interval")]
    public long RetryInterval { get; set; }

    /// <summary>
    /// custom branch headers,  dtm server => service api.
    /// </summary>
    [JsonPropertyName("branch_headers")]
    public Dictionary<string, string> BranchHeaders { get; set; }

    [JsonPropertyName("concurrent")]
    public bool Concurrent { get; set; }

    [JsonPropertyName("retry_limit")]
    public long RetryLimit { get; set; }

    [JsonPropertyName("retry_count")]
    public long RetryCount { get; set; }
}

public class TransBase : TransOptions
{
    [JsonPropertyName("gid")]
    public string Gid { get; set; } //  NOTE: unique in storage, can customize the generation rules instead of using server-side generation, it will help with the tracking

    [JsonPropertyName("trans_type")]
    public string TransType { get; set; }

    [JsonIgnore]
    public string Dtm { get; set; }

    [JsonPropertyName("custom_data,omitempty")]
    public string CustomData { get; set; } // nosql data persistence

    //[JsonIgnore]
    //public Context Context { get; set; }

    [JsonPropertyName("steps,omitempty")]
    public List<Dictionary<string, string>> Steps { get; set; }    // use in MSG/SAGA

    [JsonPropertyName("payloads,omitempty")]
    public List<string> Payloads { get; set; } // used in MSG/SAGA

    [JsonIgnore]
    public List<byte[]> BinPayloads { get; set; }

    [JsonIgnore]  // Assuming BranchIDGen is a custom class
    public BranchIDGen BranchIDGen { get; set; }  // used in XA/TCC

    public string Op { get; set; }              // used in XA/TCC

    [JsonPropertyName("query_prepared")]
    public string QueryPrepared { get; set; } // used in MSG

    public string Protocol { get; set; }

    [JsonPropertyName("rollback_reason")]
    public string RollbackReason { get; set; }

    public static TransBase NewTransBase(string gid, string transType, string dtm, string branchID)
    {
        return new TransBase
        {
            Gid = gid,
            TransType = transType,
            BranchIDGen = new BranchIDGen { BranchID = branchID },
            Dtm = dtm,
            //Context: context.Background(),
        };
    }

    // WithGlobalTransRequestTimeout defines global trans request timeout
    public void WithGlobalTransRequestTimeout(long timeout)
    {
        RequestTimeout = timeout;
    }

    // WithRetryLimit defines global trans retry limit
    public void WithRetryLimit(long retryLimit)
    {
        RetryLimit = retryLimit;
    }

    public static TransBase TransBaseFromQuery(string gid, string transType, string dtm, string branchId)
    {
        return NewTransBase(gid, transType, dtm, branchId);
    }


}
