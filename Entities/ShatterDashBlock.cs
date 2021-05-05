using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Celeste;
using Celeste.Mod.Entities;
using Monocle;
using Microsoft.Xna.Framework;

namespace Celeste.Mod.StrawberryJam2021.Entities {
    [CustomEntity("SJ2021/ShatterDashBlock")]
    public class ShatterDashBlock : Solid {

        private bool permanent;

        private EntityID id;

        private char tileType;

        private float width;

        private float height;

        private bool blendIn;

        private float speedReq;

        private float delay;

        //Temporary Debug Variables, these will be consts by the end of the workload
        private float speedDec;
        private float shakeTime;

        public ShatterDashBlock(EntityData data, Vector2 offset, EntityID id) : base(data.Position + offset, data.Width, data.Height, true) {
            base.Depth = Depths.FakeWalls + 1;
            this.id = id;
            permanent = data.Bool("permanent");
            width = data.Width;
            height = data.Height;
            blendIn = data.Bool("blendin");
            tileType = data.Char("tiletype", '3');
            delay = MathHelper.Clamp(data.Float("FreezeTime", 0.1f), 0, 0.5f);
            speedReq = Math.Max(0f, data.Float("SpeedRequirement", 0f));
            speedDec = Math.Max(0f, data.Float("SpeedDecrease", 80f));
            shakeTime = Math.Max(0f, data.Float("ShakeTime", 0.3f));
            OnDashCollide = OnDashed;
            SurfaceSoundIndex = SurfaceIndex.TileToIndex[tileType];
        }

        public override void Awake(Scene scene) {
            base.Awake(scene);
            TileGrid tileGrid;
            if (!blendIn) {
                tileGrid = GFX.FGAutotiler.GenerateBox(tileType, (int) width / 8, (int) height / 8).TileGrid;
                Add(new LightOcclude());
            } else {
                Level level = SceneAs<Level>();
                Rectangle tileBounds = level.Session.MapData.TileBounds;
                VirtualMap<char> solidsData = level.SolidsData;
                int x = (int) (base.X / 8f) - tileBounds.Left;
                int y = (int) (base.Y / 8f) - tileBounds.Top;
                int tilesX = (int) base.Width / 8;
                int tilesY = (int) base.Height / 8;
                tileGrid = GFX.FGAutotiler.GenerateOverlay(tileType, x, y, tilesX, tilesY, solidsData).TileGrid;
                Add(new EffectCutout());
                base.Depth = -10501;
            }
            Add(tileGrid);
            Add(new TileInterceptor(tileGrid, highPriority: true));
            if (CollideCheck<Player>()) {
                RemoveSelf();
            }
        }



        public void Break(Player player, Vector2 direction, bool playSound = true, bool playDebrisSound = true) {
            if (playSound) {
                if (tileType == '1') {
                    Audio.Play(SFX.game_gen_wallbreak_dirt, Position);
                } else if (tileType == '3') {
                    Audio.Play(SFX.game_gen_wallbreak_ice, Position);
                } else if (tileType == '9') {
                    Audio.Play(SFX.game_gen_wallbreak_wood, Position);
                } else {
                    Audio.Play(SFX.game_gen_wallbreak_stone, Position);
                }
            }
            for (int i = 0; (float) i < base.Width / 8f; i++) {
                for (int j = 0; (float) j < base.Height / 8f; j++) {
                    base.Scene.Add(Engine.Pooler.Create<Debris>().Init(Position + new Vector2(4 + i * 8, 4 + j * 8), tileType, playDebrisSound).BlastFrom(player.Center));
                }
            }

            Celeste.Freeze(delay);
            Collidable = false;
            SceneAs<Level>().DirectionalShake(direction * -1, shakeTime);
            player.Speed -= Vector2.UnitX.RotateTowards(direction.Angle(), 6.3f) * speedDec;

            if (permanent) {
                RemoveAndFlagAsGone();
            } else {
                RemoveSelf();
            }
        }

        public void RemoveAndFlagAsGone() {
            RemoveSelf();
            SceneAs<Level>().Session.DoNotLoad.Add(id);
        }

        private DashCollisionResults OnDashed(Player player, Vector2 direction) {
            Console.WriteLine(direction + "\t" + (0 - direction.Angle()));
            if (Math.Abs(player.Speed.X) > speedReq) {
                Break(player, direction, true);
                return DashCollisionResults.Ignore;
            } else {
                return DashCollisionResults.NormalCollision;
            }
        }

        public void Break(Player player, Vector2 direction, bool playSound = true) {
            Break(player, direction, playSound, playDebrisSound: true);
        }
    }
}
