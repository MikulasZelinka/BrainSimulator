﻿using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.IO;
using System.Linq;
using GoodAI.Logging;
using TmxMapSerializer.Elements;
using VRageMath;
using World.GameActors;
using World.GameActors.GameObjects;
using World.GameActors.Tiles;
using World.Physics;
using World.WorldInterfaces;

namespace World.ToyWorldCore
{
    public class ToyWorld : IWorld
    {
        private readonly CollisionResolver m_collisionResolver;

        public Vector2I Size { get; private set; }
        public AutoupdateRegister AutoupdateRegister { get; protected set; }
        public Atlas Atlas { get; protected set; }
        public TilesetTable TilesetTable { get; protected set; }
        public IPhysics Physics { get; protected set; }


        public ToyWorld(Map tmxDeserializedMap, StreamReader tileTable)
        {
            if (tileTable == null)
                throw new ArgumentNullException("tileTable");
            Contract.EndContractBlock();

            Size = new Vector2I(tmxDeserializedMap.Width, tmxDeserializedMap.Height);

            AutoupdateRegister = new AutoupdateRegister();

            TilesetTable = new TilesetTable(tmxDeserializedMap, tileTable);
            Action<GameActor> initializer = delegate(GameActor actor)
            {
                IAutoupdateable updateable = actor as IAutoupdateable;
                if (updateable != null)
                {
                    updateable.Update(this);
                    if (updateable.NextUpdateAfter > 0)
                        AutoupdateRegister.Register(updateable, updateable.NextUpdateAfter);
                }
            };
            Atlas = MapLoader.LoadMap(tmxDeserializedMap, TilesetTable, initializer);


            // physics
            Physics = new Physics.Physics();

            IMovementPhysics movementPhysics = new MovementPhysics();
            ICollisionChecker collisionChecker = new CollisionChecker(Atlas);
            m_collisionResolver = new CollisionResolver(collisionChecker, movementPhysics);

            Log.Instance.Debug("World.ToyWorldCore.ToyWorld: Loading Successful");
        }


        //
        // TODO: methods below will be moved to some physics class
        //


        private void UpdatePhysics()
        {
            // temporal code to test basic physics
            foreach (IAvatar avatar in Atlas.GetAvatars())
            {
                m_collisionResolver.ResolveCollision(avatar.PhysicalEntity);
            }
            foreach (ICharacter character in Atlas.Characters)
            {
                m_collisionResolver.ResolveCollision(character.PhysicalEntity);
            }
        }

        private void UpdateCharacters()
        {
            List<ICharacter> characters = Atlas.Characters;
            List<IForwardMovablePhysicalEntity> forwardMovablePhysicalEntities = characters.Select(x => x.PhysicalEntity).ToList();
            Physics.MoveMovableDirectable(forwardMovablePhysicalEntities);
        }

        private void UpdateAvatars()
        {
            List<IAvatar> avatars = Atlas.GetAvatars();
            Physics.TransformControlsPhysicalProperties(avatars);
            List<IForwardMovablePhysicalEntity> forwardMovablePhysicalEntities = avatars.Select(x => x.PhysicalEntity).ToList();
            Physics.MoveMovableDirectable(forwardMovablePhysicalEntities);
        }

        public void UpdateScheduled()
        {
            foreach (IAutoupdateable actor in AutoupdateRegister.CurrentUpdateRequests)
                actor.Update(this);
            AutoupdateRegister.CurrentUpdateRequests.Clear();
            AutoupdateRegister.Tick();
        }

        public void Update()
        {
            UpdateScheduled();
            UpdateTiles();
            UpdateAvatars();
            UpdateCharacters();
            AutoupdateRegister.UpdateItems(this);
            UpdatePhysics();
            Log.Instance.Debug("World.ToyWorldCore.ToyWorld: Step performed");
        }

        public List<int> GetAvatarsIds()
        {
            return Atlas.Avatars.Keys.ToList();
        }

        public List<string> GetAvatarsNames()
        {
            return Atlas.Avatars.Values.Select(x => x.Name).ToList();
        }

        public IAvatar GetAvatar(int id)
        {
            return Atlas.Avatars[id];
        }

        private void UpdateTiles()
        {
        }

        [ContractInvariantMethod]
        private void Invariants()
        {
            Contract.Invariant(Atlas != null, "Atlas cannot be null");
            Contract.Invariant(AutoupdateRegister != null, "Autoupdate register cannot be null");
        }
    }
}