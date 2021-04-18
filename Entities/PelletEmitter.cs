using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.StrawberryJam2021.Entities {
    /// <summary>
    /// Base class for an entity that fires pellets.
    /// Functionality and pellet visuals can be configured by overriding <see cref="CreateFiringComponent"/>.
    /// </summary>
    public abstract class PelletEmitter : OrientableEntity {
        protected PelletEmitter(EntityData data, Vector2 offset, Orientations orientation)
            : base(data, offset, orientation) {
            const float shotOriginOffset = 8f;
            var firingComponent = CreateFiringComponent(data);
            firingComponent.Settings = new() {
                CollideWithSolids = data.Bool("collideWithSolids", true),
                Color = data.HexColor("pelletColor", Color.Red),
                Speed = data.Float("pelletSpeed", 100f),
                Direction = Orientation.Direction(),
                Origin = Orientation.Direction() * shotOriginOffset,
            };

            Add(firingComponent);
            
            Sprite emitterSprite = StrawberryJam2021Module.SpriteBank.Create("laserEmitter");
            emitterSprite.Rotation = Orientation.Angle();
            
            Add(emitterSprite);
        }

        protected virtual PelletFiringComponent CreateFiringComponent(EntityData data) =>
            new PelletFiringComponent<PelletShot> {Count = data.Int("count", 1)};
        
        [Pooled]
        [Tracked]
        public class PelletShot : Entity {
            private readonly Sprite sprite;
            private readonly PelletFiringComponent.PelletComponent pelletComponent;

            public PelletShot()
                : base(Vector2.Zero) {
                Collider = new Hitbox(4f, 4f, -2f, -2f);
                Depth = Depths.Top;

                Add(sprite = GFX.SpriteBank.Create("badeline_projectile"));
                Add(new PlayerCollider(onPlayerCollide));
                Add(pelletComponent = new PelletFiringComponent.PelletComponent());
            }

            public override void Render() {
                var position = sprite.Position;
                
                // render black outline
                sprite.Color = Color.Black;
                sprite.Position = position + Vector2.UnitX;
                sprite.Render();
                sprite.Position = position - Vector2.UnitX;
                sprite.Render();
                sprite.Position = position + Vector2.UnitY;
                sprite.Render();
                sprite.Position = position - Vector2.UnitY;
                sprite.Render();
                sprite.Color = pelletComponent.Settings.Color;
                sprite.Position = position;
                
                base.Render();
            }

            private void onPlayerCollide(Player player) => player.Die((player.Center - Position).SafeNormalize());
        }
    }
}