namespace RouterQuack.IO.Gns3.Exceptions;

/// <summary>
/// Base exception for GNS3-related errors.
/// </summary>
public class Gns3Exception : Exception
{
    public Gns3Exception(string message) : base(message)
    {
    }

    public Gns3Exception(string message, Exception innerException) : base(message, innerException)
    {
    }
}

/// <summary>
/// Exception thrown when unable to connect to GNS3 server.
/// </summary>
public class Gns3ConnectionException : Gns3Exception
{
    public Gns3ConnectionException(string message) : base(message)
    {
    }

    public Gns3ConnectionException(string message, Exception innerException) : base(message, innerException)
    {
    }
}