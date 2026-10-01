using System;
using DungeonSlime;
using DungeonSlime.Diagnostics;
using DungeonSlime.Input;
using MonoGameLibrary.Adapters.Gum.MonoGame;
using MonoGameLibrary.Adapters.MonoGame.Input;
using MonoGameLibrary.Adapters.MonoGame.Lifecycle;
using MonoGameLibrary.Core;
using MonoGameLibrary.Core.Content;
using MonoGameLibrary.Core.Diagnostics;
using MonoGameLibrary.Core.Hosting;
using MonoGameLibrary.Core.Lifecycle;
using MonoGameLibrary.Core.Modularity;
using MonoGameLibrary.Extensions;
using MonoGameLibrary.Extensions.Audio;
using MonoGameLibrary.Extensions.Graphics;
using MonoGameLibrary.Extensions.Input;
using MonoGameLibrary.Extensions.Scenes;
using MonoGameLibrary.Extensions.UserInterface;

GameApplicationOptions options = new GameApplicationOptions();
options.Title = "Dungeon Slime";
options.Width = 1280;
options.Height = 720;
options.ContentRootDirectory = "Content";
options.IsMouseVisible = true;

GameApplication.Run(options, delegate(GameBuilder builder) {
    ILogger logger = new FileLogger();
    builder.UseDefaultServices(new Optional<ILogger>(logger));
    builder.LoadModulesFrom(AppContext.BaseDirectory, new Optional<ILogger>(logger));
    builder.UseInputMapping();

    IInputMappingService serviceInputMapping = builder.GetService<IInputMappingService>();
    IGameController controllerGame = new GameController(serviceInputMapping);
    builder.RegisterService<IGameController>(controllerGame);

    builder.ConfigureHost(delegate(GameHost host) {
        host.OnError = delegate(Exception exception, string context) {
            logger.Error("Error in " + context, exception);
        };
    });

    GameStartup startup = new GameStartup(
        builder.GetService<IContentService>(),
        builder.GetService<IAudioService>(),
        builder.GetService<IInputService>(),
        serviceInputMapping,
        builder.GetService<ISceneService>(),
        builder.GetService<IUserInterfaceService>(),
        builder.GetService<IRenderContext>(),
        builder.GetService<GumBridgesService>(),
        builder.GetService<IGameApplicationService>(),
        controllerGame,
        logger,
        new Optional<IProfiler>(builder.GetService<IProfiler>())
    );
    builder.AddModule(startup);
});
