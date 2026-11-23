namespace CairnMpModExample;

/// <summary>
/// Wire values of a cheer. Append new kinds at the end: peers read the number, not
/// the name.
/// </summary>
public enum CheerKind
{
    NiceMove = 0,
    TakeABreak = 1,
    Summit = 2,
}

/// <summary>
/// What a player asks the host to publish. Payloads go through System.Text.Json, so a
/// payload is a public class with get/set properties.
/// </summary>
public sealed class CheerRequest
{
    public CheerKind Kind { get; set; }
}
