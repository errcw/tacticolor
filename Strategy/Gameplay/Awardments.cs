using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml.Serialization;

using Strategy.Properties;
using Strategy.Library.Storage;

namespace Strategy.Gameplay
{
    /// <summary>
    /// An acheivement.
    /// </summary>
    [XmlInclude(typeof(FirstTerritoryCaptureAwardment))]
    [XmlInclude(typeof(FirstWinAwardment))]
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
        /// Occurs when this awardments is earned.
        /// </summary>
        public event EventHandler<EventArgs> Earned;

        /// <summary>
        /// The gamertag owning this awardment.
        /// </summary>
        [XmlIgnore]
        public string OwnerGamertag { get; set; }

        /// <summary>
        /// Updates the state of this awardment when a match starts.
        /// </summary>
        public virtual void MatchStarted(Match match, PlayerId player)
        {
        }

        /// <summary>
        /// Updates the state of this awardment when a match ends.
        /// </summary>
        public virtual void MatchEnded(Match match, PlayerId player, PlayerId winner)
        {
        }

        /// <summary>
        /// Notifies this awardment and its listeners that it was earned.
        /// </summary>
        protected void SetEarned()
        {
            if (!IsEarned) // filter multiple earned calls
            {
                IsEarned = true;
                if (Earned != null)
                {
                    Earned(this, EventArgs.Empty);
                }
            }
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

        public void MatchStarted(IDictionary<string, PlayerId> players, Match match)
        {
            _match = match;
            _players = players;
            foreach (var player in _players)
            {
                List<Awardment> awardments = null;
                if (!_awardments.TryGetValue(player.Key, out awardments))
                {
                    // no existing awardments for this gamer, create new ones
                    awardments = CreateAwardments(AwardmentTypes);
                    WireAwardmentEarnedEvents(awardments, player.Key);
                }
                foreach (Awardment awardment in awardments)
                {
                    if (!awardment.IsEarned)
                    {
                        awardment.MatchStarted(match, player.Value);
                    }
                }
            }
        }

        public void MatchEnded(PlayerId winner)
        {
            foreach (var player in _players)
            {
                List<Awardment> awardments = _awardments[player.Key];
                foreach (Awardment awardment in awardments)
                {
                    if (!awardment.IsEarned)
                    {
                        awardment.MatchEnded(_match, player.Value, winner);
                    }
                }
            }
        }

        /// <summary>
        /// Loads the awardment state for all gamers from storage.
        /// </summary>
        public void Load(Storage storage)
        {
            if (!Directory.Exists(AwardmentDirectory))
            {
                // bail early if there are no awardments to load
                return;
            }
            foreach (string file in Directory.GetFiles(AwardmentDirectory))
            {
                XmlStoreable<Awardment[]> awardmentXml = new XmlStoreable<Awardment[]>(file);
                string gamertag = Path.GetFileNameWithoutExtension(file);
                try
                {
                    storage.Load(awardmentXml);
                    List<Awardment> awardments = new List<Awardment>(awardmentXml.Data);
                    AddMissingAwardments(awardments, AwardmentTypes);
                    WireAwardmentEarnedEvents(awardments, gamertag);
                    _awardments[gamertag] = awardments;
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
            foreach (var entry in _awardments)
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

        /// <summary>
        /// Adds a listener to propagate the earned event to the aggregate event.
        /// </summary>
        private void WireAwardmentEarnedEvents(List<Awardment> awardments, string ownerGamertag)
        {
            foreach (Awardment awardment in awardments)
            {
                awardment.OwnerGamertag = ownerGamertag;
                awardment.Earned += OnAwardmentEarned;
            }
        }

        private void OnAwardmentEarned(object awardmentObj, EventArgs args)
        {
            Awardment awardment = (Awardment)awardmentObj;
            if (AwardmentEarned != null)
            {
                AwardmentEarned(this, new AwardmentEventArgs(awardment));
            }
        }

        private Dictionary<string, List<Awardment>> _awardments;

        private Match _match;
        private IDictionary<string, PlayerId> _players;

        private readonly List<Type> AwardmentTypes;
        private const string AwardmentDirectory = "Awardments";
    }

    /// <summary>
    /// Event arguments when an awardment is earned.
    /// </summary>
    public class AwardmentEventArgs : EventArgs
    {
        public Awardment Awardment { get; private set; }
        public AwardmentEventArgs(Awardment awardment)
        {
            Awardment = awardment;
        }
    }

    public class FirstWinAwardment : Awardment
    {
        public FirstWinAwardment()
        {
            Name = Resources.AwardmentFirstWinName;
            Description = Resources.AwardmentFirstWinDescription;
        }

        public override void MatchEnded(Match match, PlayerId player, PlayerId winner)
        {
            if (player == winner)
            {
                SetEarned();
            }
        }
    }

    public class FirstTerritoryCaptureAwardment : Awardment
    {
        public FirstTerritoryCaptureAwardment()
        {
            Name = Resources.AwardmentFirstTerritoryName;
            Description = Resources.AwardmentFirstTerritoryDescription;
        }

        public override void MatchStarted(Match match, PlayerId player)
        {
            match.TerritoryAttacked += delegate(object matchObj, TerritoryAttackedEventArgs args)
            {
                if (args.Successful && args.Attacker.Owner == player && args.Defender.Owner != null)
                {
                    SetEarned();
                }
            };
        }
    }

    public class ManyMatchesAwardment : Awardment
    {
        public int MatchesPlayed { get; set; }

        public ManyMatchesAwardment()
        {
            Name = Resources.AwardmentManyMatchesName;
            Description = Resources.AwardmentManyMatchesDescription;
        }

        public override void MatchEnded(Match match, PlayerId player, PlayerId winner)
        {
            MatchesPlayed += 1;
            if (MatchesPlayed == 100)
            {
                SetEarned();
            }
        }
    }
}
