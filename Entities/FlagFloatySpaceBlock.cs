using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using MonoMod.Utils;
using Monocle;
using System;
using System.Collections.Generic;

namespace Celeste.Mod.StrawberryJam2021.Entities {
    // Basically vanilla copy-paste, with a flag-toggled position changer
    [CustomEntity("SJ2021/FlagFloatySpaceBlock")]
    [Tracked]
    public class FlagFloatySpaceBlock : Solid {
        private TileGrid tiles;
        private char tileType;
        private string flag;
        private bool activated;
        private Vector2 node;
        private Vector2 startPosition;
        private Vector2 tweenStartPosition;
        private float moveTime;
        private Tween floatTween;

        private float sineWave;
        private float sinkTimer;
        private float yLerp;
        private float dashEase;
        private Vector2 dashDirection;

        private bool awake;
        private FlagFloatySpaceBlock master;
        private List<FlagFloatySpaceBlock> group;
        private List<JumpThru> jumpthrus;
        private Dictionary<Platform, Vector2> moves;
        private Point groupBoundsMin;
        private Point groupBoundsMax;

        public bool HasGroup {
            get;
            private set;
        }

        public bool MasterOfGroup {
            get;
            private set;
        }

        public FlagFloatySpaceBlock(EntityData data, Vector2 offset)
            : base(data.Position + offset, data.Width, data.Height, safe: true) {
            node = data.Nodes[0] + offset;
            startPosition = Position;
            moveTime = data.Float("moveTime", 1.0f);
            activated = false;
            tileType = data.Char("tiletype", '3');
            flag = data.Attr("flag");
            Depth = -9000;
            Add(new LightOcclude());
            SurfaceSoundIndex = SurfaceIndex.TileToIndex[tileType];
            if (!data.Bool("disableSpawnOffset")) {
                sineWave = Calc.Random.NextFloat((float) Math.PI * 2f);
            } else {
                sineWave = 0f;
            }
        }

        public override void Awake(Scene scene) {
            base.Awake(scene);
            awake = true;
            activated = SceneAs<Level>().Session.GetFlag(flag);
            if (!HasGroup) {
                MasterOfGroup = true;
                moves = new Dictionary<Platform, Vector2>();
                group = new List<FlagFloatySpaceBlock>();
                jumpthrus = new List<JumpThru>();
                groupBoundsMin = new Point((int) base.X, (int) base.Y);
                groupBoundsMax = new Point((int) base.Right, (int) base.Bottom);
                addToGroupAndFindChildren(this);
                Rectangle rectangle = new Rectangle(groupBoundsMin.X / 8, groupBoundsMin.Y / 8, (groupBoundsMax.X - groupBoundsMin.X) / 8 + 1, (groupBoundsMax.Y - groupBoundsMin.Y) / 8 + 1);
                VirtualMap<char> tilemap = new VirtualMap<char>(rectangle.Width, rectangle.Height, '0');
                foreach (FlagFloatySpaceBlock block in group) {
                    int startX = (int) (block.X / 8f) - rectangle.X;
                    int startY = (int) (block.Y / 8f) - rectangle.Y;
                    int widthTiles = (int) (block.Width / 8f);
                    int heightTiles = (int) (block.Height / 8f);
                    for (int i = startX; i < startX + widthTiles; i++) {
                        for (int j = startY; j < startY + heightTiles; j++) {
                            tilemap[i, j] = tileType;
                        }
                    }
                }
                tiles = GFX.FGAutotiler.GenerateMap(tilemap, new Autotiler.Behaviour {
                    EdgesExtend = false,
                    EdgesIgnoreOutOfLevel = false,
                    PaddingIgnoreOutOfLevel = false,
                }).TileGrid;
                tiles.Position = new Vector2(groupBoundsMin.X - X, groupBoundsMin.Y - Y);
                Add(tiles);

                floatTween = Tween.Create(Tween.TweenMode.Persist, Ease.QuadOut, moveTime);
                floatTween.OnUpdate = delegate (Tween t)
                {
                    Vector2 sineOffset = Vector2.UnitY * (float) Math.Sin(sineWave) * 4f;
                    Vector2 end = activated ? node + sineOffset : startPosition;
                    Vector2 target = Vector2.Lerp(tweenStartPosition, end, t.Eased);
                    Vector2 diff = target - Position;
                    foreach (JumpThru jp in jumpthrus) {
                        jp.MoveTo(jp.Position + diff);
                    }
                    foreach (FlagFloatySpaceBlock block in group) {
                        block.MoveTo(block.Position + diff);
                    }
                };
                floatTween.OnComplete = delegate (Tween t) {
                    if (moves is null) {
                        return;
                    }
                    Vector2 sineOffset = Vector2.UnitY * (float) Math.Sin(sineWave) * 4f;
                    foreach (JumpThru jp in jumpthrus) {
                        moves[jp] = jp.Position - sineOffset;
                    }
                    foreach (FlagFloatySpaceBlock block in group) {
                        moves[block] = block.Position - sineOffset;
                    }
                    yLerp = 0f;
                };
                Add(floatTween);
            }
            tryToInitPosition();
        }

        public override void OnStaticMoverTrigger(StaticMover sm) {
            if (sm.Entity is Spring) {
                switch ((sm.Entity as Spring).Orientation) {
                    case Spring.Orientations.Floor:
                        sinkTimer = 0.5f;
                        break;
                    case Spring.Orientations.WallLeft:
                        dashEase = 1f;
                        dashDirection = -Vector2.UnitX;
                        break;
                    case Spring.Orientations.WallRight:
                        dashEase = 1f;
                        dashDirection = Vector2.UnitX;
                        break;
                }
            }
        }

        private void tryToInitPosition() {
            if (MasterOfGroup) {
                foreach (FlagFloatySpaceBlock item in group) {
                    if (!item.awake) {
                        return;
                    }
                }
                if (activated) {
                    Vector2 diff = node - startPosition;
                    foreach (JumpThru jp in jumpthrus) {
                        jp.Position += diff;
                        moves[jp] = jp.Position;
                    }
                    foreach (FlagFloatySpaceBlock block in group) {
                        block.Position += diff;
                        moves[block] = block.Position;
                    }
                    MoveStaticMovers(diff);
                    moveToTarget();
                }
            }
            else {
                master.tryToInitPosition();
            }

        }

        private void addToGroupAndFindChildren(FlagFloatySpaceBlock from) {
            if (from.X < groupBoundsMin.X) {
                groupBoundsMin.X = (int) from.X;
            }
            if (from.Y < groupBoundsMin.Y) {
                groupBoundsMin.Y = (int) from.Y;
            }
            if (from.Right > groupBoundsMax.X) {
                groupBoundsMax.X = (int) from.Right;
            }
            if (from.Bottom > groupBoundsMax.Y) {
                groupBoundsMax.Y = (int) from.Bottom;
            }
            from.HasGroup = true;
            from.OnDashCollide = onDash;
            group.Add(from);
            moves.Add(from, from.Position);
            if (from != this) {
                from.master = this;
            }
            foreach (JumpThru jumpthru in Scene.CollideAll<JumpThru>(new Rectangle((int) from.X - 1, (int) from.Y, (int) from.Width + 2, (int) from.Height))) {
                if (!jumpthrus.Contains(jumpthru)) {
                    addJumpThru(jumpthru);
                }
            }
            foreach (JumpThru jumpthru in Scene.CollideAll<JumpThru>(new Rectangle((int) from.X, (int) from.Y - 1, (int) from.Width, (int) from.Height + 2))) {
                if (!jumpthrus.Contains(jumpthru)) {
                    addJumpThru(jumpthru);
                }
            }
            foreach (FlagFloatySpaceBlock block in Scene.Tracker.GetEntities<FlagFloatySpaceBlock>()) {
                if (!block.HasGroup && block.tileType == tileType && (Scene.CollideCheck(new Rectangle((int) from.X - 1, (int) from.Y, (int) from.Width + 2, (int) from.Height), block)
                    || Scene.CollideCheck(new Rectangle((int) from.X, (int) from.Y - 1, (int) from.Width, (int) from.Height + 2), block))) {

                    addToGroupAndFindChildren(block);
                }
            }
        }

        private void addJumpThru(JumpThru jp) {
            jp.OnDashCollide = onDash;
            jumpthrus.Add(jp);
            moves.Add(jp, jp.Position);
            foreach (FlagFloatySpaceBlock block in Scene.Tracker.GetEntities<FlagFloatySpaceBlock>()) {
                if (!block.HasGroup && block.tileType == tileType && Scene.CollideCheck(new Rectangle((int) jp.X - 1, (int) jp.Y, (int) jp.Width + 2, (int) jp.Height), block)) {
                    addToGroupAndFindChildren(block);
                }
            }
        }

        private DashCollisionResults onDash(Player player, Vector2 direction) {
            if (MasterOfGroup && dashEase <= 0.2f && activated && !floatTween.Active) {
                dashEase = 1f;
                dashDirection = direction;
            }
            return DashCollisionResults.NormalOverride;
        }

        public override void Update() {
            base.Update();
            if (MasterOfGroup) {
                if (activated != SceneAs<Level>().Session.GetFlag(flag)) {
                    activated = !activated;
                    tweenStartPosition = Position;
                    if (floatTween.Active) {
                        floatTween.Stop();
                        floatTween.Start(moveTime - floatTween.TimeLeft);
                    } else {
                        floatTween.Start(moveTime);
                    }
                }

                if (!activated || floatTween.Active) {
                    return;
                }

                bool blockHasPlayerOnIt = false;
                foreach (FlagFloatySpaceBlock block in group) {
                    if (block.HasPlayerRider()) {
                        blockHasPlayerOnIt = true;
                        break;
                    }
                }
                if (!blockHasPlayerOnIt) {
                    foreach (JumpThru jumpthru in jumpthrus) {
                        if (jumpthru.HasPlayerRider()) {
                            blockHasPlayerOnIt = true;
                            break;
                        }
                    }
                }
                if (blockHasPlayerOnIt) {
                    sinkTimer = 0.3f;
                } else if (sinkTimer > 0f) {
                    sinkTimer -= Engine.DeltaTime;
                }
                if (sinkTimer > 0f) {
                    yLerp = Calc.Approach(yLerp, 1f, 1f * Engine.DeltaTime);
                } else {
                    yLerp = Calc.Approach(yLerp, 0f, 1f * Engine.DeltaTime);
                }
                sineWave += Engine.DeltaTime;
                dashEase = Calc.Approach(dashEase, 0f, Engine.DeltaTime * 1.5f);
                moveToTarget();
            }
            LiftSpeed = Vector2.Zero;
        }

        private void moveToTarget() {
            float sine = (float) Math.Sin(sineWave) * 4f;
            Vector2 displacement = Calc.YoYo(Ease.QuadIn(dashEase)) * dashDirection * 8f;
            for (int i = 0; i < 2; i++) {
                foreach (KeyValuePair<Platform, Vector2> move in moves) {
                    Platform platform = move.Key;
                    bool hasPlayer = false;
                    JumpThru jumpThru = platform as JumpThru;
                    Solid solid = platform as Solid;
                    if ((jumpThru != null && jumpThru.HasRider()) || (solid?.HasRider() ?? false)) {
                        hasPlayer = true;
                    }
                    if ((hasPlayer || i != 0) && (!hasPlayer || i != 1)) {
                        Vector2 value = move.Value;
                        float yMove = MathHelper.Lerp(value.Y, value.Y + 12f, Ease.SineInOut(yLerp)) + sine;
                        platform.MoveToY(yMove + displacement.Y);
                        platform.MoveToX(value.X + displacement.X);
                    }
                }
            }
        }

        public override void OnShake(Vector2 amount) {
            if (!MasterOfGroup) {
                return;
            }
            base.OnShake(amount);
            tiles.Position += amount;
            foreach (JumpThru jumpthru in jumpthrus) {
                foreach (Component component in jumpthru.Components) {
                    if (component is Image image) {
                        image.Position += amount;
                    }
                }
            }
        }
    }

}