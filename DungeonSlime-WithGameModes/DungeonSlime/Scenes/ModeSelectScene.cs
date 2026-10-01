using System;
using System.Collections.Generic;
using DungeonSlime.Input;
using DungeonSlime.UI;
using Gum.DataTypes;
using Gum.Forms.Controls;
using MonoGameGum.GueDeriving;
using MonoGameLibrary.Adapters.Gum.MonoGame;
using MonoGameLibrary.Core;
using MonoGameLibrary.Core.Content;
using MonoGameLibrary.Core.Diagnostics;
using MonoGameLibrary.Core.Primitives;
using MonoGameLibrary.Core.Time;
using MonoGameLibrary.Extensions.Audio;
using MonoGameLibrary.Extensions.Graphics;
using MonoGameLibrary.Extensions.Input;
using MonoGameLibrary.Extensions.Scenes;
using MonoGameLibrary.Extensions.UserInterface;

namespace DungeonSlime.Scenes;

public sealed class ModeSelectScene : Scene {
    private const int MODES_PER_PAGE = 5;
    private static readonly string[] smv_namesMode = {
        "LOOP",
        "CLOCKWORK",
        "MIRROR",
        "TWIN",
        "ORBIT",
        "HUNTER",
        "BLOOM",
        "ECLIPSE",
        "RHYTHM",
        "TIDAL",
        "QUANTUM",
        "POLARITY",
        "SPIRAL",
        "RUSH",
        "ECHO"
    };
    private static readonly string[] smv_descriptionsMode = {
        "THE ROOM WRAPS: EVERY WALL IS A DOOR.",
        "ENTER TURNS CLOCKWISE. TIMING IS YOUR STEERING.",
        "THE CONTROL MIRROR FLIPS AFTER EVERY CATCH.",
        "GUIDE TWO SLIMES WHO MOVE AS OPPOSITES.",
        "THE TARGET ORBITS, TIGHTENS, AND REVERSES.",
        "THE BAT HUNTS YOU. SURVIVAL BECOMES SCORE.",
        "EVERY CATCH GROWS A MOVING CONSTELLATION.",
        "THE TARGET VANISHES; DARK CATCHES PAY MORE.",
        "CATCH ONLY ON THE PULSE OR THE RUN ENDS.",
        "THE ROOM BREATHES IN AND OUT AROUND YOU.",
        "REMEMBER WHICH OF THREE TARGETS IS REAL.",
        "ENTER FLIPS A FIELD BETWEEN PULL AND PUSH.",
        "THE MEANING OF EVERY DIRECTION KEEPS ROTATING.",
        "EACH TARGET HAS A SHORTER EXPIRATION CLOCK.",
        "YOUR TURNS ARRIVE HALF SECOND IN THE FUTURE."
    };

    private readonly IContentService _serviceContent;
    private readonly IAudioService _serviceAudio;
    private readonly IInputService _serviceInput;
    private readonly ISceneService _serviceScene;
    private readonly IUserInterfaceService _serviceUserInterface;
    private readonly IRenderContext _contextRender;
    private readonly GumBridgesService _bridgesGum;
    private readonly Action _actionExit;
    private readonly Rectangle _boundsScreen;
    private readonly IGameController _controllerGame;
    private readonly Optional<ILogger> _logger;
    private readonly Optional<IProfiler> _profiler;
    private readonly List<AnimatedButton> _listButtonMode;
    private Panel _panel;
    private AnimatedButton _buttonPrevious;
    private AnimatedButton _buttonNext;
    private AnimatedButton _buttonBack;
    private TextRuntime _runtimeHeading;
    private TextRuntime _runtimeDescription;
    private ITwoDimensionalTexture _patternBackground;
    private TextureAtlas _atlas;
    private IClipAudio _effectUISound;
    private TwoDimensionalVector _offsetBackground;
    private int _indexPage;

    public ModeSelectScene(
        IContentService serviceContent,
        IAudioService serviceAudio,
        IInputService serviceInput,
        ISceneService serviceScene,
        IUserInterfaceService serviceUserInterface,
        IRenderContext contextRender,
        GumBridgesService bridgesGum,
        Action actionExit,
        Rectangle boundsScreen,
        IGameController controllerGame,
        Optional<ILogger> logger = default,
        Optional<IProfiler> profiler = default
    ) {
        if (serviceContent == null) { throw new ArgumentNullException(nameof(serviceContent)); }
        if (serviceAudio == null) { throw new ArgumentNullException(nameof(serviceAudio)); }
        if (serviceInput == null) { throw new ArgumentNullException(nameof(serviceInput)); }
        if (serviceScene == null) { throw new ArgumentNullException(nameof(serviceScene)); }
        if (serviceUserInterface == null) { throw new ArgumentNullException(nameof(serviceUserInterface)); }
        if (contextRender == null) { throw new ArgumentNullException(nameof(contextRender)); }
        if (bridgesGum == null) { throw new ArgumentNullException(nameof(bridgesGum)); }
        if (actionExit == null) { throw new ArgumentNullException(nameof(actionExit)); }
        if (controllerGame == null) { throw new ArgumentNullException(nameof(controllerGame)); }
        _serviceContent = serviceContent;
        _serviceAudio = serviceAudio;
        _serviceInput = serviceInput;
        _serviceScene = serviceScene;
        _serviceUserInterface = serviceUserInterface;
        _contextRender = contextRender;
        _bridgesGum = bridgesGum;
        _actionExit = actionExit;
        _boundsScreen = boundsScreen;
        _controllerGame = controllerGame;
        _logger = logger;
        _profiler = profiler;
        _listButtonMode = new List<AnimatedButton>();
    }

    public override void LoadContent() {
        _patternBackground = _serviceContent.Load<ITwoDimensionalTexture>(
            "images/background-pattern"
        );
        _atlas = _serviceContent.Load<TextureAtlas>("images/atlas-definition.xml");
        _effectUISound = _serviceContent.Load<IClipAudio>("audio/ui");
    }

    public override void Initialize() {
        _indexPage = 0;
        _offsetBackground = TwoDimensionalVector.Zero;
        InitializeUI();
        RefreshPage();
    }

    private void InitializeUI() {
        _serviceUserInterface.ClearRoot();
        _panel = new Panel();
        _panel.Dock(Gum.Wireframe.Dock.Fill);
        _serviceUserInterface.AddToRoot(_panel.Visual);

        _runtimeHeading = CreateText(10.0f, 8.0f, 0.5f);
        _panel.AddChild(_runtimeHeading);
        _runtimeDescription = CreateText(10.0f, 31.0f, 0.22f);
        _panel.AddChild(_runtimeDescription);

        for (int index = 0; index < MODES_PER_PAGE; index += 1) {
            AnimatedButton button = CreateButton(
                24.0f,
                54.0f + index * 20.0f,
                string.Empty,
                HandleModeClicked
            );
            button.GotFocus += HandleModeFocused;
            _panel.AddChild(button);
            _listButtonMode.Add(button);
        }

        _buttonPrevious = CreateButton(
            10.0f,
            -10.0f,
            "PREV",
            HandlePreviousClicked
        );
        _buttonPrevious.Anchor(Gum.Wireframe.Anchor.BottomLeft);
        _panel.AddChild(_buttonPrevious);

        _buttonNext = CreateButton(
            0.0f,
            -10.0f,
            "NEXT",
            HandleNextClicked
        );
        _buttonNext.Anchor(Gum.Wireframe.Anchor.Bottom);
        _panel.AddChild(_buttonNext);

        _buttonBack = CreateButton(
            -10.0f,
            -10.0f,
            "BACK",
            HandleBackClicked
        );
        _buttonBack.Anchor(Gum.Wireframe.Anchor.BottomRight);
        _panel.AddChild(_buttonBack);
    }

    private static TextRuntime CreateText(float x, float y, float scale) {
        TextRuntime text = new TextRuntime();
        text.X = x;
        text.Y = y;
        text.WidthUnits = DimensionUnitType.RelativeToChildren;
        text.UseCustomFont = true;
        text.CustomFontFile = @"fonts/04b_30.fnt";
        text.FontScale = scale;
        return text;
    }

    private AnimatedButton CreateButton(
        float x,
        float y,
        string text,
        EventHandler handlerClick
    ) {
        AnimatedButton button = new AnimatedButton(_atlas, _bridgesGum);
        button.X = x;
        button.Y = y;
        button.Text = text;
        button.Click += handlerClick;
        return button;
    }

    private void RefreshPage() {
        int countPage = (smv_namesMode.Length + MODES_PER_PAGE - 1) / MODES_PER_PAGE;
        _runtimeHeading.Text = string.Format(
            "MODE LAB  {0}/{1}",
            _indexPage + 1,
            countPage
        );
        for (int index = 0; index < _listButtonMode.Count; index += 1) {
            int indexMode = _indexPage * MODES_PER_PAGE + index;
            AnimatedButton button = _listButtonMode[index];
            bool flagAvailable = indexMode < smv_namesMode.Length;
            button.IsVisible = flagAvailable;
            if (flagAvailable) {
                button.Text = smv_namesMode[indexMode];
            }
        }
        _buttonPrevious.IsEnabled = _indexPage > 0;
        _buttonNext.IsEnabled = _indexPage < countPage - 1;
        FocusFirstMode();
    }

    private void FocusFirstMode() {
        if (_listButtonMode.Count > 0) {
            _listButtonMode[0].IsFocused = true;
            UpdateDescription(0);
        }
    }

    private void UpdateDescription(int indexVisible) {
        int indexMode = _indexPage * MODES_PER_PAGE + indexVisible;
        if (indexMode >= 0 && indexMode < smv_descriptionsMode.Length) {
            _runtimeDescription.Text = smv_descriptionsMode[indexMode];
        }
    }

    private void HandleModeFocused(object sender, EventArgs arguments) {
        AnimatedButton button = sender as AnimatedButton;
        if (button == null) {
            return;
        }
        int indexVisible = _listButtonMode.IndexOf(button);
        UpdateDescription(indexVisible);
    }

    private void HandleModeClicked(object sender, EventArgs arguments) {
        AnimatedButton button = sender as AnimatedButton;
        if (button == null) {
            return;
        }
        int indexVisible = _listButtonMode.IndexOf(button);
        int indexMode = _indexPage * MODES_PER_PAGE + indexVisible;
        if (indexMode < 0 || indexMode >= smv_namesMode.Length) {
            return;
        }
        _serviceAudio.PlayClipAudio(_effectUISound);
        StartMode(indexMode);
    }

    private void HandlePreviousClicked(object sender, EventArgs arguments) {
        if (_indexPage <= 0) {
            return;
        }
        _serviceAudio.PlayClipAudio(_effectUISound);
        _indexPage -= 1;
        RefreshPage();
    }

    private void HandleNextClicked(object sender, EventArgs arguments) {
        int countPage = (smv_namesMode.Length + MODES_PER_PAGE - 1) / MODES_PER_PAGE;
        if (_indexPage >= countPage - 1) {
            return;
        }
        _serviceAudio.PlayClipAudio(_effectUISound);
        _indexPage += 1;
        RefreshPage();
    }

    private void HandleBackClicked(object sender, EventArgs arguments) {
        _serviceAudio.PlayClipAudio(_effectUISound);
        ReturnToTitle();
    }

    private void StartMode(int indexMode) {
        Scene sceneSelected = null;
        switch (indexMode) {
            case 0: sceneSelected = new LoopGameScene(
                _serviceContent, _serviceAudio, _serviceInput, _serviceScene,
                _serviceUserInterface, _contextRender, _bridgesGum, _actionExit,
                _boundsScreen, _controllerGame, _logger, _profiler
            ); break;
            case 1: sceneSelected = new ClockworkGameScene(
                _serviceContent, _serviceAudio, _serviceInput, _serviceScene,
                _serviceUserInterface, _contextRender, _bridgesGum, _actionExit,
                _boundsScreen, _controllerGame, _logger, _profiler
            ); break;
            case 2: sceneSelected = new MirrorGameScene(
                _serviceContent, _serviceAudio, _serviceInput, _serviceScene,
                _serviceUserInterface, _contextRender, _bridgesGum, _actionExit,
                _boundsScreen, _controllerGame, _logger, _profiler
            ); break;
            case 3: sceneSelected = new TwinGameScene(
                _serviceContent, _serviceAudio, _serviceInput, _serviceScene,
                _serviceUserInterface, _contextRender, _bridgesGum, _actionExit,
                _boundsScreen, _controllerGame, _logger, _profiler
            ); break;
            case 4: sceneSelected = new OrbitGameScene(
                _serviceContent, _serviceAudio, _serviceInput, _serviceScene,
                _serviceUserInterface, _contextRender, _bridgesGum, _actionExit,
                _boundsScreen, _controllerGame, _logger, _profiler
            ); break;
            case 5: sceneSelected = new HunterGameScene(
                _serviceContent, _serviceAudio, _serviceInput, _serviceScene,
                _serviceUserInterface, _contextRender, _bridgesGum, _actionExit,
                _boundsScreen, _controllerGame, _logger, _profiler
            ); break;
            case 6: sceneSelected = new BloomGameScene(
                _serviceContent, _serviceAudio, _serviceInput, _serviceScene,
                _serviceUserInterface, _contextRender, _bridgesGum, _actionExit,
                _boundsScreen, _controllerGame, _logger, _profiler
            ); break;
            case 7: sceneSelected = new EclipseGameScene(
                _serviceContent, _serviceAudio, _serviceInput, _serviceScene,
                _serviceUserInterface, _contextRender, _bridgesGum, _actionExit,
                _boundsScreen, _controllerGame, _logger, _profiler
            ); break;
            case 8: sceneSelected = new RhythmGameScene(
                _serviceContent, _serviceAudio, _serviceInput, _serviceScene,
                _serviceUserInterface, _contextRender, _bridgesGum, _actionExit,
                _boundsScreen, _controllerGame, _logger, _profiler
            ); break;
            case 9: sceneSelected = new TidalGameScene(
                _serviceContent, _serviceAudio, _serviceInput, _serviceScene,
                _serviceUserInterface, _contextRender, _bridgesGum, _actionExit,
                _boundsScreen, _controllerGame, _logger, _profiler
            ); break;
            case 10: sceneSelected = new QuantumGameScene(
                _serviceContent, _serviceAudio, _serviceInput, _serviceScene,
                _serviceUserInterface, _contextRender, _bridgesGum, _actionExit,
                _boundsScreen, _controllerGame, _logger, _profiler
            ); break;
            case 11: sceneSelected = new PolarityGameScene(
                _serviceContent, _serviceAudio, _serviceInput, _serviceScene,
                _serviceUserInterface, _contextRender, _bridgesGum, _actionExit,
                _boundsScreen, _controllerGame, _logger, _profiler
            ); break;
            case 12: sceneSelected = new SpiralGameScene(
                _serviceContent, _serviceAudio, _serviceInput, _serviceScene,
                _serviceUserInterface, _contextRender, _bridgesGum, _actionExit,
                _boundsScreen, _controllerGame, _logger, _profiler
            ); break;
            case 13: sceneSelected = new RushGameScene(
                _serviceContent, _serviceAudio, _serviceInput, _serviceScene,
                _serviceUserInterface, _contextRender, _bridgesGum, _actionExit,
                _boundsScreen, _controllerGame, _logger, _profiler
            ); break;
            case 14: sceneSelected = new EchoGameScene(
                _serviceContent, _serviceAudio, _serviceInput, _serviceScene,
                _serviceUserInterface, _contextRender, _bridgesGum, _actionExit,
                _boundsScreen, _controllerGame, _logger, _profiler
            ); break;
        }
        if (sceneSelected != null) {
            _serviceScene.ChangeScene(sceneSelected);
        }
    }

    private void ReturnToTitle() {
        _serviceScene.ChangeScene(new TitleScene(
            _serviceContent, _serviceAudio, _serviceInput, _serviceScene,
            _serviceUserInterface, _contextRender, _bridgesGum, _actionExit,
            _boundsScreen, _controllerGame, _logger, _profiler
        ));
    }

    public override void Update(FrameTime timeFrame) {
        if (_serviceInput.WasKeyJustPressed(KeyCode.Escape)) {
            ReturnToTitle();
            return;
        }
        float amountOffset = 32.0f * (float)timeFrame.DeltaTimeSpan.TotalSeconds;
        _offsetBackground.X -= amountOffset;
        _offsetBackground.Y -= amountOffset * 0.5f;
        _offsetBackground.X %= _patternBackground.Width;
        _offsetBackground.Y %= _patternBackground.Height;
    }

    public override void Draw(FrameTime timeFrame) {
        _contextRender.Clear(new Color(18, 24, 52, 255));
        _contextRender.Begin(new Optional<SamplerState>(SamplerState.PointWrap));
        Rectangle sourceBackground = new Rectangle(
            (int)_offsetBackground.X,
            (int)_offsetBackground.Y,
            _boundsScreen.Width,
            _boundsScreen.Height
        );
        _patternBackground.DrawInto(
            _contextRender,
            new TwoDimensionalVector(_boundsScreen.X, _boundsScreen.Y),
            new OptionalValue<Rectangle>(sourceBackground),
            new Color(130, 180, 255, 150),
            0.0f,
            TwoDimensionalVector.Zero,
            TwoDimensionalVector.One,
            SpriteEffects.None,
            0.0f
        );
        _contextRender.End();
    }

    protected override void Dispose(bool flagDisposing) {
        if (flagDisposing) {
            foreach (AnimatedButton button in _listButtonMode) {
                button.Click -= HandleModeClicked;
                button.GotFocus -= HandleModeFocused;
                button.Dispose();
            }
            _listButtonMode.Clear();
            DisposeButton(ref _buttonPrevious, HandlePreviousClicked);
            DisposeButton(ref _buttonNext, HandleNextClicked);
            DisposeButton(ref _buttonBack, HandleBackClicked);
            if (_atlas != null) {
                _atlas.Dispose();
            }
        }
        base.Dispose(flagDisposing);
    }

    private static void DisposeButton(
        ref AnimatedButton button,
        EventHandler handlerClick
    ) {
        if (button == null) {
            return;
        }
        button.Click -= handlerClick;
        button.Dispose();
        button = null;
    }
}
