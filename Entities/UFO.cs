// Celeste.FlingBird
using System;
using System.Collections;
using System.Collections.Generic;
using Celeste;
using Microsoft.Xna.Framework;
using Monocle;
using Celeste.Mod.Entities;

[CustomEntity("SJ2021/UFO")]
public class UFO : Actor {
    private enum States {
        Wait,
        Fling,
        Move,
        Leaving
    }

    public static readonly Vector2 FlingSpeed = new Vector2(380f, -100f);

    private Image sprite;

    private States state;

    private Vector2 flingSpeed;

    private Vector2 flingTargetSpeed;

    private float flingAccel;

    private EntityData entityData;

    private SoundSource moveSfx;

    private int segmentIndex;

    public List<Vector2[]> NodeSegments;

    Player player;

    private Wiggler bounceWiggler;

    private Vector2 hitSpeed;

    float RaySizeX = 13f;

    float RaySizeY = 60f;

    public static System.Reflection.MethodInfo SpringBounce;


    public UFO(Vector2[] nodes) : base(nodes[0]) {
        base.Depth = -1;
        Add(sprite = new Image(GFX.Game["objects/StrawberryJam2021/UFO/UFO"]));
        sprite.CenterOrigin();
        Collider = new Hitbox(24f, 24f, -12f, -12f);
        Add(new PlayerCollider(OnPlayer));
        Add(moveSfx = new SoundSource());
        NodeSegments = new List<Vector2[]>();
        NodeSegments.Add(nodes);
        bounceWiggler = Wiggler.Create(0.6f, 2.5f, delegate (float v) {
            sprite.Rotation = v * 20f * ((float) Math.PI / 180f);
        });
        Add(bounceWiggler);

    }

    public UFO(EntityData data, Vector2 levelOffset) : this(data.NodesWithPosition(levelOffset)) {
        entityData = data;
        RaySizeX = data.Int("RaySizeX");
        RaySizeY = data.Int("RaySizeY");
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
                list[i].RemoveSelf();
            }
        }
        player = scene.Tracker.GetEntity<Player>();
        if (player != null && player.X > base.X) {
            RemoveSelf();
        }
    }

    private void OnCollideV(CollisionData data) {
        if (!(data.Direction.Y > 0f)) {
            return;
        }
        for (int i = -1; i <= 1; i += 2) {
            for (int j = 1; j <= 2; j++) {
                Vector2 vector = Position + Vector2.UnitX * j * i;
                if (!CollideCheck<Solid>(vector) && !OnGround(vector)) {
                    Position = vector;
                    return;
                }
            }
        }
        hitSpeed.Y *= -0.2f;
    }

    private void OnPlayer(Player player) {
        if (state == States.Wait) {
            bounceWiggler.Start();
            player.Bounce(base.Top);
            GotoHit(player.Center);
        }
    }

    private void GotoHit(Vector2 from) {
        hitSpeed = Vector2.UnitY * 200f;
        bounceWiggler.Start();
        Audio.Play("event:/new_content/game/10_farewell/puffer_boop", Position);
    }

    public override void Update() {
        base.Update();
        switch (state) {
            case States.Wait: 
                Player entity = base.Scene.Tracker.GetEntity<Player>();
                if (entity != null && entity.X - base.X >= 100f) {
                    Skip();
                } else if (entity != null) {
                    float scaleFactor = Calc.ClampedMap((entity.Center - Position).Length(), 16f, 64f, 12f, 0f);
                    Vector2 value = (entity.Center - Position).SafeNormalize();
                }
                break;
            case States.Fling:
                if (flingAccel > 0f) {
                    flingSpeed = Calc.Approach(flingSpeed, flingTargetSpeed, flingAccel * Engine.DeltaTime);
                }
                Position += flingSpeed * Engine.DeltaTime;
                break;
            case States.Move:
                break;
        }

        MoveV(hitSpeed.Y * Engine.DeltaTime, OnCollideV);
        MoveH(hitSpeed.X * Engine.DeltaTime, OnCollideV);
        hitSpeed.X = Calc.Approach(hitSpeed.X, 0f, 150f * Engine.DeltaTime);
        hitSpeed = Calc.Approach(hitSpeed, Vector2.Zero, 320f * Engine.DeltaTime);


        if (CheckIfInRay(player.Position, player.Bottom, player.Top, false, null)) {
            flingSpeed = player.Speed * 0.4f;
            flingSpeed.Y = 120f;
            flingTargetSpeed = Vector2.Zero;
            flingAccel = 1000f;
            player.Speed = Vector2.Zero;
            state = States.Fling;
            Add(new Coroutine(DoFlingRoutine(player)));
            Audio.Play("event:/new_content/game/10_farewell/bird_throw", base.Center);
        } else {
            foreach (Glider CollidingGlider in Scene.Entities.FindAll<Glider>()) {
                if (CheckIfInRay(CollidingGlider.Position, CollidingGlider.Bottom, CollidingGlider.Top, true, CollidingGlider)) {
                    flingSpeed = CollidingGlider.Speed * 0.4f;
                    flingSpeed.Y = 120f;
                    flingTargetSpeed = Vector2.Zero;
                    flingAccel = 1000f;
                    CollidingGlider.Speed = Vector2.Zero;
                    state = States.Fling;
                    Add(new Coroutine(DoFlingRoutineJelly(CollidingGlider)));
                    Audio.Play("event:/new_content/game/10_farewell/bird_throw", base.Center);
                }
            }
        }

        Spring CollidingSpring = (Spring) Collide.First(this, Scene.Entities.FindAll<Spring>());

        if (CollidingSpring != null && state == States.Wait) {
            switch (CollidingSpring.Orientation) {
                default:
                    if (hitSpeed.Y >= 0f) {
                        hitSpeed = 224f * -Vector2.UnitY;
                        MoveTowardsX(CollidingSpring.CenterX, 4f);
                        bounceWiggler.Start();
                    }
                    break;
                case Spring.Orientations.WallLeft:
                    if (hitSpeed.X <= 60f) {
                        hitSpeed = 280f * Vector2.UnitX;
                        MoveTowardsY(CollidingSpring.CenterY, 4f);
                        bounceWiggler.Start();
                    }
                    break;
                case Spring.Orientations.WallRight:
                    if (hitSpeed.X >= -60f) {
                        hitSpeed = 280f * -Vector2.UnitX;
                        MoveTowardsY(CollidingSpring.CenterY, 4f);
                        bounceWiggler.Start();
                    }
                    break;
            }
            SpringBounce.Invoke(CollidingSpring, null);
            
        }
    }

    bool CheckIfInRay(Vector2 EntityPosition, float EntityBottom, float EntityTop, bool IsJelly, Glider CollidingGlider) {
        if(EntityPosition.X >= Position.X - RaySizeX && EntityPosition.X <= Position.X + RaySizeX && EntityTop < Position.Y + RaySizeY + 12 && EntityBottom > Top + 5f && state == States.Wait) {
            if (!IsJelly) {
                return true;
            }
            if(player.Holding != null && player.Holding.Entity == CollidingGlider && CheckIfInRay(player.Position, player.Bottom, player.Top, false, null)) {
                return true;
            }
        }
        return false;
    }

    private void Skip() {
        state = States.Move;
        Add(new Coroutine(MoveRoutine()));
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

    private IEnumerator DoFlingRoutineJelly(Glider Jelly) {
        Level level = Scene as Level;
        Vector2 position = level.Camera.Position;
        Engine.TimeRate = 0.8f;
        Input.Rumble(RumbleStrength.Light, RumbleLength.Medium);
        while (flingSpeed != Vector2.Zero) {
            yield return null;
        }
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
        flingTargetSpeed = Vector2.Zero;
        flingAccel = 4000f;
        Jelly.Speed = FlingBird.FlingSpeed/1.5f;
        yield return 0.3f;
        Add(new Coroutine(MoveRoutine()));
    }

    private IEnumerator MoveRoutine() {
        state = States.Move;
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
            sprite.Scale.X = 1f;
            state = States.Leaving;
            Add(new Coroutine(LeaveRoutine()));
            yield break;
        }
        sprite.Scale.X = -1f;
        state = States.Wait;
    }

    private IEnumerator LeaveRoutine() {
        sprite.Scale.X = 1f;
        Vector2 vector = new Vector2((Scene as Level).Bounds.Right + 32, Y);
        yield return MoveOnCurve(Position, (Position + vector) * 0.5f - Vector2.UnitY * 12f, vector);
        RemoveSelf();
    }

    private IEnumerator MoveOnCurve(Vector2 from, Vector2 anchor, Vector2 to) {
        SimpleCurve curve = new SimpleCurve(from, to, anchor);
        float duration = curve.GetLengthParametric(32) / 500f;
        Vector2 was = from;
        for (float t = 0.016f; t <= 1f; t += Engine.DeltaTime / duration) {
            Position = curve.GetPoint(t).Floor();
            sprite.Scale.X = 1.25f;
            sprite.Scale.Y = 0.7f;
            if ((was - Position).Length() > 32f) {
                was = Position;
            }
            yield return null;
        }
        Position = to;
    }

    public override void Render() {
        base.Render();
        if (state == States.Wait) {
            Draw.Rect(Position.X - RaySizeX, Position.Y + 12, RaySizeX * 2, RaySizeY, Color.White);
        }
    }

    public static void Load() {
        SpringBounce = typeof(Spring).GetMethod("BounceAnimate", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
    }
}
