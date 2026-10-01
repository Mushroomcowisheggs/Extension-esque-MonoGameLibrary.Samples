using MonoGameLibrary.Core.Primitives;

namespace DungeonSlime.GameObjects;

public struct SlimeSegment {
    public TwoDimensionalVector At;
    public TwoDimensionalVector To;
    public TwoDimensionalVector Direction;

    public TwoDimensionalVector ReverseDirection {
        get { return -Direction; }
    }
}
