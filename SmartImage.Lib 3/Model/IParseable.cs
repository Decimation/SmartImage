namespace SmartImage.Lib.Model;

public interface IParseable<out TResult, in TSource>
{
    public static abstract TResult Parse(TSource t);
}