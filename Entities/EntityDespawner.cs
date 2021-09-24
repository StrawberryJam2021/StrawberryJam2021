using Celeste.Mod.Entities;
using Celeste.Mod.Helpers;
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
        public bool Despawn; //causes entity to do nothing if set to false
        private Type[] typesToDespawn;
        public string FlagName;
        //constructors
        public EntityDespawner(EntityData data, Vector2 offset)
            : base(data.Position + offset) {
            string unsplitNames = data.Attr("NamesOfEntitiesToDespawn");
            if (unsplitNames == "") {
                throw new ArgumentException("Names of entities to despawn cannot be empty.");
            }
            string[] entitiesToDespawn = data.Attr("NamesOfEntitiesToDespawn").Split(',');
            typesToDespawn = new Type[entitiesToDespawn.Length];
            int i = 0;
            foreach (string name in entitiesToDespawn) {
                Type t = FakeAssembly.GetFakeEntryAssembly().GetType(name);
                if (t is null) {
                    throw new ArgumentException($"\"{name}\" is not a valid entity class name.");
                }
                typesToDespawn[i++] = t;
            }
            Despawn = data.Bool("Despawn");
            FlagName = data.Attr("Name");
        }
        //methods

        public override void Awake(Scene scene)
        {
            if (Despawn) {
                foreach (Type t in typesToDespawn) {
                    scene.Tracker.Entities.TryGetValue(t, out List<Entity> entitiesOfType);
                    foreach (Entity e in entitiesOfType) {
                        e.RemoveSelf();
                    }
                }
            }
        }
	}
}
