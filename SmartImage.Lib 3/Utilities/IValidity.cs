namespace SmartImage.Lib.Utilities;

public interface IValidity<in T>
{
    public static abstract bool IsValid([CBN] T value);
}