using System;
using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Forms.Controls;
using Gum.Forms.DefaultVisuals.V3;
using Gum.Graphics.Animation;
using Gum.Managers;
using MonoGameGum.GueDeriving;
using MonoGameLibrary.Adapters.Gum.MonoGame;
using MonoGameLibrary.Core.Primitives;
using MonoGameLibrary.Extensions.Graphics;
using MonoGameLibrary.Extensions.Input;

namespace DungeonSlime.UI;

internal sealed class AnimatedButton : Button, IDisposable {
    private readonly GumBridgesService _bridgesGum;
    private bool _flagDisposed;

    public AnimatedButton(TextureAtlas atlas, GumBridgesService bridgesGum) {
        if (atlas == null) {
            throw new ArgumentNullException(nameof(atlas));
        }
        if (bridgesGum == null) {
            throw new ArgumentNullException(nameof(bridgesGum));
        }
        _bridgesGum = bridgesGum;

        ButtonVisual visualButton = (ButtonVisual)Visual;
        visualButton.Height = 14.0f;
        visualButton.HeightUnits = DimensionUnitType.Absolute;
        visualButton.Width = 21.0f;
        visualButton.WidthUnits = DimensionUnitType.RelativeToChildren;

        NineSliceRuntime runtimeBackground = visualButton.Background;
        runtimeBackground.SetTexture(atlas.Texture, _bridgesGum);
        runtimeBackground.TextureAddress = TextureAddress.Custom;
        runtimeBackground.SetColor(Color.White, _bridgesGum);

        TextRuntime runtimeText = visualButton.TextInstance;
        runtimeText.Text = "START";
        runtimeText.Blue = 130;
        runtimeText.Green = 86;
        runtimeText.Red = 70;
        runtimeText.UseCustomFont = true;
        runtimeText.CustomFontFile = "fonts/04b_30.fnt";
        runtimeText.FontScale = 0.25f;
        runtimeText.Anchor(Gum.Wireframe.Anchor.Center);
        runtimeText.Width = 0.0f;
        runtimeText.WidthUnits = DimensionUnitType.RelativeToChildren;

        TextureRegion regionUnfocused = atlas.GetRegion("unfocused-button");
        AnimationChain chainUnfocused = new AnimationChain();
        chainUnfocused.Name = nameof(chainUnfocused);
        chainUnfocused.Add(GumGraphicsExtensions.CreateAnimationFrame(
            regionUnfocused,
            0.3f,
            _bridgesGum
        ));

        Animation animationFocused = atlas.GetAnimation("focused-button-animation");
        AnimationChain chainFocused = new AnimationChain();
        chainFocused.Name = nameof(chainFocused);
        foreach (TextureRegion region in animationFocused.Frames) {
            chainFocused.Add(GumGraphicsExtensions.CreateAnimationFrame(
                region,
                (float)animationFocused.Delay.TotalSeconds,
                _bridgesGum
            ));
        }

        runtimeBackground.AnimationChains = new AnimationChainList {
            chainUnfocused,
            chainFocused
        };

        visualButton.ButtonCategory.ResetAllStates();
        StateSave stateEnabled = visualButton.States.Enabled;
        stateEnabled.Apply = delegate {
            runtimeBackground.CurrentChainName = chainUnfocused.Name;
        };
        StateSave stateFocused = visualButton.States.Focused;
        stateFocused.Apply = delegate {
            runtimeBackground.CurrentChainName = chainFocused.Name;
            runtimeBackground.Animate = true;
        };
        visualButton.States.HighlightedFocused.Apply = stateFocused.Apply;
        visualButton.States.Highlighted.Apply = stateEnabled.Apply;

        KeyDown += HandleKeyDown;
        visualButton.RollOn += HandleRollOn;
    }

    private void HandleKeyDown(object sender, KeyEventArgs arguments) {
        if (arguments.IsKey(KeyCode.Left, _bridgesGum)) {
            HandleTab(TabDirection.Up, loop: true);
        }
        if (arguments.IsKey(KeyCode.Right, _bridgesGum)) {
            HandleTab(TabDirection.Down, loop: true);
        }
    }

    private void HandleRollOn(object sender, EventArgs arguments) {
        IsFocused = true;
    }

    public void Dispose() {
        if (_flagDisposed) {
            return;
        }
        KeyDown -= HandleKeyDown;
        if (Visual != null) {
            Visual.RollOn -= HandleRollOn;
        }
        _flagDisposed = true;
        GC.SuppressFinalize(this);
    }
}
