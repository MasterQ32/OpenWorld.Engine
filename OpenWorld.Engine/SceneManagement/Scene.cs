﻿using BulletSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OpenWorld.Engine.SceneManagement
{
	/// <summary>
	/// Defines a 3D scene.
	/// </summary>
	public sealed class Scene : System.IDisposable
	{
		readonly SceneNode root = null;

		private DynamicsWorld world;
		private SceneRenderer defaultRenderer; 


		/// <summary>
		/// Instantiates a new scene.
		/// </summary>
		public Scene()
			: this(SceneCreationFlags.None)
		{
			
		}

		/// <summary>
		/// Instantiates a new scene.
		/// </summary>
		/// <param name="flags">Specifies the behaviour of the world.</param>
		public Scene(SceneCreationFlags flags)
		{
			this.root = SceneNode.CreateRoot(this);
			this.defaultRenderer = new SimpleRenderer();

			if (flags.HasFlag(SceneCreationFlags.EnablePhysics))
			{
				this.CollisionConfiguration = new DefaultCollisionConfiguration();
				this.Dispatcher = new CollisionDispatcher(this.CollisionConfiguration);
				this.Broadphase = new DbvtBroadphase();
				this.Broadphase.OverlappingPairCache.SetInternalGhostPairCallback(new GhostPairCallback());

				this.world = new DiscreteDynamicsWorld(this.Dispatcher, this.Broadphase, null, this.CollisionConfiguration);

				// Register physics handler
				Game.Current.UpdateNonScene += this.UpdatePhysics;
			}

			Game.Current.Disposing += this.Dispose;
		}

		/// <summary>
		/// Destroys the scene.
		/// </summary>
		~Scene()
		{
			this.Dispose();
		}

		/// <summary>
		/// Casts a ray in the physics world.
		/// </summary>
		/// <param name="ray">Ray</param>
		/// <param name="distance">Distance the ray should travel</param>
		/// <returns>RaycastResult or null if nothing hit.</returns>
		public RaycastResult Raycast(Ray ray, float distance)
		{
			var from = ray.Origin.ToBullet();
			var to = (ray.Origin + distance * ray.Direction).ToBullet();

			var callback = new CollisionWorld.ClosestRayResultCallback(from, to);
			this.world.RayTest(from, to, callback);

			if (!callback.HasHit)
				return null;

			var result = new RaycastResult();

			result.Position = callback.HitPointWorld.ToOpenTK();
			result.Normal = callback.HitNormalWorld.ToOpenTK();
			result.RigidBody = callback.CollisionObject.UserObject as RigidBody;
			if (result.RigidBody != null)
				result.Node = result.RigidBody.Node;

			return result;
		}

		/// <summary>
		/// Updates the whole scene.
		/// </summary>
		/// <param name="time">Time snapshot</param>
		public void Update(GameTime time)
		{
			// Just update the root node.
			this.root.DoUpdate(time);
		}

		private void UpdatePhysics(object sender, UpdateEventArgs e)
		{
			if (this.PhysicsEnabled)
			{
				this.world.StepSimulation(e.Time.DeltaTime);
			}
		}

		/// <summary>
		/// Draws the whole scene.
		/// </summary>
		/// <param name="camera">The camera setting for the scene.</param>
		/// <param name="time">Time snapshot</param>
		public void Draw(Camera camera, GameTime time)
		{
			this.Draw(camera, this.defaultRenderer, time);	
		}

		/// <summary>
		/// Draws the whole scene.
		/// </summary>
		/// <param name="camera">The camera setting for the scene.</param>
		/// <param name="renderer">The renderer that draws the the scene.</param>
		/// <param name="time">Time snapshot</param>
		public void Draw(Camera camera, SceneRenderer renderer, GameTime time)
		{
			if (camera == null)
				throw new ArgumentNullException("camera");
			if (renderer == null)
				throw new ArgumentNullException("renderer");

			renderer.Begin();
			this.root.DoDraw(time, renderer);
			renderer.End(this, camera);
		}

		/// <summary>
		/// Disposes all native components.
		/// </summary>
		public void Dispose()
		{
			Game.Current.Disposing -= this.Dispose;

			if(this.PhysicsEnabled)
			{
				// Unregister physics handler
				Game.Current.UpdateNonScene -= this.UpdatePhysics;
			}

			this.root.Release();

			if (this.World != null)
				this.World.Dispose();
			if (this.Broadphase != null)
				this.Broadphase.Dispose();
			if (this.Dispatcher != null)
				this.Dispatcher.Dispose();
			if (this.CollisionConfiguration != null)
				this.CollisionConfiguration.Dispose();

			this.world = null;
			this.Broadphase = null;
			this.Dispatcher = null;
			this.CollisionConfiguration = null;

			GC.SuppressFinalize(this);
		}

		/// <summary>
		/// Gets the root of the scene.
		/// </summary>
		public SceneNode Root
		{
			get { return root; }
		}

		/// <summary>
		/// Gets the Jitter world if physics are enabled or returns null if not.
		/// </summary>
		public DynamicsWorld World
		{
			get { return world; }
		}

		/// <summary>
		/// Gets a value that indicates wheather physics are enabled or not.
		/// </summary>
		public bool PhysicsEnabled
		{
			get { return this.world != null; }
		}

		/// <summary>
		/// Gets the bullet collision configuration.
		/// </summary>
		public CollisionConfiguration CollisionConfiguration { get; private set; }


		/// <summary>
		/// Gets the bullet collision dispatcher.
		/// </summary>
		public CollisionDispatcher Dispatcher { get; private set; }


		/// <summary>
		/// Gets the bullet broadphase interface.
		/// </summary>
		public BroadphaseInterface Broadphase { get; private set; }
	}

	/// <summary>
	/// Specifies flags for world creation.
	/// </summary>
	[Flags]
	public enum SceneCreationFlags
	{
		/// <summary>
		/// No special functionality.
		/// </summary>
		None = 0,

		/// <summary>
		/// Enables physics for the scene.
		/// </summary>
		EnablePhysics = (1 << 0)
	}
}
