using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Monocle;
using MonoMod.Utils;
using System.Collections;
using System.Reflection;

namespace Celeste.Mod.StrawberryJam2021.Entities {
    [Tracked]
    [CustomEntity("SJ2021/WormholeBooster")]
    class WormholeBooster : Booster {
        private PlayerCollider coll;
        private DynData<Booster> self;
        public static bool TeleDeath;
        public static bool TeleportingDNI;
        public static bool TDLock = false;

        public IEnumerator TeleportCoroutine(Player player) {
            yield return null;

            WormholeBooster nearest = FindNearestBooster();
            player.Position = nearest.Position;
            Audio.Play("event:/char/badeline/disappear", nearest.Position);
            typeof(Booster).GetMethod("OnPlayer", BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.InvokeMethod).Invoke(nearest, new object[] { player }); // teleports and boosts from nearest booster
            RemoveSelf();
        }

        public WormholeBooster(EntityData data, Vector2 offset) : this(data.Position + offset) {
        }

        public WormholeBooster(Vector2 position) : base(position, false) {

        }

        public override void Awake(Scene scene) {
            base.Awake(scene);

            PlayerCollider c;
            while ((c = Get<PlayerCollider>()) != null)
                Remove(c); // get rid of the existing player collider

            Add(coll = new PlayerCollider(onWormholeActivate)); // add our own
        }
        public override void Added(Scene scene) {
            base.Added(scene);
            self = new DynData<Booster>(this);
            self.Get<Entity>("outline").RemoveSelf();
        }
        public override void Update() {
            base.Update();
            if (self.Get<float>("respawnTimer") > 0.2f) {
                self.Set("respawnTimer", 0.1f);
            }
        }
        private void onWormholeActivate(Player player) {
            if (TeleportingDNI)
                return; // if something is teleporting the player, don't do it yourself;
            else {
                TeleportingDNI = true; // teleporting (or killing) rn, everyone else shut up;
                if (TeleDeath) { // TIME TO DIE!!!
                    typeof(Booster).GetMethod("OnPlayer", BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.InvokeMethod).Invoke(this, new object[] { player }); // with TeleDeath, this will kill the player
                } else { // oh, guess not.
                    Add(new Coroutine(TeleportCoroutine(player)));
                }
            }
        }
        private WormholeBooster FindNearestBooster() {
            WormholeBooster leader = null; // the closest booster gets stored to be returned at the end
            foreach (WormholeBooster booster in SceneAs<Level>().Tracker.GetEntities<WormholeBooster>()) {
                if (booster != this) { // this is why we can't just Tracker.GetNearestEntity
                    if (leader != null) {
                        if ((booster.Position - Position).Length() < (leader.Position - Position).Length()) { // if the distance between this and the current booster is less than the distance between this and the leader, update the leader!
                            leader = booster;
                        }
                    } else {
                        leader = booster; //if the leader is still null, just update the leader with the first booster it can find
                    }
                }
            }
            if (leader == null) {
                SceneAs<Level>().Tracker.GetEntity<Player>().Die(Vector2.Zero);
                return this;
            } else
                return leader; // this'll be the closest wormholebooster around
        }
    }
}
