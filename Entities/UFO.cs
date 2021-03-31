// Celeste.FlingBird
using System;
using System.Collections;
using System.Collections.Generic;
using Celeste;
using Microsoft.Xna.Framework;
using Monocle;

public class UFO : Entity {
    private enum States {
        Wait,
        Fling,
        Move,
        WaitForLightningClear,
        Leaving
    }

    public static ParticleType P_Feather;

    public const float SkipDist = 100f;

    public static readonly Vector2 FlingSpeed = new Vector2(380f, -100f);

    private Vector2 spriteOffset = new Vector2(0f, 8f);

    private Sprite sprite;

    private States state;

    private Vector2 flingSpeed;

    private Vector2 flingTargetSpeed;

    private float flingAccel;

    private Color trailColor = Calc.HexToColor("639bff");

    private EntityData entityData;

    private SoundSource moveSfx;

    private int segmentIndex;

    public List<Vector2[]> NodeSegments;

    public List<bool> SegmentsWaiting;

    public bool LightningRemoved;

    public UFO(Vector2[] nodes, bool skippable)
        : base(nodes[0]) {
        base.Depth = -1;
        Add(sprite = GFX.SpriteBank.Create("bird"));
        sprite.Play("hover");
        sprite.Scale.X = -1f;
        sprite.Position = spriteOffset;
        sprite.OnFrameChange = delegate {
            BirdNPC.FlapSfxCheck(sprite);
        };
        base.Collider = new Circle(16f);
        Add(new PlayerCollider(OnPlayer));
        Add(moveSfx = new SoundSource());
        NodeSegments = new List<Vector2[]>();
        NodeSegments.Add(nodes);
        SegmentsWaiting = new List<bool>();
        SegmentsWaiting.Add(skippable);
        Add(new TransitionListener {
            OnOut = delegate (float t) {
                sprite.Color = Color.White * (1f - Calc.Map(t, 0f, 0.4f));
            }
        });
    }

    public UFO(EntityData data, Vector2 levelOffset)
        : this(data.NodesWithPosition(levelOffset), data.Bool("waiting")) {
        entityData = data;
    }

    public override void Awake(Scene scene) {
        base.Awake(scene);
        List<UFO> list = base.Scene.Entities.FindAll<UFO>();
        for (int num = list.Count - 1; num >= 0; num--) {
            if (list[num].entityData.Level.Name != entityData.Level.Name) {
                list.RemoveAt(num);
            }
        }
        list.Sort((UFO a, UFO b) => Math.Sign(a.X - b.X));
        if (list[0] == this) {
            for (int i = 1; i < list.Count; i++) {
                NodeSegments.Add(list[i].NodeSegments[0]);
                SegmentsWaiting.Add(list[i].SegmentsWaiting[0]);
                list[i].RemoveSelf();
            }
        }
        if (SegmentsWaiting[0]) {
            sprite.Play("hoverStressed");
            sprite.Scale.X = 1f;
        }
        Player entity = scene.Tracker.GetEntity<Player>();
        if (entity != null && entity.X > base.X) {
            RemoveSelf();
        }
    }

    private void Skip() {
        state = States.Move;
        Add(new Coroutine(MoveRoutine()));
    }

    private void OnPlayer(Player player) {
        if (state == States.Wait && player.DoFlingBird(this)) {
            flingSpeed = player.Speed * 0.4f;
            flingSpeed.Y = 120f;
            flingTargetSpeed = Vector2.Zero;
            flingAccel = 1000f;
            player.Speed = Vector2.Zero;
            state = States.Fling;
            Add(new Coroutine(DoFlingRoutine(player)));
            Audio.Play("event:/new_content/game/10_farewell/bird_throw", base.Center);
        }
    }

    public override void Update() {
        base.Update();
        if (state != 0) {
            sprite.Position = Calc.Approach(sprite.Position, spriteOffset, 32f * Engine.DeltaTime);
        }
        switch (state) {
            case States.Wait: {
                    Player entity = base.Scene.Tracker.GetEntity<Player>();
                    if (entity != null && entity.X - base.X >= 100f) {
                        Skip();
                    } else if (SegmentsWaiting[segmentIndex] && LightningRemoved) {
                        Skip();
                    } else if (entity != null) {
                        float scaleFactor = Calc.ClampedMap((entity.Center - Position).Length(), 16f, 64f, 12f, 0f);
                        Vector2 value = (entity.Center - Position).SafeNormalize();
                        sprite.Position = Calc.Approach(sprite.Position, spriteOffset + value * scaleFactor, 32f * Engine.DeltaTime);
                    }
                    break;
                }
            case States.Fling:
                if (flingAccel > 0f) {
                    flingSpeed = Calc.Approach(flingSpeed, flingTargetSpeed, flingAccel * Engine.DeltaTime);
                }
                Position += flingSpeed * Engine.DeltaTime;
                break;
            case States.WaitForLightningClear:
                if (base.Scene.Entities.FindFirst<Lightning>() == null || base.X > (float) (base.Scene as Level).Bounds.Right) {
                    sprite.Scale.X = 1f;
                    state = States.Leaving;
                    Add(new Coroutine(LeaveRoutine()));
                }
                break;
            case States.Move:
                break;
        }
    }

    private IEnumerator DoFlingRoutine(Player player) {
        Level level = Scene as Level;
        Vector2 position = level.Camera.Position;
        Vector2 screenSpaceFocusPoint = player.Position - position;
        screenSpaceFocusPoint.X = Calc.Clamp(screenSpaceFocusPoint.X, 145f, 215f);
        screenSpaceFocusPoint.Y = Calc.Clamp(screenSpaceFocusPoint.Y, 85f, 95f);
        Add(new Coroutine(level.ZoomTo(screenSpaceFocusPoint, 1.1f, 0.2f)));
        Engine.TimeRate = 0.8f;
        Input.Rumble(RumbleStrength.Light, RumbleLength.Medium);
        while (flingSpeed != Vector2.Zero) {
            yield return null;
        }
        sprite.Play("throw");
        sprite.Scale.X = 1f;
        flingSpeed = new Vector2(-140f, 140f);
        flingTargetSpeed = Vector2.Zero;
        flingAccel = 1400f;
        yield return 0.1f;
        Celeste.Celeste.Freeze(0.05f);
        flingTargetSpeed = FlingSpeed;
        flingAccel = 6000f;
        yield return 0.1f;
        Input.Rumble(RumbleStrength.Strong, RumbleLength.Medium);
        Engine.TimeRate = 1f;
        level.Shake();
        Add(new Coroutine(level.ZoomBack(0.1f)));
        player.FinishFlingBird();
        flingTargetSpeed = Vector2.Zero;
        flingAccel = 4000f;
        yield return 0.3f;
        Add(new Coroutine(MoveRoutine()));
    }

    private IEnumerator MoveRoutine() {
        state = States.Move;
        sprite.Play("fly");
        sprite.Scale.X = 1f;
        moveSfx.Play("event:/new_content/game/10_farewell/bird_relocate");
        for (int nodeIndex = 1; nodeIndex < NodeSegments[segmentIndex].Length - 1; nodeIndex += 2) {
            Vector2 position = Position;
            Vector2 anchor = NodeSegments[segmentIndex][nodeIndex];
            Vector2 to = NodeSegments[segmentIndex][nodeIndex + 1];
            yield return MoveOnCurve(position, anchor, to);
        }
        segmentIndex++;
        bool atEnding = segmentIndex >= NodeSegments.Count;
        if (!atEnding) {
            Vector2 position2 = Position;
            Vector2 anchor2 = NodeSegments[segmentIndex - 1][NodeSegments[segmentIndex - 1].Length - 1];
            Vector2 to2 = NodeSegments[segmentIndex][0];
            yield return MoveOnCurve(position2, anchor2, to2);
        }
        sprite.Rotation = 0f;
        sprite.Scale = Vector2.One;
        if (atEnding) {
            sprite.Play("hoverStressed");
            sprite.Scale.X = 1f;
            state = States.WaitForLightningClear;
            yield break;
        }
        if (SegmentsWaiting[segmentIndex]) {
            sprite.Play("hoverStressed");
        } else {
            sprite.Play("hover");
        }
        sprite.Scale.X = -1f;
        state = States.Wait;
    }

    private IEnumerator LeaveRoutine() {
        sprite.Scale.X = 1f;
        sprite.Play("fly");
        Vector2 vector = new Vector2((Scene as Level).Bounds.Right + 32, Y);
        yield return MoveOnCurve(Position, (Position + vector) * 0.5f - Vector2.UnitY * 12f, vector);
        RemoveSelf();
    }

    private IEnumerator MoveOnCurve(Vector2 from, Vector2 anchor, Vector2 to) {
        SimpleCurve curve = new SimpleCurve(from, to, anchor);
        float duration = curve.GetLengthParametric(32) / 500f;
        _ = from;
        Vector2 was = from;
        for (float t = 0.016f; t <= 1f; t += Engine.DeltaTime / duration) {
            Position = curve.GetPoint(t).Floor();
            sprite.Rotation = Calc.Angle(curve.GetPoint(Math.Max(0f, t - 0.05f)), curve.GetPoint(Math.Min(1f, t + 0.05f)));
            sprite.Scale.X = 1.25f;
            sprite.Scale.Y = 0.7f;
            if ((was - Position).Length() > 32f) {
                TrailManager.Add(this, trailColor, 1f);
                was = Position;
            }
            yield return null;
        }
        Position = to;
    }

    public override void Render() {
        base.Render();
    }

    private void DrawLine(Vector2 a, Vector2 anchor, Vector2 b) {
        new SimpleCurve(a, b, anchor).Render(Color.Red, 32);
    }
}

