using MonoGameLibrary.Core.Primitives;

namespace DungeonSlime.Input;

public interface IGameController {
    TwoDimensionalVector GetDirection();
    TwoDimensionalVector GetFreeDirection();
    bool Pause();
    bool Action();
}
