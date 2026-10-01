using System;
using System.Collections.Generic;
using DungeonSlime.GameObjects;
using DungeonSlime.Input;
using DungeonSlime.UI;
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

public sealed class HunterGameScene : Scene {
    private enum GameState {
        Playing,
        Paused,
        GameOver
    }

    private const float FADE_SPEED = 0.02f;
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
    private Slime _slime;
    private Bat _bat;
    private Tilemap _tilemap;
    private Rectangle _boundsRoom;
    private IClipAudio _effectCollectSound;
    private IClipAudio _effectBounceSound;
    private int _score;
    private GameSceneUI _ui;
    private GameState _state;
    private IEffect _effectGrayscale;
    private float _saturation = 1.0f;
    private TextureAtlas _atlas;
    private TimeSpan _spanScore;

    public HunterGameScene(
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
        _atlas = _serviceContent.Load<TextureAtlas>("images/atlas-definition.xml");
        _tilemap = _serviceContent.Load<Tilemap>("images/tilemap-definition.xml");
        _tilemap.Scale = new TwoDimensionalVector(4.0f, 4.0f);
        _effectBounceSound = _serviceContent.Load<IClipAudio>("audio/bounce");
        _effectCollectSound = _serviceContent.Load<IClipAudio>("audio/collect");
        _effectGrayscale = _serviceContent.Load<IEffect>("effects/grayscaleEffect");
    }

    public override void Initialize() {
        _boundsRoom = _boundsScreen;
        _boundsRoom.Inflate(
            -(int)_tilemap.ScaledTileWidth,
            -(int)_tilemap.ScaledTileHeight
        );
        AnimatedSprite animationSlime = _atlas.CreateAnimatedSprite("slime-animation");
        animationSlime.Scale = new TwoDimensionalVector(4.0f, 4.0f);
        AnimatedSprite animationBat = _atlas.CreateAnimatedSprite("bat-animation");
        animationBat.Scale = new TwoDimensionalVector(4.0f, 4.0f);
        _slime = new Slime(animationSlime);
        _bat = new Bat(animationBat, _effectBounceSound, _serviceAudio);
        _slime.BodyCollision += OnSlimeBodyCollision;
        InitializeUI();
        InitializeNewGame();
    }

    private void InitializeUI() {
        _serviceUserInterface.ClearRoot();
        _ui = new GameSceneUI(
            _serviceAudio,
            _serviceContent,
            _serviceUserInterface,
            _atlas,
            _bridgesGum
        );
        _ui.ResumeButtonClick += OnResumeButtonClicked;
        _ui.RetryButtonClick += OnRetryButtonClicked;
        _ui.QuitButtonClick += OnQuitButtonClicked;
    }

    private void OnResumeButtonClicked(object sender, EventArgs arguments) {
        _state = GameState.Playing;
    }

    private void OnRetryButtonClicked(object sender, EventArgs arguments) {
        _serviceAudio.PlayClipAudio(_effectCollectSound);
        _serviceScene.ChangeScene(new HunterGameScene(
            _serviceContent, _serviceAudio, _serviceInput, _serviceScene,
            _serviceUserInterface, _contextRender, _bridgesGum, _actionExit,
            _boundsScreen, _controllerGame, _logger, _profiler
        ));
    }

    private void OnQuitButtonClicked(object sender, EventArgs arguments) {
        _serviceScene.ChangeScene(new ModeSelectScene(
            _serviceContent, _serviceAudio, _serviceInput, _serviceScene,
            _serviceUserInterface, _contextRender, _bridgesGum, _actionExit,
            _boundsScreen, _controllerGame, _logger, _profiler
        ));
    }

    private void InitializeNewGame() {
        TwoDimensionalVector positionSlime = new TwoDimensionalVector(
            (_tilemap.Columns / 2) * _tilemap.ScaledTileWidth,
            (_tilemap.Rows / 2) * _tilemap.ScaledTileHeight
        );
        _slime.Initialize(positionSlime, _tilemap.ScaledTileWidth);
        _bat.RandomizeVelocity();
        PositionBatAwayFromSlime();
        _score = 0;
        _state = GameState.Playing;
        _ui.UpdateScoreText(_score);
        _ui.UpdateModeText("HUNTER");
        _ui.UpdateStatusText("SURVIVE");
        _ui.HidePausePanel();
        _ui.HideGameOverPanel();
        _saturation = 1.0f;
        _spanScore = TimeSpan.Zero;
    }

    public override void Update(FrameTime timeFrame) {
        if (_state != GameState.Playing) {
            _saturation = Math.Max(0.0f, _saturation - FADE_SPEED);
            if (_state == GameState.GameOver) {
                return;
            }
        }
        if (_controllerGame.Pause()) {
            TogglePause();
        }
        if (_state == GameState.Paused) {
            return;
        }
        _slime.SetDirection(_controllerGame.GetDirection());
        _slime.Update(timeFrame);
        Circle boundsSlime = _slime.GetBounds();
        Circle boundsBat = _bat.GetBounds();
        TwoDimensionalVector positionSlime =
            new TwoDimensionalVector(boundsSlime.X, boundsSlime.Y);
        TwoDimensionalVector positionBat =
            new TwoDimensionalVector(boundsBat.X, boundsBat.Y);
        TwoDimensionalVector directionHunt =
            TwoDimensionalVector.Normalize(positionSlime - positionBat);
        _bat.Velocity = directionHunt * Math.Min(7.5f, 3.5f + _score / 400.0f);
        _bat.Update(timeFrame);
        _spanScore += timeFrame.DeltaTimeSpan;
        while (_spanScore >= TimeSpan.FromSeconds(1.0)) {
            _spanScore -= TimeSpan.FromSeconds(1.0);
            _score += 10;
            _ui.UpdateScoreText(_score);
        }
        CollisionChecks();
    }

    private void CollisionChecks() {
        Circle boundsSlime = _slime.GetBounds();
        Circle boundsBat = _bat.GetBounds();
        if (boundsSlime.Intersects(boundsBat) || IsOutsideRoom(boundsSlime)) {
            GameOver();
            return;
        }
        if (boundsBat.Top < _boundsRoom.Top) {
            _bat.Position = new TwoDimensionalVector(_bat.Position.X, _boundsRoom.Top);
        }
        else if (boundsBat.Bottom > _boundsRoom.Bottom) {
            _bat.Position = new TwoDimensionalVector(
                _bat.Position.X, _boundsRoom.Bottom - boundsBat.Radius * 2
            );
        }
        if (boundsBat.Left < _boundsRoom.Left) {
            _bat.Position = new TwoDimensionalVector(_boundsRoom.Left, _bat.Position.Y);
        }
        else if (boundsBat.Right > _boundsRoom.Right) {
            _bat.Position = new TwoDimensionalVector(
                _boundsRoom.Right - boundsBat.Radius * 2, _bat.Position.Y
            );
        }
    }

    private bool IsOutsideRoom(Circle bounds) {
        return
            bounds.Top < _boundsRoom.Top ||
            bounds.Bottom > _boundsRoom.Bottom ||
            bounds.Left < _boundsRoom.Left ||
            bounds.Right > _boundsRoom.Right;
    }

    private void BounceBat(Bat bat, Circle boundsBat) {
        if (boundsBat.Top < _boundsRoom.Top) {
            bat.Bounce(TwoDimensionalVector.UnitY);
        }
        else if (boundsBat.Bottom > _boundsRoom.Bottom) {
            bat.Bounce(-TwoDimensionalVector.UnitY);
        }
        if (boundsBat.Left < _boundsRoom.Left) {
            bat.Bounce(TwoDimensionalVector.UnitX);
        }
        else if (boundsBat.Right > _boundsRoom.Right) {
            bat.Bounce(-TwoDimensionalVector.UnitX);
        }
    }

    private void PositionBatAwayFromSlime() {
        TwoDimensionalVector positionRoomCenter = new TwoDimensionalVector(
            _boundsRoom.X + _boundsRoom.Width * 0.5f,
            _boundsRoom.Y + _boundsRoom.Height * 0.5f
        );
        Circle boundsSlime = _slime.GetBounds();
        TwoDimensionalVector positionSlimeCenter = new TwoDimensionalVector(
            boundsSlime.X,
            boundsSlime.Y
        );
        TwoDimensionalVector vectorToSlimeCenter =
            positionSlimeCenter - positionRoomCenter;
        Circle boundsBat = _bat.GetBounds();
        int amountPadding = boundsBat.Radius * 2;
        TwoDimensionalVector positionNewBat = TwoDimensionalVector.Zero;
        if (Math.Abs(vectorToSlimeCenter.X) > Math.Abs(vectorToSlimeCenter.Y)) {
            positionNewBat.Y = Random.Shared.Next(
                _boundsRoom.Top + amountPadding,
                _boundsRoom.Bottom - amountPadding
            );
            if (vectorToSlimeCenter.X > 0.0f) {
                positionNewBat.X = _boundsRoom.Left + amountPadding;
            }
            else {
                positionNewBat.X = _boundsRoom.Right - amountPadding * 2;
            }
        }
        else {
            positionNewBat.X = Random.Shared.Next(
                _boundsRoom.Left + amountPadding,
                _boundsRoom.Right - amountPadding
            );
            if (vectorToSlimeCenter.Y > 0.0f) {
                positionNewBat.Y = _boundsRoom.Top + amountPadding;
            }
            else {
                positionNewBat.Y = _boundsRoom.Bottom - amountPadding * 2;
            }
        }
        _bat.Position = positionNewBat;
    }

   private void OnSlimeBodyCollision(object sender, EventArgs arguments) {
        GameOver();
    }

    private void TogglePause() {
        if (_state == GameState.Paused) {
            _ui.HidePausePanel();
            _state = GameState.Playing;
        }
        else {
            _ui.ShowPausePanel();
            _state = GameState.Paused;
            _saturation = 1.0f;
        }
    }

    private void GameOver() {
        _ui.ShowGameOverPanel();
        _state = GameState.GameOver;
        _saturation = 1.0f;
    }

    public override void Draw(FrameTime timeFrame) {
        _contextRender.Clear(Color.CornflowerBlue);
        Optional<SamplerState> sampler =
            new Optional<SamplerState>(SamplerState.PointClamp);
        if (_state != GameState.Playing) {
            _effectGrayscale.SetParameter("Saturation", _saturation);
            _contextRender.Begin(
                sampler,
                default(Optional<BlendState>),
                new Optional<IEffect>(_effectGrayscale)
            );
        }
        else {
            _contextRender.Begin(sampler);
        }
        _tilemap.Draw(_contextRender);
        _slime.Draw(_contextRender);
        _bat.Draw(_contextRender);
        _contextRender.End();
    }

    protected override void Dispose(bool flagDisposing) {
        if (flagDisposing) {
            if (_ui != null) {
                _ui.ResumeButtonClick -= OnResumeButtonClicked;
                _ui.RetryButtonClick -= OnRetryButtonClicked;
                _ui.QuitButtonClick -= OnQuitButtonClicked;
                _ui.Dispose();
                _ui = null;
            }
            if (_slime != null) {
                _slime.BodyCollision -= OnSlimeBodyCollision;
            }
            if (_tilemap != null) {
                _tilemap.Dispose();
            }
            if (_atlas != null) {
                _atlas.Dispose();
            }
        }
        base.Dispose(flagDisposing);
    }
}
