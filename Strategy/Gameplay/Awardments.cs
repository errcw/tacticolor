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
    [XmlInclude(typeof(FirstMatchWonAwardment))]
    [XmlInclude(typeof(ManyMatchesWonAwardment))]
    [XmlInclude(typeof(ManyMatchesPlayedAwardment))]
    [XmlInclude(typeof(FirstTerritoryCapturedAwardment))]
    [XmlInclude(typeof(ManyTerritoriesCapturedAwardment))]
    [XmlInclude(typeof(QuickTerritoryCaptureAwardment))]
    [XmlInclude(typeof(QuickWinAwardment))]
    [XmlInclude(typeof(VeryQuickWinAwardment))]
    [XmlInclude(typeof(MatchShortWinStreakAwardment))]
    [XmlInclude(typeof(MatchLongWinStreakAwardment))]
    [XmlInclude(typeof(ManyPiecesPlacedAwardment))]
    [XmlInclude(typeof(VeryManyPiecesPlacedAwardment))]
    [XmlInclude(typeof(QuickPiecePlacementAwardment))]
    [XmlInclude(typeof(EveryConfigurationAwardment))]
    [XmlInclude(typeof(RecaptureAwardment))]
    [XmlInclude(typeof(BrinkAwardment))]
    public abstract class Awardment
    {
        /// <summary>
        /// The name of this awardment.
        /// </summary>
        [XmlIgnore]
        public virtual string Name { get; protected set; }

        /// <summary>
        /// A description of how to acheive this awardment.
        /// </summary>
        [XmlIgnore]
        public virtual string Description { get; protected set; }

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
        public virtual void MatchEnded(Match match, PlayerId player, PlayerId? winner)
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

        public void MatchStarted(Match match, IDictionary<string, PlayerId> players)
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
                    _awardments[player.Key] = awardments;
                }
                foreach (Awardment awardment in awardments)
                {
                    awardment.MatchStarted(match, player.Value);
                }
            }
        }

        public void MatchEnded(PlayerId? winner)
        {
            foreach (var player in _players)
            {
                List<Awardment> awardments = _awardments[player.Key];
                foreach (Awardment awardment in awardments)
                {
                    awardment.MatchEnded(_match, player.Value, winner);
                }
            }
        }

        /// <summary>
        /// Loads the awardment state for all gamers from storage.
        /// </summary>
        public void Load(Storage storage)
        {
            try
            {
                foreach (string file in storage.GetFiles(AwardmentDirectory))
                {
                    XmlStoreable<Awardment[]> awardmentXml = new XmlStoreable<Awardment[]>(file);
                    string gamertag = Path.GetFileNameWithoutExtension(file);
                    bool loadedExistingAwardments = storage.Load(awardmentXml);
                    if (loadedExistingAwardments)
                    {
                        List<Awardment> awardments = new List<Awardment>(awardmentXml.Data);
                        AddMissingAwardments(awardments, AwardmentTypes);
                        WireAwardmentEarnedEvents(awardments, gamertag);
                        _awardments[gamertag] = awardments;
                    }
                }
            }
            catch (Exception e)
            {
                Debug.WriteLine(e);
                // simply create new awardments
            }
        }

        /// <summary>
        /// Saves the awardment state from all gamers to storage.
        /// </summary>
        public void Save(Storage storage)
        {
            try
            {
                foreach (var entry in _awardments)
                {
                    string awardmentPath = Path.Combine(AwardmentDirectory, entry.Key);
                    Awardment[] awardments = entry.Value.ToArray();
                    XmlStoreable<Awardment[]> awardmentXml = new XmlStoreable<Awardment[]>(awardmentPath, awardments);
                    storage.Save(awardmentXml);
                }
            }
            catch (Exception e)
            {
                Debug.WriteLine(e);
            }
        }

        /// <summary>
        /// Returns the awardments for the given gamertag.
        /// </summary>
        public List<Awardment> GetAwardments(string gamertag)
        {
            List<Awardment> awardments = null;
            if (!_awardments.TryGetValue(gamertag, out awardments))
            {
                awardments = CreateAwardments(AwardmentTypes);
            }
            return awardments;
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
            var missingTypes = types.Except(awardmentTypes);
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

    public abstract class MatchesPlayedAwardment : Awardment
    {
        public int MatchCount { get; set; }

        public MatchesPlayedAwardment(int threshold, bool includeLosses)
        {
            MatchThreshold = threshold;
            MatchCountIncludesLosses = includeLosses;
            MatchCount = 0;
        }

        public override void MatchEnded(Match match, PlayerId player, PlayerId? winner)
        {
            if (winner != null && (player == winner || MatchCountIncludesLosses))
            {
                MatchCount += 1;
            }
            if (MatchCount >= MatchThreshold)
            {
                SetEarned();
            }
        }

        private readonly int MatchThreshold;
        private readonly bool MatchCountIncludesLosses;
    }

    public class FirstMatchWonAwardment : MatchesPlayedAwardment
    {
        public FirstMatchWonAwardment() : base(1, false)
        {
            Name = Resources.AwardmentFirstMatchWonName;
            Description = Resources.AwardmentFirstMatchWonDescription;
        }
    }

    public class ManyMatchesWonAwardment : MatchesPlayedAwardment
    {
        public override string Description
        {
            get { return String.Format(Resources.AwardmentManyMatchesWonDescription, MatchCount, 100); }
        }

        public ManyMatchesWonAwardment() : base(100, false)
        {
            Name = Resources.AwardmentManyMatchesWonName;
        }
    }

    public class ManyMatchesPlayedAwardment : MatchesPlayedAwardment
    {
        public override string Description
        {
            get { return String.Format(Resources.AwardmentManyMatchesPlayedDescription, MatchCount, 100); }
        }

        public ManyMatchesPlayedAwardment() : base(100, true)
        {
            Name = Resources.AwardmentManyMatchesPlayedName;
        }
    }

    public class TerritoryCaptureAwardment : Awardment
    {
        public int TerritoriesCaptured { get; set; }

        public TerritoryCaptureAwardment(int threshold)
        {
            TerritoryThreshold = threshold;
            TerritoriesCaptured = 0;
        }

        public override void MatchStarted(Match match, PlayerId player)
        {
            match.TerritoryAttacked += delegate(object matchObj, TerritoryAttackedEventArgs args)
            {
                if (args.Successful && args.Attacker.Owner == player)
                {
                    TerritoriesCaptured += 1;
                }
                if (TerritoriesCaptured >= TerritoryThreshold)
                {
                    SetEarned();
                }
            };
        }

        private readonly int TerritoryThreshold;
    }

    public class FirstTerritoryCapturedAwardment : TerritoryCaptureAwardment
    {
        public FirstTerritoryCapturedAwardment() : base(1)
        {
            Name = Resources.AwardmentFirstTerritoryCapturedName;
            Description = Resources.AwardmentFirstTerritoryCapturedDescription;
        }
    }

    public class ManyTerritoriesCapturedAwardment : TerritoryCaptureAwardment
    {
        public override string Description
        {
            get { return String.Format(Resources.AwardmentManyTerritoriesCapturedDescription, TerritoriesCaptured, 1000); }
        }

        public ManyTerritoriesCapturedAwardment() : base(1000)
        {
            Name = Resources.AwardmentManyTerritoriesCapturedName;
        }
    }

    public class QuickTerritoryCaptureAwardment : Awardment
    {
        public QuickTerritoryCaptureAwardment()
        {
            Name = Resources.AwardmentQuickTerritoryCaptureName;
            Description = Resources.AwardmentQuickTerritoryCaptureDescription;
        }

        public override void MatchStarted(Match match, PlayerId player)
        {
            match.TerritoryAttacked += delegate(object matchObj, TerritoryAttackedEventArgs args)
            {
                if (args.Successful && args.Attacker.Owner == player)
                {
                    if (_captureCount == 0)
                    {
                        _firstCaptureTime = match.Time;
                    }

                    _captureCount += 1;

                    long dt = match.Time - _firstCaptureTime;
                    if (dt > TimeoutThreshold)
                    {
                        // reset the baseline to the current capture
                        _captureCount = 1;
                        _firstCaptureTime = match.Time;
                    }

                    if (_captureCount >= CaptureThreshold)
                    {
                        SetEarned();
                    }
                }
            };
        }

        private long _firstCaptureTime;
        private int _captureCount = 0;

        private const int TimeoutThreshold = 3000;
        private const int CaptureThreshold = 3;
    }

    public abstract class MatchTimeAwardment : Awardment
    {
        public MatchTimeAwardment(int threshold)
        {
            MatchTimeThreshold = threshold;
        }

        public override void MatchEnded(Match match, PlayerId player, PlayerId? winner)
        {
            if (match.Time < MatchTimeThreshold && player == winner)
            {
                SetEarned();
            }
        }

        private readonly int MatchTimeThreshold;
    }

    public class QuickWinAwardment : MatchTimeAwardment
    {
        public QuickWinAwardment() : base(2 * 60 * 1000)
        {
            Name = Resources.AwardmentQuickWinName;
            Description = Resources.AwardmentQuickWinDescription;
        }
    }

    public class VeryQuickWinAwardment : MatchTimeAwardment
    {
        public VeryQuickWinAwardment() : base(1 * 60 * 1000)
        {
            Name = Resources.AwardmentVeryQuickWinName;
            Description = Resources.AwardmentVeryQuickWinDescription;
        }
    }

    public abstract class MatchWinStreakAwardment : Awardment
    {
        public int MatchesWon { get; set; }

        public MatchWinStreakAwardment(int threshold)
        {
            MatchWinStreakThreshold = threshold;
            MatchesWon = 0;
        }

        public override void MatchEnded(Match match, PlayerId player, PlayerId? winner)
        {
            if (player == winner)
            {
                MatchesWon += 1;
                if (MatchesWon >= MatchWinStreakThreshold)
                {
                    SetEarned();
                }
            }
            else
            {
                // reset the streak
                MatchesWon = 0;
            }
        }

        private readonly int MatchWinStreakThreshold;
    }

    public class MatchShortWinStreakAwardment : MatchWinStreakAwardment
    {
        public override string Description
        {
            get { return String.Format(Resources.AwardmentShortStreakDescription, MatchesWon, 3); }
        }

        public MatchShortWinStreakAwardment() : base(3)
        {
            Name = Resources.AwardmentShortStreakName;
        }
    }

    public class MatchLongWinStreakAwardment : MatchWinStreakAwardment
    {
        public override string Description
        {
            get { return String.Format(Resources.AwardmentLongStreakDescription, MatchesWon, 20); }
        }

        public MatchLongWinStreakAwardment() : base(20)
        {
            Name = Resources.AwardmentLongStreakName;
        }
    }

    public class PiecePlacementAwardment : Awardment
    {
        public int PiecesPlaced { get; set; }

        public PiecePlacementAwardment(int threshold)
        {
            PiecesPlacedThreshold = threshold;
            PiecesPlaced = 0;
        }

        public override void MatchStarted(Match match, PlayerId player)
        {
            match.PiecePlaced += delegate(object matchObj, PiecePlacedEventArgs args)
            {
                if (args.Location.Owner == player)
                {
                    PiecesPlaced += 1;
                }
                if(PiecesPlaced >= PiecesPlacedThreshold)
                {
                    SetEarned();
                }
            };
        }

        private readonly int PiecesPlacedThreshold;
    }

    public class ManyPiecesPlacedAwardment : PiecePlacementAwardment
    {
        public override string Description
        {
            get { return String.Format(Resources.AwardmentManyPiecesPlacedDescription, PiecesPlaced, 250); }
        }

        public ManyPiecesPlacedAwardment() : base(250)
        {
            Name = Resources.AwardmentManyPiecesPlacedName;
        }
    }

    public class VeryManyPiecesPlacedAwardment : PiecePlacementAwardment
    {
        public override string Description
        {
            get { return String.Format(Resources.AwardmentVeryManyPiecesPlacedDescription, PiecesPlaced, 1000); }
        }

        public VeryManyPiecesPlacedAwardment() : base(1000)
        {
            Name = Resources.AwardmentVeryManyPiecesPlacedName;
        }
    }

    public class QuickPiecePlacementAwardment : Awardment
    {
        public QuickPiecePlacementAwardment()
        {
            Name = Resources.AwardmentQuickPiecePlacementName;
            Description = Resources.AwardmentQuickPiecePlacementDescription;
        }

        public override void MatchStarted(Match match, PlayerId player)
        {
            match.PiecePlaced += delegate(object matchObj, PiecePlacedEventArgs args)
            {
                if (args.Location.Owner.Value != player)
                {
                    return;
                }

                if (_placementCount == 0)
                {
                    _firstPlacementTime = match.Time;
                }

                _placementCount += 1;

                long dt = match.Time - _firstPlacementTime;
                if (dt > TimeoutThreshold)
                {
                    // reset the baseline to the current capture
                    _placementCount = 1;
                    _firstPlacementTime = match.Time;
                }

                if (_placementCount >= PlacementThreshold)
                {
                    SetEarned();
                }
            };
        }

        private long _firstPlacementTime;
        private int _placementCount = 0;

        private const int TimeoutThreshold = 1000;
        private const int PlacementThreshold = 5;
    }

    public class EveryConfigurationAwardment : Awardment
    {
        public bool[][] Configurations { get; set; }

        public override string Description
        {
            get { return String.Format(Resources.AwardmentEveryConfigurationDescription, GetPlayedConfigurationCount(), MapTypeCount * MapSizeCount); }
        }

        public EveryConfigurationAwardment()
        {
            Name = Resources.AwardmentEveryConfigurationName;
            Description = Resources.AwardmentEveryConfigurationDescription;

            Configurations = new bool[MapSizeCount][];
            for (int i = 0; i < Configurations.Length; i++)
            {
                Configurations[i] = new bool[MapTypeCount];
            }
        }

        public override void MatchStarted(Match match, PlayerId player)
        {
            int mapSizeIdx = 0;
            switch (match.Map.Territories.Count)
            {
                case (int)MapSize.Tiny: mapSizeIdx = 0; break;
                case (int)MapSize.Small: mapSizeIdx = 1; break;
                case (int)MapSize.Normal: mapSizeIdx = 2; break;
                case (int)MapSize.Large: mapSizeIdx = 3; break;
            }

            int mapTypeIdx = 0;
            if (match.Map.Territories.Count(t => t.Owner == player) == 1)
            {
                mapTypeIdx = 1;
            }

            Configurations[mapSizeIdx][mapTypeIdx] = true;

            if (GetPlayedConfigurationCount() == MapSizeCount * MapTypeCount)
            {
                SetEarned();
            }
        }

        private int GetPlayedConfigurationCount()
        {
            int count = 0;
            for (int s = 0; s < MapSizeCount; s++)
            {
                for (int t = 0; t < MapTypeCount; t++)
                {
                    if (Configurations[s][t])
                    {
                        count += 1;
                    }
                }
            }
            return count;
        }

        private const int MapSizeCount = 4;
        private const int MapTypeCount = 2;
    }

    public class RecaptureAwardment : Awardment
    {
        public RecaptureAwardment()
        {
            Name = Resources.AwardmentRecaptureName;
            Description = Resources.AwardmentRecaptureDescription;
        }

        public override void MatchStarted(Match match, PlayerId player)
        {
            Dictionary<Territory, int> territoryCaptureCount = new Dictionary<Territory,int>();
            match.TerritoryAttacked += delegate(object matchObj, TerritoryAttackedEventArgs args)
            {
                if (args.Successful && args.Attacker.Owner == player)
                {
                    int captureCount;
                    if (!territoryCaptureCount.TryGetValue(args.Defender, out captureCount))
                    {
                        captureCount = 0;
                    }

                    captureCount += 1;
                    if (captureCount == CaptureThreshold)
                    {
                        SetEarned();
                    }

                    territoryCaptureCount[args.Defender] = captureCount;
                }
            };
        }

        private const int CaptureThreshold = 10;
    }

    public class BrinkAwardment : Awardment
    {
        public BrinkAwardment()
        {
            Name = Resources.AwardmentBrinkName;
            Description = Resources.AwardmentBrinkDescription;
        }

        public override void MatchStarted(Match match, PlayerId player)
        {
            _oneTerritoryLeft = false;
            match.TerritoryAttacked += delegate(object matchObj, TerritoryAttackedEventArgs args)
            {
                // check for 2 because the lost territory hasn't been counted yet
                PlayerId defender = args.Defenders.First().Piece.Owner;
                if (defender == player && args.Successful && match.TerritoriesOwnedCount[(int)player] == 2)
                {
                    _oneTerritoryLeft = true;
                }
            };
        }

        public override void MatchEnded(Match match, PlayerId player, PlayerId? winner)
        {
            if (player == winner && _oneTerritoryLeft)
            {
                SetEarned();
            }
        }

        private bool _oneTerritoryLeft;
    }
}
