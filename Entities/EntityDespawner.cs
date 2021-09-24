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
		public bool Respawn; //inverts the thing
        public string NamesOfEntitiesToDespawn;
        public string FlagName;
        //constructors
        public EntityDespawner(EntityData data, Vector2 offset)
            : base(data.Position + offset) {
            NamesOfEntitiesToDespawn = data.Attr("NamesOfEntitiesToDespawn"); //think of a better name for this field, probably
            Respawn = data.Attr("Respawn") == "true";
            FlagName = data.Attr("Name");
        }
        //methods

        public override void Awake(Scene scene)
        {
            List<Entity> entities = scene.Tracker.GetEntities<Entity>();
            string[] entitiesToDespawn = NamesOfEntitiesToDespawn.Split(';');
            foreach(Entity e in entities) {
                CustomEntityAttribute entityAttribute =
                    (CustomEntityAttribute) Attribute.GetCustomAttribute(e.GetType(), typeof(CustomEntityAttribute));
                if (entityAttribute is null) continue;
                else {
                    foreach (string entityName in entityAttribute.IDs) {
                        foreach (string name in entitiesToDespawn) {
                            if (name.Equals(entityName)) {
                                if (Respawn) {
                                    //I feel like we should be checking if its already despawned / spawned from another controller maybe? not sure what field / attribute to check for that
                                    //especially for the reverse?
                                    //
                                    //todo FIGURE OUT WHAT ADDS AN ENTITY BACK
                                } else {
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
