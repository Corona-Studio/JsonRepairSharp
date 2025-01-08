namespace JsonRepairSharp.Class;

public class JsonRepairError(string message, int position) : Exception($"{message} at position {position}")
{
    public int Position { get; } = position;
}