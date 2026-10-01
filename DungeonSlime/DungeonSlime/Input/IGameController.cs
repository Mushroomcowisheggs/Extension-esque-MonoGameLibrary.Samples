using MonoGameLibrary.Core.Primitives;

namespace DungeonSlime.Input;

public interface IGameController {
    TwoDimensionalVector GetDirection();
    bool Pause();
    bool Action();
}
