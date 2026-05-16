namespace AsaSavegameToolkit.Plumbing;

[Serializable]
internal class AsaDataException : Exception
{
    public AsaDataException()
    {
    }

    public AsaDataException(string? message) : base(message)
    {
    }

    public AsaDataException(string? message, Exception? innerException) : base(message, innerException)
    {
    }
}