using System;
using DungeonSlime.Input;
using DungeonSlime.Scenes;
using MonoGameLibrary.Adapters.Gum.MonoGame;
using MonoGameLibrary.Core;
using MonoGameLibrary.Core.Content;
using MonoGameLibrary.Core.Diagnostics;
using MonoGameLibrary.Core.Lifecycle;
using MonoGameLibrary.Core.Primitives;
using MonoGameLibrary.Extensions.Audio;
using MonoGameLibrary.Extensions.Graphics;
using MonoGameLibrary.Extensions.Input;
using MonoGameLibrary.Extensions.Scenes;
using MonoGameLibrary.Extensions.UserInterface;

namespace DungeonSlime;

public sealed class GameStartup : ILoadable {
    private readonly IContentService _serviceContent;
    private readonly IAudioService _serviceAudio;
    private readonly IInputService _serviceInput;
    private readonly IInputMappingService _serviceInputMapping;
    private readonly ISceneService _serviceScene;
    private readonly IUserInterfaceService _serviceUserInterface;
    private readonly IRenderContext _contextRender;
    private readonly GumBridgesService _bridgesGum;
    private readonly IGameApplicationService _serviceApplication;
    private readonly IGameController _controllerGame;
    private readonly ILogger _logger;
    private readonly Optional<IProfiler> _profiler;

    public GameStartup(
        IContentService serviceContent,
        IAudioService serviceAudio,
        IInputService serviceInput,
        IInputMappingService serviceInputMapping,
        ISceneService serviceScene,
        IUserInterfaceService serviceUserInterface,
        IRenderContext contextRender,
        GumBridgesService bridgesGum,
        IGameApplicationService serviceApplication,
        IGameController controllerGame,
        ILogger logger,
        Optional<IProfiler> profiler = default
    ) {
        if (serviceContent == null) { throw new ArgumentNullException(nameof(serviceContent)); }
        if (serviceAudio == null) { throw new ArgumentNullException(nameof(serviceAudio)); }
        if (serviceInput == null) { throw new ArgumentNullException(nameof(serviceInput)); }
        if (serviceInputMapping == null) { throw new ArgumentNullException(nameof(serviceInputMapping)); }
        if (serviceScene == null) { throw new ArgumentNullException(nameof(serviceScene)); }
        if (serviceUserInterface == null) { throw new ArgumentNullException(nameof(serviceUserInterface)); }
        if (contextRender == null) { throw new ArgumentNullException(nameof(contextRender)); }
        if (bridgesGum == null) { throw new ArgumentNullException(nameof(bridgesGum)); }
        if (serviceApplication == null) { throw new ArgumentNullException(nameof(serviceApplication)); }
        if (controllerGame == null) { throw new ArgumentNullException(nameof(controllerGame)); }
        if (logger == null) { throw new ArgumentNullException(nameof(logger)); }
        _serviceContent = serviceContent;
        _serviceAudio = serviceAudio;
        _serviceInput = serviceInput;
        _serviceInputMapping = serviceInputMapping;
        _serviceScene = serviceScene;
        _serviceUserInterface = serviceUserInterface;
        _contextRender = contextRender;
        _bridgesGum = bridgesGum;
        _serviceApplication = serviceApplication;
        _controllerGame = controllerGame;
        _logger = logger;
        _profiler = profiler;
    }

    public void LoadContent() {
        BindInputs(_serviceInputMapping);

        Rectangle boundsScreen = _serviceApplication.ClientBounds;
        _serviceUserInterface.SetCanvas(
            boundsScreen.Width / 4.0f,
            boundsScreen.Height / 4.0f,
            4.0f
        );
        _serviceUserInterface.ConfigureInput(true, true);
        _serviceUserInterface.AddNavigationReverseKey(NavigationKey.Up);
        _serviceUserInterface.AddNavigationForwardKey(NavigationKey.Down);

        ITrackAudio trackTheme = _serviceContent.Load<ITrackAudio>("audio/theme");
        _serviceAudio.PlayTrackAudio(trackTheme, true);

        _serviceScene.ChangeScene(CreateTitleScene(boundsScreen));
    }

    private TitleScene CreateTitleScene(Rectangle boundsScreen) {
        return new TitleScene(
            _serviceContent,
            _serviceAudio,
            _serviceInput,
            _serviceScene,
            _serviceUserInterface,
            _contextRender,
            _bridgesGum,
            _serviceApplication.Exit,
            boundsScreen,
            _controllerGame,
            new Optional<ILogger>(_logger),
            _profiler
        );
    }

    private static void BindInputs(IInputMappingService serviceInputMapping) {
        serviceInputMapping.BindKey(GameAction.MoveUp, KeyCode.Up);
        serviceInputMapping.BindKey(GameAction.MoveUp, KeyCode.W);
        serviceInputMapping.BindKey(GameAction.MoveDown, KeyCode.Down);
        serviceInputMapping.BindKey(GameAction.MoveDown, KeyCode.S);
        serviceInputMapping.BindKey(GameAction.MoveLeft, KeyCode.Left);
        serviceInputMapping.BindKey(GameAction.MoveLeft, KeyCode.A);
        serviceInputMapping.BindKey(GameAction.MoveRight, KeyCode.Right);
        serviceInputMapping.BindKey(GameAction.MoveRight, KeyCode.D);
        serviceInputMapping.BindKey(GameAction.Pause, KeyCode.Escape);
        serviceInputMapping.BindKey(GameAction.Confirm, KeyCode.Enter);

        serviceInputMapping.BindButton(GameAction.MoveUp, PlayerIndex.One, GamePadButton.DPadUp);
        serviceInputMapping.BindButton(GameAction.MoveUp, PlayerIndex.One, GamePadButton.LeftThumbstickUp);
        serviceInputMapping.BindButton(GameAction.MoveDown, PlayerIndex.One, GamePadButton.DPadDown);
        serviceInputMapping.BindButton(GameAction.MoveDown, PlayerIndex.One, GamePadButton.LeftThumbstickDown);
        serviceInputMapping.BindButton(GameAction.MoveLeft, PlayerIndex.One, GamePadButton.DPadLeft);
        serviceInputMapping.BindButton(GameAction.MoveLeft, PlayerIndex.One, GamePadButton.LeftThumbstickLeft);
        serviceInputMapping.BindButton(GameAction.MoveRight, PlayerIndex.One, GamePadButton.DPadRight);
        serviceInputMapping.BindButton(GameAction.MoveRight, PlayerIndex.One, GamePadButton.LeftThumbstickRight);
        serviceInputMapping.BindButton(GameAction.Pause, PlayerIndex.One, GamePadButton.Start);
        serviceInputMapping.BindButton(GameAction.Confirm, PlayerIndex.One, GamePadButton.A);
    }
}
