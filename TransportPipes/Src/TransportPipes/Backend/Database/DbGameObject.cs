using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using TransportPipesCommon;
using TransportPipesCommon.Enums;

namespace TransportPipes.Backend.Database
{
    /// <summary>
    /// GameObject as stored in the DB.
    /// </summary>
    public class DbGameObject
    {
        /// <summary>
        /// Gets or sets the identifier.
        /// </summary>
        /// <value>
        /// The identifier.
        /// </value>
        // ReSharper disable once InconsistentNaming
        public virtual int ID { get; set; }

        /// <summary>
        /// Gets or sets the x.
        /// </summary>
        /// <value>
        /// The x.
        /// </value>
        public virtual int X { get; set; }

        /// <summary>
        /// Gets or sets the y.
        /// </summary>
        /// <value>
        /// The y.
        /// </value>
        public virtual int Y { get; set; }

        /// <summary>
        /// Gets or sets the type of the game object.
        /// </summary>
        /// <value>
        /// The type of the game object.
        /// </value>
        public virtual GameObjectType GameObjectType { get; set; }

        /// <summary>
        /// Gets or sets the chunk x.
        /// </summary>
        /// <value>
        /// The chunk x.
        /// </value>
        [Index]
        public virtual int ChunkX { get; set; }

        /// <summary>
        /// Gets or sets the chunk y.
        /// </summary>
        /// <value>
        /// The chunk y.
        /// </value>
        [Index]
        public virtual int ChunkY { get; set; }

        /// <summary>
        /// Gets or sets the GlobalConnection.
        /// </summary>
        /// <value>
        /// The GlobalConnection
        /// </value>
        public virtual DbGlobalConnection GlobalConnection { get; set; }

        /// <summary>
        /// Gets or sets the sources.
        /// </summary>
        /// <value>
        /// The sources.
        /// </value>
        [InverseProperty("Sources")]
        public virtual ICollection<DbConnection> Sources { get; set; }

        /// <summary>
        /// Gets or sets the destinations.
        /// </summary>
        /// <value>
        /// The destinations.
        /// </value>
        [InverseProperty("Destinations")]
        public virtual ICollection<DbConnection> Destinations { get; set; }

        /// <summary>
        /// Gets or sets the owner.
        /// </summary>
        /// <value>
        /// The owner.
        /// </value>
        public virtual DbUser Owner { get; set; }

        /// <summary>
        /// Gets or sets the backend game object.
        /// </summary>
        /// <value>
        /// The backend game object.
        /// </value>
        [NotMapped]
        public GameLogicObjects.BackendGameObject BackendGameObject { get; set; }

        /// <summary>
        /// Prevents a default instance of the <see cref="DbGameObject"/> class from being created.
        /// </summary>
        private DbGameObject()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DbGameObject" /> class.
        /// </summary>
        /// <param name="x">The x.</param>
        /// <param name="y">The y.</param>
        /// <param name="gameObjectType">Type of the game object.</param>
        /// <param name="owner">The owner.</param>
        public DbGameObject(int x, int y, GameObjectType gameObjectType, DbUser owner)
        {
            //this.GlobalConnectionId = -1;

            this.X = x;
            this.Y = y;


            int chunkX = (int)Math.Floor(x / (float)StaticConfigInfo.ChunkWidth);
            int chunkY = (int)Math.Floor(y / (float)StaticConfigInfo.ChunkHeight);

            this.ChunkX = chunkX;
            this.ChunkY = chunkY;
            this.GameObjectType = gameObjectType;
            this.Owner = owner;
        }
    }
}