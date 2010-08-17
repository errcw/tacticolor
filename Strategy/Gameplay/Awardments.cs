using System;
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

        public Awardments(Storage storage)
        {
            _storage = storage;
            _awardments = new Dictionary<Gamer, List<Awardment>>(Match.MaxPlayerCount);

            AwardmentTypes = GetAwardmentTypes();
        }

        public void MatchStarted(ICollection<Gamer> players)
        {
            _awardments.Clear();
            foreach (Gamer gamer in players)
            {
                // load the awardments from storage if they exist
                List<Awardment> awardments = null;
                XmlStoreable<Awardment[]> awardmentXml = new XmlStoreable<Awardment[]>(GetStorageLocation(gamer));
                try
                {
                    if (_storage.Exists(awardmentXml))
                    {
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
                _awardments.Add(gamer, awardments);

                // run through the list of awardments
                foreach (Awardment awardment in awardments)
                {
                    bool earned = awardment.CheckOnMatchStarted();
                    if (earned && !awardment.IsEarned)
                    {
                        awardment.IsEarned = true;
                        if (AwardmentEarned != null)
                        {
                            AwardmentEarned(this, new AwardmentEventArgs(awardment, gamer));
                        }
                    }
                }
            }

            // flush the update awardment state to storage
            Save();
        }

        public void MatchEnded(ICollection<Gamer> players)
        {
            // for the players still in the game update the awardments with the match end
            foreach (Gamer gamer in players)
            {
                List<Awardment> awardments = _awardments[gamer];
                foreach (Awardment awardment in awardments)
                {
                    bool earned = awardment.CheckOnMatchEnded();
                    if (earned && !awardment.IsEarned)
                    {
                        awardment.IsEarned = true;
                        if (AwardmentEarned != null)
                        {
                            AwardmentEarned(this, new AwardmentEventArgs(awardment, gamer));
                        }
                    }
                }
            }

            // flush the update awardment state to storage
            Save();
        }

        private void Save()
        {
            foreach (KeyValuePair<Gamer, List<Awardment>> entry in _awardments)
            {
                Gamer gamer = entry.Key;
                List<Awardment> awardments = entry.Value;

                XmlStoreable<Awardment[]> awardmentXml = new XmlStoreable<Awardment[]>(GetStorageLocation(gamer));
                awardmentXml.Data = awardments.ToArray();
                try
                {
                    _storage.Save(awardmentXml);
                }
                catch (Exception e)
                {
                    Debug.WriteLine(e);
                }
            }
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

        /// <summary>
        /// Returns the file name where the awardments for the specified gamer are kept.
        /// </summary>
        private string GetStorageLocation(Gamer gamer)
        {
            return "StrategyAwardments_" + gamer.Gamertag;
        }

        private Storage _storage;

        private Dictionary<Gamer, List<Awardment>> _awardments;

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
            return MatchesPlayed >= 100;
        }
    }
}
