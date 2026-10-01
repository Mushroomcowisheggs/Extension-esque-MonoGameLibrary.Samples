using System;
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

public class TitleScene : Scene {
    private const string DUNGEON_TEXT = "Dungeon";
    private const string SLIME_TEXT = "Slime";
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
    private ISpriteFont _font5x;
    private TwoDimensionalVector _positionDungeonText;
    private TwoDimensionalVector _originDungeonText;
    private TwoDimensionalVector _positionSlimeText;
    private TwoDimensionalVector _originSlimeText;
    private ITwoDimensionalTexture _patternBackground;
    private TwoDimensionalVector _offsetBackground;
    private readonly float _speedScroll = 50.0f;
    private IClipAudio _effectUISound;
    private Panel _panelTitleScreenButtons;
    private Panel _panelOptions;
    private AnimatedButton _buttonStart;
    private OptionsSlider _sliderMusic;
    private OptionsSlider _sliderSfx;
    private AnimatedButton _buttonOptions;
    private AnimatedButton _buttonOptionsBack;
    private TextureAtlas _atlas;

    public TitleScene(
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
    }

    public override void LoadContent() {
        _font5x = _serviceContent.Load<ISpriteFont>("fonts/04B_30_5x");
        _patternBackground = _serviceContent.Load<ITwoDimensionalTexture>("images/background-pattern");
        _effectUISound = _serviceContent.Load<IClipAudio>("audio/ui");
        _atlas = _serviceContent.Load<TextureAtlas>("images/atlas-definition.xml");
    }

    public override void Initialize() {
        TwoDimensionalVector size = _font5x.MeasureString(DUNGEON_TEXT);
        _positionDungeonText = new TwoDimensionalVector(640.0f, 100.0f);
        _originDungeonText = size * 0.5f;
        size = _font5x.MeasureString(SLIME_TEXT);
        _positionSlimeText = new TwoDimensionalVector(757.0f, 207.0f);
        _originSlimeText = size * 0.5f;
        _offsetBackground = TwoDimensionalVector.Zero;
        InitializeUI();
    }

    private void InitializeUI() {
        _serviceUserInterface.ClearRoot();
        CreateTitlePanel();
        CreateOptionsPanel();
    }

    private void CreateTitlePanel() {
        _panelTitleScreenButtons = new Panel();
        _panelTitleScreenButtons.Dock(Gum.Wireframe.Dock.Fill);
        _serviceUserInterface.AddToRoot(_panelTitleScreenButtons.Visual);

        _buttonStart = new AnimatedButton(_atlas, _bridgesGum);
        _buttonStart.Anchor(Gum.Wireframe.Anchor.BottomLeft);
        _buttonStart.X = 50.0f;
        _buttonStart.Y = -12.0f;
        _buttonStart.Text = "Start";
        _buttonStart.Click += HandleStartClicked;
        _panelTitleScreenButtons.AddChild(_buttonStart);

        _buttonOptions = new AnimatedButton(_atlas, _bridgesGum);
        _buttonOptions.Anchor(Gum.Wireframe.Anchor.BottomRight);
        _buttonOptions.X = -50.0f;
        _buttonOptions.Y = -12.0f;
        _buttonOptions.Text = "Options";
        _buttonOptions.Click += HandleOptionsClicked;
        _panelTitleScreenButtons.AddChild(_buttonOptions);
        _buttonStart.IsFocused = true;
    }

    private void HandleStartClicked(object sender, EventArgs arguments) {
        _serviceAudio.PlayClipAudio(_effectUISound);
        _serviceScene.ChangeScene(new GameScene(
            _serviceContent, _serviceAudio, _serviceInput, _serviceScene,
            _serviceUserInterface, _contextRender, _bridgesGum, _actionExit,
            _boundsScreen, _controllerGame,
            _logger, _profiler
        ));
    }

    private void HandleOptionsClicked(object sender, EventArgs arguments) {
        _serviceAudio.PlayClipAudio(_effectUISound);
        _panelTitleScreenButtons.IsVisible = false;
        _panelOptions.IsVisible = true;
        _buttonOptionsBack.IsFocused = true;
    }

    private void CreateOptionsPanel() {
        _panelOptions = new Panel();
        _panelOptions.Dock(Gum.Wireframe.Dock.Fill);
        _panelOptions.IsVisible = false;
        _serviceUserInterface.AddToRoot(_panelOptions.Visual);

        TextRuntime textOptions = new TextRuntime();
        textOptions.X = 10.0f;
        textOptions.Y = 10.0f;
        textOptions.Text = "OPTIONS";
        textOptions.UseCustomFont = true;
        textOptions.FontScale = 0.5f;
        textOptions.CustomFontFile = @"fonts/04b_30.fnt";
        _panelOptions.AddChild(textOptions);

        _sliderMusic = CreateSlider("MusicSlider", "MUSIC", 30.0f, _serviceAudio.SongVolume);
        _sliderMusic.ValueChanged += HandleMusicSliderValueChanged;
        _sliderMusic.ValueChangeCompleted += HandleMusicSliderValueChangeCompleted;
        _panelOptions.AddChild(_sliderMusic);

        _sliderSfx = CreateSlider("SfxSlider", "SFX", 93.0f, _serviceAudio.SoundEffectVolume);
        _sliderSfx.ValueChanged += HandleSfxSliderChanged;
        _sliderSfx.ValueChangeCompleted += HandleSfxSliderChangeCompleted;
        _panelOptions.AddChild(_sliderSfx);

        _buttonOptionsBack = new AnimatedButton(_atlas, _bridgesGum);
        _buttonOptionsBack.Text = "BACK";
        _buttonOptionsBack.Anchor(Gum.Wireframe.Anchor.BottomRight);
        _buttonOptionsBack.X = -28.0f;
        _buttonOptionsBack.Y = -10.0f;
        _buttonOptionsBack.Click += HandleOptionsButtonBack;
        _panelOptions.AddChild(_buttonOptionsBack);
    }

    private OptionsSlider CreateSlider(string name, string text, float y, float value) {
        OptionsSlider slider = new OptionsSlider(_atlas, _bridgesGum);
        slider.Name = name;
        slider.Text = text;
        slider.Anchor(Gum.Wireframe.Anchor.Top);
        slider.Y = y;
        slider.Minimum = 0.0;
        slider.Maximum = 1.0;
        slider.Value = value;
        slider.SmallChange = 0.1;
        slider.LargeChange = 0.2;
        return slider;
    }

    private void HandleSfxSliderChanged(object sender, EventArgs arguments) {
        Slider slider = (Slider)sender;
        _serviceAudio.SoundEffectVolume = (float)slider.Value;
    }

    private void HandleSfxSliderChangeCompleted(object sender, EventArgs arguments) {
        _serviceAudio.PlayClipAudio(_effectUISound);
    }

    private void HandleMusicSliderValueChanged(object sender, EventArgs arguments) {
        Slider slider = (Slider)sender;
        _serviceAudio.SongVolume = (float)slider.Value;
    }

    private void HandleMusicSliderValueChangeCompleted(object sender, EventArgs arguments) {
        _serviceAudio.PlayClipAudio(_effectUISound);
    }

    private void HandleOptionsButtonBack(object sender, EventArgs arguments) {
        _serviceAudio.PlayClipAudio(_effectUISound);
        _panelTitleScreenButtons.IsVisible = true;
        _panelOptions.IsVisible = false;
        _buttonOptions.IsFocused = true;
    }

    public override void Update(FrameTime timeFrame) {
        if (_serviceInput.WasKeyJustPressed(KeyCode.Escape)) {
            _actionExit.Invoke();
        }
        float offset = _speedScroll * (float)timeFrame.DeltaTimeSpan.TotalSeconds;
        _offsetBackground.X -= offset;
        _offsetBackground.Y -= offset;
        _offsetBackground.X %= _patternBackground.Width;
        _offsetBackground.Y %= _patternBackground.Height;
    }

    public override void Draw(FrameTime timeFrame) {
        _contextRender.Clear(new Color(32, 40, 78, 255));
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
            Color.White.MultiplyAlpha(0.5f),
            0.0f,
            TwoDimensionalVector.Zero,
            TwoDimensionalVector.One,
            SpriteEffects.None,
            0.0f
        );
        _contextRender.End();

        if (_panelTitleScreenButtons.IsVisible) {
            _contextRender.Begin(new Optional<SamplerState>(SamplerState.PointClamp));
            Color shadow = Color.Black.MultiplyAlpha(0.5f);
            DrawTitleText(
                _contextRender, DUNGEON_TEXT, _positionDungeonText,
                _originDungeonText, shadow
            );
            DrawTitleText(
                _contextRender, SLIME_TEXT, _positionSlimeText,
                _originSlimeText, shadow
            );
            _contextRender.End();
        }
    }

    private void DrawTitleText(
        IRenderContext contextRender,
        string text,
        TwoDimensionalVector position,
        TwoDimensionalVector origin,
        Color shadow
    ) {
        _font5x.DrawInto(
            contextRender, text, position + new TwoDimensionalVector(10.0f, 10.0f),
            shadow, 0.0f, origin, 1.0f, SpriteEffects.None, 1.0f
        );
        _font5x.DrawInto(
            contextRender, text, position, Color.White, 0.0f, origin,
            1.0f, SpriteEffects.None, 1.0f
        );
    }

    protected override void Dispose(bool flagDisposing) {
        if (flagDisposing) {
            DisposeControls();
            if (_atlas != null) {
                _atlas.Dispose();
            }
        }
        base.Dispose(flagDisposing);
    }

    private void DisposeControls() {
        if (_buttonStart != null) {
            _buttonStart.Click -= HandleStartClicked;
            _buttonStart.Dispose();
            _buttonStart = null;
        }
        if (_buttonOptions != null) {
            _buttonOptions.Click -= HandleOptionsClicked;
            _buttonOptions.Dispose();
            _buttonOptions = null;
        }
        if (_buttonOptionsBack != null) {
            _buttonOptionsBack.Click -= HandleOptionsButtonBack;
            _buttonOptionsBack.Dispose();
            _buttonOptionsBack = null;
        }
        if (_sliderMusic != null) {
            _sliderMusic.ValueChanged -= HandleMusicSliderValueChanged;
            _sliderMusic.ValueChangeCompleted -= HandleMusicSliderValueChangeCompleted;
            _sliderMusic.Dispose();
            _sliderMusic = null;
        }
        if (_sliderSfx != null) {
            _sliderSfx.ValueChanged -= HandleSfxSliderChanged;
            _sliderSfx.ValueChangeCompleted -= HandleSfxSliderChangeCompleted;
            _sliderSfx.Dispose();
            _sliderSfx = null;
        }
    }
}
