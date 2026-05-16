using System.Security.Cryptography;

using RaftSimulator.Abstractions;

namespace RaftSimulator.Logic;

/// <summary>
/// Cryptographic random source for raft timing jitter.
/// </summary>
internal sealed class CryptoRaftRandom : IRaftRandom
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CryptoRaftRandom"/> class.
    /// </summary>
    public CryptoRaftRandom()
    {
    }

    /// <inheritdoc />
    public double NextDouble()
    {
        var value = RandomNumberGenerator.GetInt32(int.MaxValue);
        return value / (double)int.MaxValue;
    }
}
