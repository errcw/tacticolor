﻿using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Xml.Serialization;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.GamerServices;

using Strategy.Properties;
using Strategy.Library.Storage;

namespace Strategy.Gameplay
{
    /// <summary>
    /// An acheivement.
    /// </summary>
    [XmlInclude(typeof(ManyMatchesAwardment))]
    public abstract class Awardment
    {
        /// <summary>
        /// The name of this awardment.
        /// </summary>
        [XmlIgnore]
        public string Name { get; protected set; }

        /// <summary>
        /// A description of how to acheive this awardment.
        /// </summary>
        [XmlIgnore]
        public string Description { get; protected set; }

        /// <summary>
        /// If this awardment has been earned.
        /// </summary>
        public bool IsEarned { get; set; }

        /// <summary>
        /// Updates the state of this awardment when a match starts.
        /// </summary>
        /// <returns>True if this awardment was earned; otherwise, false.</returns>
        public virtual bool CheckOnMatchStarted()
        {
            return false;
        }

        /// <summary>
        /// Updates the state of this awardment when a match ends.
        /// </summary>
        /// <returns>True if this awardment was earned; otherwise, false.</returns>
        public virtual bool CheckOnMatchEnded()
        {
            return false;
        }
    }

    /// <summary>
    /// A set of awardments.
    /// </summary>
    public class Awardments
    {
        /// <summary>
        /// Occurs when a new awardment is earned.
        /// </summary>
        public event EventHandler<AwardmentEventArgs> AwardmentEarned;

        public Awardments(Game game, Storage storage)
        {
            _game = game;
            _storage = storage;

            AwardmentTypes = GetAwardmentTypes();
        }

        public void MatchStarted(ICollection<Gamer> players)
        {
            foreach (Gamer gamer in players)
            {
                XmlStoreable<Awardment[]> awardmentXml = new XmlStoreable<Awardment[]>(GetStorageLocation(gamer));
                List<Awardment> awardments = null;
                try
                {
                    if (_storage.Exists(awardmentXml))
                    {
                        // offload the load to another thread?
                        _storage.Load(awardmentXml);
                        awardments = new List<Awardment>(awardmentXml.Data);
                        AddMissingAwardments(awardments, AwardmentTypes);
                    }
                }
                catch (Exception e)
                {
                    Debug.WriteLine(e);
                }
                // if there is nothing to load then start with a blank list
                if (awardments == null)
                {
                    awardments = CreateAwardments(AwardmentTypes);
                }
                // do something useful with the data
            }
        }

        public void MatchEnded()
        {
            // offload the save to another thread
        }

        /// <summary>
        /// Returns the types of awardments declared on the awardment class.
        /// </summary>
        private List<Type> GetAwardmentTypes()
        {
            object[] typeAttrs = typeof(Awardment).GetCustomAttributes(typeof(XmlIncludeAttribute), false);
            List<Type> types = new List<Type>(typeAttrs.Length);
            foreach (object typeAttr in typeAttrs)
            {
                XmlIncludeAttribute attr = (XmlIncludeAttribute)typeAttr;
                types.Add(attr.Type);
            }
            return types;
        }

        /// <summary>
        /// Creates a list of awardments from a list of types.
        /// </summary>
        /// <param name="types">The types of awardment to create.</param>
        private List<Awardment> CreateAwardments(IEnumerable<Type> types)
        {
            List<Awardment> awardments = new List<Awardment>();
            foreach (Type type in types)
            {
                Awardment awardment = (Awardment)type.GetConstructor(Type.EmptyTypes).Invoke(new object[0]);
                awardments.Add(awardment);
            }
            return awardments;
        }

        /// <summary>
        /// Ensures a list of awardments contains all of a set of types.
        /// </summary>
        /// <param name="awardments">The list of awardments.</param>
        /// <param name="types">The types of awardments that should appear in the list.</param>
        private void AddMissingAwardments(List<Awardment> awardments, List<Type> types)
        {
            var awardmentTypes = awardments.Select(a => a.GetType());
            var missingTypes = types.Intersect(types);
            var newAwardments = CreateAwardments(missingTypes);
            awardments.AddRange(newAwardments);
        }

        private string GetStorageLocation(Gamer gamer)
        {
            return "StrategyAwardments_" + gamer.Gamertag;
        }

        private Game _game;
        private Storage _storage;

        private readonly List<Type> AwardmentTypes;
    }

    /// <summary>
    /// Event arguments when an awardment is earned.
    /// </summary>
    public class AwardmentEventArgs : EventArgs
    {
        public Awardment Awardment { get; private set; }
        public Gamer Gamer { get; private set; }

        public AwardmentEventArgs(Awardment awardment, Gamer gamer)
        {
            Awardment = awardment;
            Gamer = gamer;
        }
    }

    public class ManyMatchesAwardment : Awardment
    {
        public int MatchesPlayed { get; set; }

        public ManyMatchesAwardment()
        {
            Name = Resources.AwardmentManyMatchesName;
            Description = Resources.AwardmentManyMatchesDescription;
            IsEarned = false;
        }

        public override bool CheckOnMatchEnded()
        {
            MatchesPlayed += 1;
            return MatchesPlayed == 100;
        }
    }
}
