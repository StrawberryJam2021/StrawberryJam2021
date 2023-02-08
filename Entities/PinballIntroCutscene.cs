using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Monocle;
using System;
using System.Collections;
using System.Threading;


namespace Celeste.Mod.StrawberryJam2021.Entities
{
    class PinballIntroCutscene : CutsceneEntity
    {
        private class Fader : Entity
        {
            public float Target;
            public bool Ended;
            private float fade;
            public Fader()
            {
                base.Depth = -1000000;
            }
            public override void Update()
            {
                fade = Calc.Approach(fade, Target, Engine.DeltaTime * 0.5f);
                if (Target <= 0f && fade <= 0f && Ended)
                {
                    RemoveSelf();
                }
                base.Update();
            }

            public override void Render()
            {
                Camera camera = (base.Scene as Level).Camera;
                if (fade > 0f)
                {
                    Draw.Rect(camera.X - 10f, camera.Y - 10f, 340f, 200f, Color.Black * fade);
                }
                Player entity = base.Scene.Tracker.GetEntity<Player>();
                if (entity != null && !entity.OnGround(2))
                {
                    entity.Render();
                }
            }
        }
        private Player player;
        private Fader fader;
        private FMOD.Studio.EventInstance sfx;
        private PinballMachine pinball;
        public PinballIntroCutscene(Player player, PinballMachine pinball)
        {
            this.player = player;
            this.pinball = pinball;
        }
        public override void OnBegin(Level level)
        {
            Add(new Coroutine(Cutscene(level)));
            level.Add(fader = new Fader());
        }
        private IEnumerator Cutscene(Level level)
        {
            player.StateMachine.State = 11;
            player.StateMachine.Locked = true;
            Audio.SetMusic(null);
            yield return player.DummyWalkToExact((int)pinball.X);
            yield return .25f;
            yield return player.DummyWalkToExact((int)pinball.X - 16);
            yield return .5f;
            yield return player.DummyWalkToExact((int)pinball.X + 16);
            yield return .25f;
            player.Facing = Facings.Left;
            yield return 0.1f;
            yield return 1f;
            pinball.Activate();
            yield return 4f;
            player.DummyAutoAnimate = false;
            player.Sprite.Play("lookUp");
            yield return 1.1f;
            player.DummyAutoAnimate = true;
            Add(new Coroutine(level.ZoomTo(new Vector2(160f, 95f), 2.3f, 10f)));
            yield return 0.25f;
            player.ForceStrongWindHair.X = -1f;
            yield return player.DummyWalkToExact((int)player.X + 12, walkBackwards: false);
            player.Facing = Facings.Right;
            player.DummyAutoAnimate = false;
            player.DummyGravity = false;
            sfx = Audio.Play("event:/strawberry_jam_2021/game/maya/pumber", pinball.Center);
            player.Sprite.Play("runWind");
            while (player.Sprite.Rate > 0f)
            {
                player.MoveH(player.Sprite.Rate * 10f * Engine.DeltaTime);
                player.MoveV((0f - (1f - player.Sprite.Rate)) * 6f * Engine.DeltaTime);
                player.Sprite.Rate -= Engine.DeltaTime * 0.15f;

                yield return null;
            }
            yield return 0.5f;
            player.Sprite.Play("fallFast");
            player.Sprite.Rate = 1f;
            Vector2 target = pinball.Center - new Vector2(0f, 7f);
            Vector2 from = player.Position;
            for (float p2 = 0f; p2 < 1f; p2 += Engine.DeltaTime * 2f)
            {
                player.Position = from + (target - from) * Ease.SineInOut(p2);
                if(p2 > .1f) {
                    Glitch.Value = .3f;
                    SceneAs<Level>().Session.SetFlag("introSilhouette", true);
                }
                if(p2 > .3f) {
                    Glitch.Value = 0f;
                    SceneAs<Level>().Session.SetFlag("introSilhouette", false);
                }
                yield return null;
            }
            Glitch.Value = .3f;
            yield return .1f;
            SceneAs<Level>().Session.SetFlag("introSilhouette", true);
            yield return .1f;
            Glitch.Value = 0f;
            player.ForceStrongWindHair.X = 0f;


            SpotlightWipe.FocusPoint = player.Position - (Scene as Level).Camera.Position + new Vector2(0f, -8f);
            SpotlightWipe spotWipe = new SpotlightWipe(Scene, wipeIn: false, delegate {
                Thread.Sleep(100);
                EndCutscene(level);
            });
            level.Add(spotWipe);
            

            
        }
        public override void OnEnd(Level level)
        {
            level.OnEndOfFrame += delegate {
                if (fader != null && !WasSkipped)
                {
                    fader.Tag = Tags.Global;
                    fader.Target = 0f;
                    fader.Ended = true;
                }
                if (WasSkipped) {
                    level.Remove(player);
                    level.UnloadLevel();
                    Audio.SetMusic(null);
                    if (sfx != null) {
                        Audio.Stop(sfx);
                        }
                    level.Session.Level = "Tutorial";
                    level.Session.FirstLevel = false;
                    level.Session.RespawnPoint = level.GetSpawnPoint(new Vector2(level.Bounds.Left, level.Bounds.Bottom));
                    level.LoadLevel(Player.IntroTypes.None);
                    level.Wipe.Cancel();

                } else {
                    
                    level.Remove(player);
                    level.EndCutscene();
                    level.UnloadLevel();
                    level.Session.Level = "Tutorial";
                    level.Session.FirstLevel = false;
                    level.Session.RespawnPoint = level.GetSpawnPoint(new Vector2(level.Bounds.Left, level.Bounds.Top));
                    level.LoadLevel(Player.IntroTypes.Fall);
                    Audio.SetMusic(null);
                    level.Add(new PinballFallCutscene());
                    level.Camera.Y -= 8f;
                    if (!WasSkipped && level.Wipe != null) {
                        level.Wipe.Cancel();
                    }

                    if (fader != null) {
                        fader.RemoveTag(Tags.Global);
                    }
                }
                
            };
        }
        

    }
}
