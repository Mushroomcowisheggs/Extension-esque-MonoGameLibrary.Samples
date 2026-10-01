using System;
using System.Collections.Generic;
using MonoGameLibrary.Core;
using MonoGameLibrary.Core.Content;
using MonoGameLibrary.Core.Primitives;
using MonoGameLibrary.Core.Time;
using MonoGameLibrary.Extensions.Graphics;
using MonoGameLibrary.Extensions.Input;
using MonoGameLibrary.Extensions.Scenes;
using MonoGameLibrary.Extensions.Screens;
using MonoGameLibrary.Extensions.UserInterface;

namespace DungeonSlime.Screens;

public sealed class SplashScreen : Screen {
    private const float FADE_DURATION = 0.5f;
    private const float DISPLAY_TIME = 5.0f;
    private static readonly KeyCode[] smv_keysTrigger = new[] {
        KeyCode.Enter,
        KeyCode.Space,
        KeyCode.Escape
    };
    private readonly IContentService _serviceContent;
    private readonly IInputService _serviceInput;
    private readonly ISceneService _serviceScene;
    private readonly IUserInterfaceService _serviceUserInterface;
    private readonly IRenderContext _contextRender;
    private readonly Func<Scene> _factoryNextScene;
    private readonly HashSet<KeyCode> _keysWaitingForRelease = new HashSet<KeyCode>();
    private ITwoDimensionalTexture _textureLogo;
    private float _timeDisplay;
    private float _alphaFade;
    private bool _flagWaitingForRelease;
    private bool _flagShouldExit;
    private readonly Rectangle _rectangleIconSource = new Rectangle(0, 0, 128, 128);
    private readonly TwoDimensionalVector _originIcon = new TwoDimensionalVector(64.0f, 64.0f);
    private readonly TwoDimensionalVector _positionIcon = new TwoDimensionalVector(640.0f, 200.0f);
    private readonly Rectangle _rectangleWordmarkSource = new Rectangle(150, 34, 458, 58);
    private readonly TwoDimensionalVector _originWordmark = new TwoDimensionalVector(128.0f, 32.0f);
    private readonly TwoDimensionalVector _positionWordmark = new TwoDimensionalVector(640.0f, 400.0f);

    public SplashScreen(
        IContentService serviceContent,
        IInputService serviceInput,
        ISceneService serviceScene,
        IUserInterfaceService serviceUserInterface,
        IRenderContext contextRender,
        Func<Scene> factoryNextScene
    ) {
        if (serviceContent == null) { throw new ArgumentNullException(nameof(serviceContent)); }
        if (serviceInput == null) { throw new ArgumentNullException(nameof(serviceInput)); }
        if (serviceScene == null) { throw new ArgumentNullException(nameof(serviceScene)); }
        if (serviceUserInterface == null) { throw new ArgumentNullException(nameof(serviceUserInterface)); }
        if (contextRender == null) { throw new ArgumentNullException(nameof(contextRender)); }
        if (factoryNextScene == null) { throw new ArgumentNullException(nameof(factoryNextScene)); }
        _serviceContent = serviceContent;
        _serviceInput = serviceInput;
        _serviceScene = serviceScene;
        _serviceUserInterface = serviceUserInterface;
        _contextRender = contextRender;
        _factoryNextScene = factoryNextScene;
    }

    public override bool IsBlocking { get { return true; } }

    public override void LoadContent() {
        _textureLogo = _serviceContent.Load<ITwoDimensionalTexture>("images/logo");
    }

    public override void Enter() {
        _timeDisplay = DISPLAY_TIME;
        _alphaFade = 1.0f;
        _flagWaitingForRelease = false;
        _flagShouldExit = false;
        _keysWaitingForRelease.Clear();
    }

    public override void Update(FrameTime timeFrame) {
        if (_flagWaitingForRelease) {
            WaitForRelease();
            return;
        }

        foreach (KeyCode codeKey in smv_keysTrigger) {
            if (_serviceInput.WasKeyJustPressed(codeKey)) {
                _keysWaitingForRelease.Add(codeKey);
                _flagWaitingForRelease = true;
                return;
            }
        }

        _timeDisplay -= (float)timeFrame.DeltaTimeSpan.TotalSeconds;
        if (_timeDisplay <= 0.0f) {
            foreach (KeyCode codeKey in smv_keysTrigger) {
                if (_serviceInput.IsKeyDown(codeKey)) {
                    _keysWaitingForRelease.Add(codeKey);
                }
            }
            if (_keysWaitingForRelease.Count > 0) {
                _flagWaitingForRelease = true;
            }
            else {
                PerformExit();
            }
            return;
        }

        if (_timeDisplay <= FADE_DURATION) {
            _alphaFade = _timeDisplay / FADE_DURATION;
        }
    }

    private void WaitForRelease() {
        foreach (KeyCode codeKey in _keysWaitingForRelease) {
            if (_serviceInput.IsKeyDown(codeKey)) {
                return;
            }
        }
        _keysWaitingForRelease.Clear();
        _flagWaitingForRelease = false;
        PerformExit();
    }

    private void PerformExit() {
        if (_flagShouldExit) {
            return;
        }
        _flagShouldExit = true;
        _serviceUserInterface.ClearRoot();
        _serviceScene.ChangeScene(_factoryNextScene.Invoke());
        RequestPop();
    }

    public override void Draw(FrameTime timeFrame) {
        if (_textureLogo == null) {
            _contextRender.Clear(Color.DarkSlateGray);
            return;
        }
        _contextRender.Clear(Color.CornflowerBlue);
        _contextRender.Begin(new Optional<SamplerState>(SamplerState.PointClamp));
        Color tint = Color.White.MultiplyAlpha(_alphaFade);
        SpriteEffects effects = SpriteEffects.FlipHorizontally | SpriteEffects.FlipVertically;
        _textureLogo.DrawInto(
            _contextRender,
            _positionIcon,
            new OptionalValue<Rectangle>(_rectangleIconSource),
            tint,
            (float)Math.PI,
            _originIcon,
            TwoDimensionalVector.One,
            effects,
            0.0f
        );
        _textureLogo.DrawInto(
            _contextRender,
            _positionWordmark,
            new OptionalValue<Rectangle>(_rectangleWordmarkSource),
            tint,
            (float)Math.PI,
            _originWordmark,
            TwoDimensionalVector.One,
            effects,
            0.0f
        );
        _contextRender.End();
    }

    protected override void Dispose(bool flagDisposing) {
        if (flagDisposing) {
            _textureLogo = null;
            _keysWaitingForRelease.Clear();
        }
        base.Dispose(flagDisposing);
    }
}
