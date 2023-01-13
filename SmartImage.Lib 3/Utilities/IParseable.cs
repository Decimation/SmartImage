namespace SmartImage.Lib.Utilities;

public interface IParseable<out TResult, in TSource>
{
    public static abstract TResult Parse(TSource t);
}