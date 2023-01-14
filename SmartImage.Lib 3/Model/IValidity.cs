namespace SmartImage.Lib.Model;

public interface IValidity<in T>
{
    public static abstract bool IsValid([CBN] T value);
}