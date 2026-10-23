namespace System.Data;

public class RowNotUniqueException : DataException
{
    public override string Message => "Obtained row is not unique.";
}
