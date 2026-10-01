using System;
using MonoGameLibrary.Core.Primitives;
using MonoGameLibrary.Extensions.Input;

namespace DungeonSlime.Input;

public sealed class GameController : IGameController {
    private readonly IInputMappingService _serviceInputMapping;

    public GameController(IInputMappingService serviceInputMapping) {
        if (serviceInputMapping == null) {
            throw new ArgumentNullException(nameof(serviceInputMapping));
        }
        _serviceInputMapping = serviceInputMapping;
    }

    public TwoDimensionalVector GetDirection() {
        if (_serviceInputMapping.IsActionPressed(GameAction.MoveUp)) {
            return -TwoDimensionalVector.UnitY;
        }
        if (_serviceInputMapping.IsActionPressed(GameAction.MoveDown)) {
            return TwoDimensionalVector.UnitY;
        }
        if (_serviceInputMapping.IsActionPressed(GameAction.MoveLeft)) {
            return -TwoDimensionalVector.UnitX;
        }
        if (_serviceInputMapping.IsActionPressed(GameAction.MoveRight)) {
            return TwoDimensionalVector.UnitX;
        }
        return TwoDimensionalVector.Zero;
    }

    public bool Pause() {
        return _serviceInputMapping.IsActionPressed(GameAction.Pause);
    }

    public bool Action() {
        return _serviceInputMapping.IsActionPressed(GameAction.Confirm);
    }
}
