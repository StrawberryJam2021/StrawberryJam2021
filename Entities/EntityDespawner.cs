using Celeste.Mod.Entities;
using Celeste.Mod.Helpers;
using Microsoft.Xna.Framework;
using Monocle;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Celeste.Mod.StrawberryJam2021.Entities
{
    [CustomEntity("SJ2021/EntityDespawner")]
    public class EntityDespawner : Entity
    {
        //fields
        public bool invert;
        private Type[] typesToDespawn;
        public string sessionFlagName;

        //constructors
        public EntityDespawner(EntityData data, Vector2 offset)
            : base(data.Position + offset) {
            string unsplitNames = data.Attr("entityTypes");
            if (string.IsNullOrWhiteSpace(unsplitNames)) throw new ArgumentException("Entity Types cannot be blank.");

            typesToDespawn = unsplitNames
                .Split(',')
                .Select(name => FakeAssembly.GetFakeEntryAssembly().GetType(name) ?? throw new ArgumentException($"\"{name}\" is not a valid entity class name."))
                .ToArray();

            invert = data.Bool("invert");
            sessionFlagName = data.Attr("flag");
        }

        //methods
        public override void Awake(Scene scene)
        {
            bool sessionFlag = SceneAs<Level>().Session.GetFlag(sessionFlagName);
            if (sessionFlag ^ invert) {
                foreach (Type t in typesToDespawn) {
                    if(scene.Tracker.Entities.TryGetValue(t, out List<Entity> entitiesOfType)) {
                        foreach (Entity e in entitiesOfType) {
                            e.RemoveSelf();
                        }
                    }
                }
            }
        }
	}
}
