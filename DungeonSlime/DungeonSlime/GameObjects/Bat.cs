using System;
using MonoGameLibrary.Core.Primitives;
using MonoGameLibrary.Core.Time;
using MonoGameLibrary.Extensions.Audio;
using MonoGameLibrary.Extensions.Graphics;

namespace DungeonSlime.GameObjects;

public sealed class Bat {
    private const float MOVEMENT_SPEED = 5.0f;
    private TwoDimensionalVector _velocity;
    private readonly AnimatedSprite _spriteAnimated;
    private readonly IClipAudio _effectBounceSound;
    private readonly IAudioService _serviceAudio;

    public TwoDimensionalVector Position { get; set; }

    public Bat(
        AnimatedSprite spriteAnimated,
        IClipAudio effectBounceSound,
        IAudioService serviceAudio
    ) {
        if (spriteAnimated == null) {
            throw new ArgumentNullException(nameof(spriteAnimated));
        }
        if (effectBounceSound == null) {
            throw new ArgumentNullException(nameof(effectBounceSound));
        }
        if (serviceAudio == null) {
            throw new ArgumentNullException(nameof(serviceAudio));
        }
        _spriteAnimated = spriteAnimated;
        _effectBounceSound = effectBounceSound;
        _serviceAudio = serviceAudio;
        _velocity = TwoDimensionalVector.Zero;
        Position = TwoDimensionalVector.Zero;
    }

    public void RandomizeVelocity() {
        float angle = (float)(Random.Shared.NextDouble() * Math.PI * 2.0);
        TwoDimensionalVector direction = new TwoDimensionalVector(
            (float)Math.Cos(angle),
            (float)Math.Sin(angle)
        );
        _velocity = direction * MOVEMENT_SPEED;
    }

    public void Bounce(TwoDimensionalVector normal) {
        TwoDimensionalVector positionNew = Position;
        if (normal.X != 0.0f) {
            positionNew.X += normal.X * (_spriteAnimated.Width * 0.1f);
        }
        if (normal.Y != 0.0f) {
            positionNew.Y += normal.Y * (_spriteAnimated.Height * 0.1f);
        }
        Position = positionNew;
        normal = normal.Normalize();
        _velocity = TwoDimensionalVector.Reflect(_velocity, normal);
        _serviceAudio.PlayClipAudio(_effectBounceSound);
    }

    public Circle GetBounds() {
        int x = (int)(Position.X + _spriteAnimated.Width * 0.5f);
        int y = (int)(Position.Y + _spriteAnimated.Height * 0.5f);
        int radius = (int)(_spriteAnimated.Width * 0.25f);
        return new Circle(x, y, radius);
    }

    public void Update(FrameTime timeFrame) {
        _spriteAnimated.Update(timeFrame);
        Position += _velocity;
    }

    public void Draw(IRenderContext contextRender) {
        if (contextRender == null) {
            throw new ArgumentNullException(nameof(contextRender));
        }
        _spriteAnimated.Draw(contextRender, Position);
    }
}
