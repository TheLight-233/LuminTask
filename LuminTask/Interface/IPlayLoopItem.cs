namespace Lumin.Threading.Interface;

public unsafe delegate bool MoveNext(void* ptr);

public interface IPlayLoopItem
{
    bool MoveNext();
}