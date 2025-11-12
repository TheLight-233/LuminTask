namespace LuminThread.Interface;

public delegate bool MoveNext(in LuminTaskState state);

public interface IPlayLoopItem
{
    bool MoveNext();
}