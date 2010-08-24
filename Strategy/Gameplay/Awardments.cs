using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
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

        public Awardments()
        {
            _awardments = new Dictionary<string, List<Awardment>>();
            AwardmentTypes = GetAwardmentTypes();
        }

        public void MatchStarted(IEnumerable<Gamer> players)
        {
            foreach (Gamer gamer in players)
            {
                List<Awardment> awardments = null;
                if (!_awardments.TryGetValue(gamer.Gamertag, out awardments))
                {
                    // no existing awardments for this gamer, create new ones
                    awardments = CreateAwardments(AwardmentTypes);
                }
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
        }

        public void MatchEnded(IEnumerable<Gamer> players, Gamer winner)
        {
            foreach (Gamer gamer in players)
            {
                List<Awardment> awardments = _awardments[gamer.Gamertag];
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
        }

        /// <summary>
        /// Loads the awardment state for all gamers from storage.
        /// </summary>
        public void Load(Storage storage)
        {
            foreach (string file in Directory.GetFiles(AwardmentDirectory))
            {
                XmlStoreable<Awardment[]> awardmentXml = new XmlStoreable<Awardment[]>(file);
                string gamer = Path.GetFileNameWithoutExtension(file);
                try
                {
                    storage.Load(awardmentXml);
                    List<Awardment> awardments = new List<Awardment>(awardmentXml.Data);
                    AddMissingAwardments(awardments, AwardmentTypes);
                    _awardments.Add(gamer, awardments);
                }
                catch (Exception e)
                {
                    Debug.WriteLine(e);
                }
            }
        }

        /// <summary>
        /// Saves the awardment state from all gamers to storage.
        /// </summary>
        public void Save(Storage storage)
        {
            // in trial mode do not save earned awardments
            if (Guide.IsTrialMode)
            {
                return;
            }

            foreach (KeyValuePair<string, List<Awardment>> entry in _awardments)
            {
                string awardmentPath = Path.Combine(AwardmentDirectory, entry.Key);
                Awardment[] awardments = entry.Value.ToArray();
                XmlStoreable<Awardment[]> awardmentXml = new XmlStoreable<Awardment[]>(awardmentPath, awardments);
                try
                {
                    storage.Save(awardmentXml);
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
                Awardment awardment = (Awardment)type.GetConstructor(new Type[0]).Invoke(new object[0]);
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

        private Dictionary<string, List<Awardment>> _awardments;

        private readonly List<Type> AwardmentTypes;
        private const string AwardmentDirectory = "Awardments";
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
