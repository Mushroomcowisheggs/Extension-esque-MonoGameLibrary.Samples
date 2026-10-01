using System;
using Gum.DataTypes;
using Gum.Forms.Controls;
using Gum.Managers;
using MonoGameGum.GueDeriving;
using MonoGameLibrary.Adapters.Gum.MonoGame;
using MonoGameLibrary.Core.Content;
using MonoGameLibrary.Extensions.Audio;
using MonoGameLibrary.Extensions.Graphics;
using MonoGameLibrary.Extensions.UserInterface;
using MonoGameLibrary.Utilities;

namespace DungeonSlime.UI;

public sealed class GameSceneUI : ContainerRuntime, IDisposable {
    private static readonly string smv_formatScore = "SCORE: {0:D6}";
    private readonly IAudioService _serviceAudio;
    private readonly IClipAudio _effectUISound;
    private readonly TextureAtlas _atlas;
    private readonly GumBridgesService _bridgesGum;
    private readonly CompensationStack _compensations = new CompensationStack();
    private Panel _panelPause;
    private Panel _panelGameOver;
    private AnimatedButton _buttonResume;
    private AnimatedButton _buttonRetry;
    private TextRuntime _runtimeScoreText;
    private bool _flagDisposed;

    public event EventHandler ResumeButtonClick;
    public event EventHandler QuitButtonClick;
    public event EventHandler RetryButtonClick;

    public GameSceneUI(
        IAudioService serviceAudio,
        IContentService serviceContent,
        IUserInterfaceService serviceUserInterface,
        TextureAtlas atlas,
        GumBridgesService bridgesGum
    ) {
        if (serviceAudio == null) { throw new ArgumentNullException(nameof(serviceAudio)); }
        if (serviceContent == null) { throw new ArgumentNullException(nameof(serviceContent)); }
        if (serviceUserInterface == null) { throw new ArgumentNullException(nameof(serviceUserInterface)); }
        if (atlas == null) { throw new ArgumentNullException(nameof(atlas)); }
        if (bridgesGum == null) { throw new ArgumentNullException(nameof(bridgesGum)); }

        _serviceAudio = serviceAudio;
        _atlas = atlas;
        _bridgesGum = bridgesGum;
        _effectUISound = serviceContent.Load<IClipAudio>("audio/ui");
        Dock(Gum.Wireframe.Dock.Fill);
        serviceUserInterface.AddToRoot(this);

        _runtimeScoreText = CreateScoreText();
        AddChild(_runtimeScoreText);
        _panelPause = CreatePausePanel(atlas);
        AddChild(_panelPause.Visual);
        _panelGameOver = CreateGameOverPanel(atlas);
        AddChild(_panelGameOver.Visual);
    }

    private TextRuntime CreateScoreText() {
        TextRuntime text = new TextRuntime();
        text.Anchor(Gum.Wireframe.Anchor.TopLeft);
        text.WidthUnits = DimensionUnitType.RelativeToChildren;
        text.X = 20.0f;
        text.Y = 5.0f;
        text.UseCustomFont = true;
        text.CustomFontFile = @"fonts/04b_30.fnt";
        text.FontScale = 0.25f;
        text.Text = string.Format(smv_formatScore, 0);
        return text;
    }

    private Panel CreatePausePanel(TextureAtlas atlas) {
        Panel panel = CreatePanel(atlas);
        AddPanelTitle(panel, "PAUSED");
        _buttonResume = CreateButton(panel, "RESUME", false, OnResumeButtonClicked);
        CreateButton(panel, "QUIT", true, OnQuitButtonClicked);
        return panel;
    }

    private Panel CreateGameOverPanel(TextureAtlas atlas) {
        Panel panel = CreatePanel(atlas);
        AddPanelTitle(panel, "GAME OVER");
        _buttonRetry = CreateButton(panel, "RETRY", false, OnRetryButtonClicked);
        CreateButton(panel, "QUIT", true, OnQuitButtonClicked);
        return panel;
    }

    private Panel CreatePanel(TextureAtlas atlas) {
        Panel panel = new Panel();
        panel.Anchor(Gum.Wireframe.Anchor.Center);
        panel.WidthUnits = DimensionUnitType.Absolute;
        panel.HeightUnits = DimensionUnitType.Absolute;
        panel.Width = 264.0f;
        panel.Height = 70.0f;
        panel.IsVisible = false;

        TextureRegion region = atlas.GetRegion("panel-background");
        NineSliceRuntime background = new NineSliceRuntime();
        background.Dock(Gum.Wireframe.Dock.Fill);
        background.SetTexture(region.Texture, _bridgesGum);
        background.TextureAddress = TextureAddress.Custom;
        background.TextureHeight = region.Height;
        background.TextureWidth = region.Width;
        background.TextureTop = region.SourceRectangle.Top;
        background.TextureLeft = region.SourceRectangle.Left;
        panel.AddChild(background);
        return panel;
    }

    private static void AddPanelTitle(Panel panel, string title) {
        TextRuntime text = new TextRuntime();
        text.Text = title;
        text.WidthUnits = DimensionUnitType.RelativeToChildren;
        text.UseCustomFont = true;
        text.CustomFontFile = "fonts/04b_30.fnt";
        text.FontScale = 0.5f;
        text.X = 10.0f;
        text.Y = 10.0f;
        panel.AddChild(text);
    }

    private AnimatedButton CreateButton(
        Panel panel,
        string text,
        bool flagRight,
        EventHandler handlerClick
    ) {
        AnimatedButton button = new AnimatedButton(_atlas, _bridgesGum);
        button.Text = text;
        if (flagRight) {
            button.Anchor(Gum.Wireframe.Anchor.BottomRight);
            button.X = -9.0f;
        }
        else {
            button.Anchor(Gum.Wireframe.Anchor.BottomLeft);
            button.X = 9.0f;
        }
        button.Y = -9.0f;
        button.Click += handlerClick;
        button.GotFocus += OnElementGotFocus;
        _compensations.Compensate(delegate {
            button.Click -= handlerClick;
            button.GotFocus -= OnElementGotFocus;
            button.Dispose();
        });
        panel.AddChild(button);
        return button;
    }

    private void OnResumeButtonClicked(object sender, EventArgs arguments) {
        _serviceAudio.PlayClipAudio(_effectUISound);
        HidePausePanel();
        if (ResumeButtonClick != null) {
            ResumeButtonClick(sender, arguments);
        }
    }

    private void OnRetryButtonClicked(object sender, EventArgs arguments) {
        _serviceAudio.PlayClipAudio(_effectUISound);
        HideGameOverPanel();
        if (RetryButtonClick != null) {
            RetryButtonClick(sender, arguments);
        }
    }

    private void OnQuitButtonClicked(object sender, EventArgs arguments) {
        _serviceAudio.PlayClipAudio(_effectUISound);
        HidePausePanel();
        HideGameOverPanel();
        if (QuitButtonClick != null) {
            QuitButtonClick(sender, arguments);
        }
    }

    private void OnElementGotFocus(object sender, EventArgs arguments) {
        _serviceAudio.PlayClipAudio(_effectUISound);
    }

    public void UpdateScoreText(int score) {
        _runtimeScoreText.Text = string.Format(smv_formatScore, score);
    }

    public void ShowPausePanel() {
        _panelPause.IsVisible = true;
        _buttonResume.IsFocused = true;
        _panelGameOver.IsVisible = false;
    }

    public void HidePausePanel() {
        _panelPause.IsVisible = false;
    }

    public void ShowGameOverPanel() {
        _panelGameOver.IsVisible = true;
        _buttonRetry.IsFocused = true;
        _panelPause.IsVisible = false;
    }

    public void HideGameOverPanel() {
        _panelGameOver.IsVisible = false;
    }

    public void Dispose() {
        if (_flagDisposed) {
            return;
        }
        _compensations.Dispose();
        _flagDisposed = true;
        GC.SuppressFinalize(this);
    }
}
