using System;

namespace JsonRepairUtils;

/// <summary>
/// Represents an error that occurs during JSON repair.
/// </summary>
public class JsonRepairError : Exception
{
    /// <summary>
    /// The index at which the error occurred.
    /// </summary>
    public int Index { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="JsonRepairError"/> class with a specified error message and index.
    /// </summary>
    /// <param name="message">The error message that explains the reason for the exception.</param>
    /// <param name="index">The index at which the error occurred.</param>
    public JsonRepairError(string message, int index) : base(message)
    {
        Index = index;
    }
}
