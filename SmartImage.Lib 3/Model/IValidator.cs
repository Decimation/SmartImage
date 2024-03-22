namespace SmartImage.Lib.Model;

public interface IValidator<in T>
{
    public static abstract bool IsValid([CBN] T value);
}