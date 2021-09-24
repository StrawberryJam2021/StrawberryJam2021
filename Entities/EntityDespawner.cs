using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Monocle;
using System;
using System.Collections.Generic;

namespace Celeste.Mod.StrawberryJam2021.Entities
{
    [CustomEntity("SJ2021/EntityDespawner")]
    public class EntityDespawner : Entity
	{
		//fields
		public bool Despawn; //inverts the thing
        public string NamesOfEntitiesToDespawn;
        public string FlagName;
        //constructors
        public EntityDespawner(EntityData data, Vector2 offset)
            : base(data.Position + offset) {
            NamesOfEntitiesToDespawn = data.Attr("NamesOfEntitiesToDespawn"); //think of a better name for this field, probably
            Despawn = data.Bool("Despawn");
            FlagName = data.Attr("Name");
        }
        //methods

        public override void Awake(Scene scene)
        {
            if (Despawn) {
                List<Entity> entities = scene.Tracker.GetEntities<Entity>();
                string[] entitiesToDespawn = NamesOfEntitiesToDespawn.Split(';');
                foreach (Entity e in entities) {
                    CustomEntityAttribute entityAttribute =
                        (CustomEntityAttribute) Attribute.GetCustomAttribute(e.GetType(), typeof(CustomEntityAttribute));
                    if (entityAttribute is null)
                        continue;
                    else {
                        foreach (string entityName in entityAttribute.IDs) {
                            foreach (string name in entitiesToDespawn) {
                                if (name.Equals(entityName)) {
                                    e.RemoveSelf();
                                }
                            }
                        }
                    }
                }
            }
        }
	}

}
