using System;
using System.Collections.Generic;
using MonoGameLibrary.Core.Primitives;
using MonoGameLibrary.Core.Time;
using MonoGameLibrary.Extensions.Graphics;

namespace DungeonSlime.GameObjects;

public sealed class Slime {
    private static readonly TimeSpan somv_timeMovementInterval = TimeSpan.FromMilliseconds(200);
    private const int MAX_BUFFER_SIZE = 2;
    private TimeSpan _spanMovementTime;
    private float _progressMovement;
    private TwoDimensionalVector _directionNext;
    private float _stride;
    private readonly List<SlimeSegment> _listSegment;
    private readonly AnimatedSprite _spriteAnimated;
    private readonly Queue<TwoDimensionalVector> _queueInput;

    public event EventHandler BodyCollision;

    public Color Color {
        get { return _spriteAnimated.Color; }
        set { _spriteAnimated.Color = value; }
    }

    public Slime(AnimatedSprite spriteAnimated) {
        if (spriteAnimated == null) {
            throw new ArgumentNullException(nameof(spriteAnimated));
        }
        _spriteAnimated = spriteAnimated;
        _listSegment = new List<SlimeSegment>();
        _queueInput = new Queue<TwoDimensionalVector>(MAX_BUFFER_SIZE);
        _spanMovementTime = TimeSpan.Zero;
        _directionNext = TwoDimensionalVector.Zero;
    }

    public void Initialize(TwoDimensionalVector positionStarting, float stride) {
        Initialize(positionStarting, stride, TwoDimensionalVector.UnitX);
    }

    public void Initialize(
        TwoDimensionalVector positionStarting,
        float stride,
        TwoDimensionalVector directionInitial
    ) {
        if (stride <= 0.0f) {
            throw new ArgumentOutOfRangeException(nameof(stride));
        }
        if (directionInitial == TwoDimensionalVector.Zero) {
            throw new ArgumentException(
                "The initial direction cannot be zero.",
                nameof(directionInitial)
            );
        }
        _listSegment.Clear();
        _queueInput.Clear();
        _stride = stride;

        directionInitial = TwoDimensionalVector.Normalize(directionInitial);

        SlimeSegment segmentHead = new SlimeSegment();
        segmentHead.At = positionStarting;
        segmentHead.To = positionStarting + directionInitial * _stride;
        segmentHead.Direction = directionInitial;
        _listSegment.Add(segmentHead);
        _directionNext = segmentHead.Direction;
        _spanMovementTime = TimeSpan.Zero;
        _progressMovement = 0.0f;
    }

    public void SetDirection(TwoDimensionalVector direction) {
        if (direction == TwoDimensionalVector.Zero || _queueInput.Count >= MAX_BUFFER_SIZE) {
            return;
        }
        TwoDimensionalVector directionValidateAgainst;
        if (_queueInput.Count > 0) {
            TwoDimensionalVector[] inputs = _queueInput.ToArray();
            directionValidateAgainst = inputs[inputs.Length - 1];
        }
        else {
            directionValidateAgainst = _listSegment[0].Direction;
        }
        if (TwoDimensionalVector.Dot(direction, directionValidateAgainst) >= 0.0f) {
            _queueInput.Enqueue(direction);
        }
    }

    private void Move() {
        if (_queueInput.Count > 0) {
            _directionNext = _queueInput.Dequeue();
        }
        SlimeSegment segmentHead = _listSegment[0];
        segmentHead.Direction = _directionNext;
        segmentHead.At = segmentHead.To;
        segmentHead.To = segmentHead.At + segmentHead.Direction * _stride;
        _listSegment.Insert(0, segmentHead);
        _listSegment.RemoveAt(_listSegment.Count - 1);

        for (int index = 1; index < _listSegment.Count; index += 1) {
            if (segmentHead.At == _listSegment[index].At) {
                if (BodyCollision != null) {
                    BodyCollision.Invoke(this, EventArgs.Empty);
                }
                return;
            }
        }
    }

    public void Grow() {
        SlimeSegment segmentTail = _listSegment[_listSegment.Count - 1];
        SlimeSegment newTail = new SlimeSegment();
        newTail.At = segmentTail.To + segmentTail.ReverseDirection * _stride;
        newTail.To = segmentTail.At;
        newTail.Direction = TwoDimensionalVector.Normalize(segmentTail.At - newTail.At);
        _listSegment.Add(newTail);
    }

    public void Translate(TwoDimensionalVector offset) {
        for (int index = 0; index < _listSegment.Count; index += 1) {
            SlimeSegment segment = _listSegment[index];
            segment.At += offset;
            segment.To += offset;
            _listSegment[index] = segment;
        }
    }

    public void Update(FrameTime timeFrame) {
        _spriteAnimated.Update(timeFrame);
        _spanMovementTime += timeFrame.DeltaTimeSpan;
        if (_spanMovementTime >= somv_timeMovementInterval) {
            _spanMovementTime -= somv_timeMovementInterval;
            Move();
        }
        _progressMovement = (float)(_spanMovementTime.TotalSeconds / somv_timeMovementInterval.TotalSeconds);
    }

    public void Draw(IRenderContext contextRender) {
        if (contextRender == null) {
            throw new ArgumentNullException(nameof(contextRender));
        }
        foreach (SlimeSegment segment in _listSegment) {
            TwoDimensionalVector position = TwoDimensionalVector.Lerp(
                segment.At,
                segment.To,
                _progressMovement
            );
            _spriteAnimated.Draw(contextRender, position);
        }
    }

    public Circle GetBounds() {
        SlimeSegment segmentHead = _listSegment[0];
        TwoDimensionalVector position = TwoDimensionalVector.Lerp(
            segmentHead.At,
            segmentHead.To,
            _progressMovement
        );
        return new Circle(
            (int)(position.X + _spriteAnimated.Width * 0.5f),
            (int)(position.Y + _spriteAnimated.Height * 0.5f),
            (int)(_spriteAnimated.Width * 0.5f)
        );
    }
}
