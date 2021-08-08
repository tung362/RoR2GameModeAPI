using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.Networking;
using R2API.Utils;
using RoR2;

namespace RoR2GameModeAPI
{
    /// <summary>
    /// API implementing a voting system to multiplayer and singleplayer lobbies with support for vanilla and modded clients along with multiple mod support.
    /// Handles registrations of custom vote polls, headers, selections, and choices
    /// <para>Fully networked custom integration and expansion of the base game's vote system</para>
    /// </summary>
    public static class VoteAPI
    {
        #region Format
        /// <summary>
        /// Vote result entry
        /// </summary>
        public class VoteResult
        {
            /// <summary>
            /// Poll results of each selection
            /// </summary>
            public VoteMask Vote = VoteMask.none;
            /// <summary>
            /// Extra data results of each selection
            /// </summary>
            public Dictionary<string, object> VoteExtraDatas = new Dictionary<string, object>();
        }
        #endregion

        /*Cache*/
        //Result
        /// <summary>
        /// All vote results after leaving lobby scene and game session started
        /// <para>Key pollName, Value voteResult</para>
        /// </summary>
        private static Dictionary<string, VoteResult> _VoteResults = new Dictionary<string, VoteResult>();

        //Pending vote elements to be registered
        /// <summary>
        /// All top headers requested for registration
        /// </summary>
        private static List<RuleCategoryDef> _VoteHeadersTop = new List<RuleCategoryDef>();
        /// <summary>
        /// All bottom headers requested for registration
        /// </summary>
        private static List<RuleCategoryDef> _VoteHeadersBottom = new List<RuleCategoryDef>();
        /// <summary>
        /// All selections requested for registration
        /// </summary>
        private static List<RuleDef> _VoteSelections = new List<RuleDef>();

        //Registered vote elements
        /// <summary>
        /// All registered headers
        /// </summary>
        private static readonly HashSet<RuleCategoryDef> RegisteredVoteHeaders = new HashSet<RuleCategoryDef>();
        /// <summary>
        /// All registered selections
        /// </summary>
        private static readonly List<RuleDef> RegisteredVoteSelections = new List<RuleDef>();
        /// <summary>
        /// All registered choices
        /// </summary>
        private static readonly List<RuleChoiceDef> RegisteredVoteChoices = new List<RuleChoiceDef>();

        //Network proxy
        /// <summary>
        /// Pending network packets to be written from RuleBook
        /// </summary>
        private static List<Tuple<string, string>> RuleBookPendingWrites = new List<Tuple<string, string>>();
        /// <summary>
        /// Pending network packets to be written from RuleMask
        /// </summary>
        private static List<Tuple<string, bool>> RuleMaskPendingWrites = new List<Tuple<string, bool>>();
        /// <summary>
        /// Pending network packets to be written from RuleChoiceMask
        /// </summary>
        private static List<Tuple<string, bool>> RuleChoiceMaskPendingWrites = new List<Tuple<string, bool>>();
        /// <summary>
        /// Pending RuleBook to read custom packets to
        /// </summary>
        private static RuleBook TargetedRuleBook;
        /// <summary>
        /// Pending RuleMask to read custom packets to
        /// </summary>
        private static RuleMask TargetedRuleMask;
        /// <summary>
        /// Pending RuleChoiceMask to read custom packets to
        /// </summary>
        private static RuleChoiceMask TargetedRuleChoiceMask;
        /// <summary>
        /// Cached vote results when still in lobby scene
        /// </summary>
        private static Dictionary<string, string> SelectionResults = new Dictionary<string, string>();

        /*Properties*/
        //Result
        /// <summary>
        /// All vote results after leaving lobby scene and game session started
        /// <para>Key pollName, Value voteResult</para>
        /// </summary>
        public static IReadOnlyDictionary<string, VoteResult> VoteResults { get { return _VoteResults; } }

        //Pending vote elements to be registered
        /// <summary>
        /// All top headers requested for registration
        /// </summary>
        public static IReadOnlyList<RuleCategoryDef> VoteHeadersTop { get { return _VoteHeadersTop; } }
        /// <summary>
        /// All bottom headers requested for registration
        /// </summary>
        public static IReadOnlyList<RuleCategoryDef> VoteHeadersBottom { get { return _VoteHeadersBottom; } }
        /// <summary>
        /// All selections requested for registration
        /// </summary>
        public static IReadOnlyList<RuleDef> VoteSelections { get { return _VoteSelections; } }

        /// <summary>
        /// Set core hooks
        /// </summary>
        internal static void SetHook()
        {
            On.RoR2.RuleCatalog.Init += RegisterVotes;
            Run.onRunSetRuleBookGlobal += ApplyVotes;
            On.RoR2.VoteController.OnSerialize += VoteControllerSerialize;
            On.RoR2.VoteController.OnDeserialize += VoteControllerDeserialize;
            On.RoR2.PreGameRuleVoteController.WriteVotes += PreGameRuleVoteControllerSerialize;
            On.RoR2.PreGameRuleVoteController.ReadVotes += PreGameRuleVoteControllerDeserialize;
            On.RoR2.NetworkExtensions.Write_NetworkWriter_RuleBook += NetworkRuleBookSerialize;
            On.RoR2.NetworkExtensions.ReadRuleBook += NetworkRuleBookDeserialize;
            On.RoR2.RuleMask.Serialize += RuleMaskSerialize;
            On.RoR2.RuleMask.Deserialize += RuleMaskDeserialize;
            On.RoR2.RuleChoiceMask.Serialize += RuleChoiceMaskSerialize;
            On.RoR2.RuleChoiceMask.Deserialize += RuleChoiceMaskDeserialize;
        }

        /// <summary>
        /// Unset core hooks
        /// </summary>
        internal static void UnsetHook()
        {
            On.RoR2.RuleCatalog.Init -= RegisterVotes;
            Run.onRunSetRuleBookGlobal -= ApplyVotes;
            On.RoR2.VoteController.OnSerialize -= VoteControllerSerialize;
            On.RoR2.VoteController.OnDeserialize -= VoteControllerDeserialize;
            On.RoR2.PreGameRuleVoteController.WriteVotes -= PreGameRuleVoteControllerSerialize;
            On.RoR2.PreGameRuleVoteController.ReadVotes -= PreGameRuleVoteControllerDeserialize;
            On.RoR2.NetworkExtensions.Write_NetworkWriter_RuleBook -= NetworkRuleBookSerialize;
            On.RoR2.NetworkExtensions.ReadRuleBook -= NetworkRuleBookDeserialize;
            On.RoR2.RuleBook.Serialize -= RuleBookSerialize;
            On.RoR2.RuleBook.Deserialize -= RuleBookDeserialize;
            On.RoR2.RuleMask.Serialize -= RuleMaskSerialize;
            On.RoR2.RuleMask.Deserialize -= RuleMaskDeserialize;
            On.RoR2.RuleChoiceMask.Serialize -= RuleChoiceMaskSerialize;
            On.RoR2.RuleChoiceMask.Deserialize -= RuleChoiceMaskDeserialize;
        }

        #region Hooks
        /// <summary>
        /// Hook for registering pending vote elements
        /// </summary>
        static void RegisterVotes(On.RoR2.RuleCatalog.orig_Init orig)
        {
            foreach (RuleCategoryDef header in _VoteHeadersTop) RegisterVoteHeader(header);
            orig();
            foreach(RuleCategoryDef header in _VoteHeadersBottom) RegisterVoteHeader(header);
            foreach (RuleDef selection in _VoteSelections) RegisterVoteSelection(selection);

            //Filler due to last 2 indexes never showing up
            //RuleCategoryDef dummyHeader = VoteAPI.AddVoteHeader("Dummy", new Color(0.0f, 0.0f, 0.0f, 1.0f), true);
            //RegisterVoteHeader(dummyHeader);

            //RuleDef dummySelection1 = VoteAPI.AddVoteSelection(dummyHeader, "Dummy1", new ChoiceMenu("Dummy1 On", new Color(0.0f, 0.0f, 0.0f, 0.4f), "", Color.black, "Textures/ArtifactIcons/texCommandSmallSelected", "artifact_dummy", -1));
            //VoteAPI.AddVoteChoice(dummySelection1, new ChoiceMenu("Dummy1 Off", new Color(0.0f, 0.0f, 0.0f, 0.4f), "", Color.black, "Textures/ArtifactIcons/texCommandSmallDeselected", "artifact_dummy", -1));
            //dummySelection1.defaultChoiceIndex = 0;
            //RegisterVoteSelection(dummySelection1);

            //RuleDef dummySelection2 = VoteAPI.AddVoteSelection(dummyHeader, "Dummy2", new ChoiceMenu("Dummy2 On", new Color(0.0f, 0.0f, 0.0f, 0.4f), "", Color.black, "Textures/ArtifactIcons/texCommandSmallSelected", "artifact_dummy", -1));
            //VoteAPI.AddVoteChoice(dummySelection2, new ChoiceMenu("Dummy2 Off", new Color(0.0f, 0.0f, 0.0f, 0.4f), "", Color.black, "Textures/ArtifactIcons/texCommandSmallDeselected", "artifact_dummy", -1));
            //dummySelection2.defaultChoiceIndex = 0;
            //RegisterVoteSelection(dummySelection2);
        }

        /// <summary>
        /// Hook for applying all vote results to _VoteResults
        /// </summary>
        static void ApplyVotes(Run self, RuleBook newRuleBook)
        {
            foreach (string key in _VoteResults.Keys)
            {
                _VoteResults[key].Vote = VoteMask.none;
                _VoteResults[key].VoteExtraDatas.Clear();
            }
            foreach (KeyValuePair<string, string> kv in SelectionResults)
            {
                RuleDef ruleDef = RuleCatalog.FindRuleDef(kv.Key);
                if (ruleDef != null)
                {
                    VoteChoiceDef choiceDef = (VoteChoiceDef)ruleDef.FindChoice(kv.Value);
                    if (choiceDef != null)
                    {
                        if (choiceDef.VoteIndex > 0) _VoteResults[choiceDef.VotePollKey].Vote.AddVote(choiceDef.VoteIndex);
                        _VoteResults[choiceDef.VotePollKey].VoteExtraDatas.Add(kv.Key, choiceDef.extraData);
                    }
                }
            }
        }

        /// <summary>
        /// Hook for writing custom packets of RuleBook and RuleChoiceMask, serialize end point
        /// </summary>
        static bool VoteControllerSerialize(On.RoR2.VoteController.orig_OnSerialize orig, VoteController self, NetworkWriter writer, bool forceAll)
        {
            bool result = orig(self, writer, forceAll);
            if(!Run.instance)
            {
                writer.Write(RuleBookPendingWrites.Count);
                writer.Write(RuleChoiceMaskPendingWrites.Count);
                for (int i = 0; i < RuleBookPendingWrites.Count; i++)
                {
                    writer.Write(RuleBookPendingWrites[i].Item1);
                    writer.Write(RuleBookPendingWrites[i].Item2);
                }
                for (int i = 0; i < RuleChoiceMaskPendingWrites.Count; i++)
                {
                    writer.Write(RuleChoiceMaskPendingWrites[i].Item1);
                    writer.Write(RuleChoiceMaskPendingWrites[i].Item2);
                }
            }
            RuleBookPendingWrites.Clear();
            RuleChoiceMaskPendingWrites.Clear();
            return result;
        }

        /// <summary>
        /// Hook for reading custom packets of RuleBook and RuleChoiceMask, deserialize end point
        /// </summary>
        static void VoteControllerDeserialize(On.RoR2.VoteController.orig_OnDeserialize orig, VoteController self, NetworkReader reader, bool initialState)
        {
            orig(self, reader, initialState);
            if (reader.Position < reader.Length && !Run.instance)
            {
                int ruleBookCount = reader.ReadInt32();
                int ruleChoiceMaskCount = reader.ReadInt32();
                if (TargetedRuleBook != null)
                {
                    SelectionResults.Clear();
                    byte[] ruleBookBytes = TargetedRuleBook.GetFieldValue<byte[]>("ruleValues");
                    for (int i = 0; i < ruleBookCount; i++)
                    {
                        string globalName = reader.ReadString();
                        string localName = reader.ReadString();
                        RuleDef ruleDef = RuleCatalog.FindRuleDef(globalName);
                        if (ruleDef != null)
                        {
                            RuleChoiceDef choiceDef = ruleDef.FindChoice(localName);
                            if(choiceDef != null) ruleBookBytes[ruleDef.globalIndex] = (byte)choiceDef.localIndex;
                            else ruleBookBytes[ruleDef.globalIndex] = (byte)ruleDef.defaultChoiceIndex;
                            SelectionResults.Add(globalName, localName);
                        }
                    }
                    TargetedRuleBook = null;
                }

                if (TargetedRuleChoiceMask != null)
                {
                    for (int i = 0; i < ruleChoiceMaskCount; i++)
                    {
                        string globalName = reader.ReadString();
                        bool mask = reader.ReadBoolean();
                        RuleChoiceDef choiceDef = RuleCatalog.FindChoiceDef(globalName);
                        if(choiceDef != null) TargetedRuleChoiceMask[choiceDef] = mask;
                    }
                    TargetedRuleChoiceMask = null;
                }
            }
        }

        /// <summary>
        /// Hook for writing custom packets of RuleMask, also handles vanilla writes
        /// </summary>
        static void PreGameRuleVoteControllerSerialize(On.RoR2.PreGameRuleVoteController.orig_WriteVotes orig, PreGameRuleVoteController self, NetworkWriter writer)
        {
            Array votes = self.GetType().GetField("votes", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(self) as Array;
            RuleMask ruleMaskBuffer = self.GetFieldValue<RuleMask>("ruleMaskBuffer");

            for (int i = 0; i < RuleCatalog.ruleCount; i++) ruleMaskBuffer[i] = votes.GetValue(i).GetPropertyValue<bool>("hasVoted");
            writer.Write(ruleMaskBuffer);
            for (int i = 0; i < RuleCatalog.ruleCount - RegisteredVoteSelections.Count; i++)
            {
                if (votes.GetValue(i).GetPropertyValue<bool>("hasVoted")) votes.GetValue(i).GetType().InvokeMethod("Serialize", writer, votes.GetValue(i));
            }

            writer.Write(RuleMaskPendingWrites.Count);
            for (int i = 0; i < RuleMaskPendingWrites.Count; i++)
            {
                writer.Write(RuleMaskPendingWrites[i].Item1);
                writer.Write(RuleMaskPendingWrites[i].Item2);
            }

            Dictionary<string, string> votePendingWrites = new Dictionary<string, string>();
            for (int i = 0; i < RegisteredVoteSelections.Count; i++)
            {
                byte internalValue = (byte)votes.GetValue(RegisteredVoteSelections[i].globalIndex).GetType().GetField("internalValue", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(votes.GetValue(RegisteredVoteSelections[i].globalIndex));
                int choiceIndex = internalValue - 1;
                votePendingWrites.Add(RegisteredVoteSelections[i].globalName, choiceIndex >= 0 ? RegisteredVoteSelections[i].choices[choiceIndex].localName : "");
            }

            writer.Write(votePendingWrites.Count);
            foreach (KeyValuePair<string, string> kv in votePendingWrites)
            {
                writer.Write(kv.Key);
                writer.Write(kv.Value);
            }
            RuleMaskPendingWrites.Clear();
        }

        /// <summary>
        /// Hook for reading custom packets of RuleMask, also handles vanilla reads
        /// </summary>
        static void PreGameRuleVoteControllerDeserialize(On.RoR2.PreGameRuleVoteController.orig_ReadVotes orig, PreGameRuleVoteController self, NetworkReader reader)
        {
            Array votes = self.GetType().GetField("votes", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(self) as Array;
            RuleMask ruleMaskBuffer = self.GetFieldValue<RuleMask>("ruleMaskBuffer");

            reader.ReadRuleMask(ruleMaskBuffer);
            bool flag = !self.networkUserNetworkIdentity || !self.networkUserNetworkIdentity.isLocalPlayer;
            for (int i = 0; i < RuleCatalog.ruleCount - RegisteredVoteSelections.Count; i++)
            {
                object vote;
                if (ruleMaskBuffer[i]) vote = votes.GetValue(i).GetType().InvokeMethod<object>("Deserialize", reader);
                else vote = default(object);
                if (flag) votes.SetValue(vote, i);
            }

            if (TargetedRuleMask != null)
            {
                if(reader.Position < reader.Length)
                {
                    int ruleMaskCount = reader.ReadInt32();
                    for (int i = 0; i < ruleMaskCount; i++)
                    {
                        string globalName = reader.ReadString();
                        bool mask = reader.ReadBoolean();
                        RuleDef ruleDef = RuleCatalog.FindRuleDef(globalName);
                        if (ruleDef != null) TargetedRuleMask[ruleDef.globalIndex] = mask;
                    }

                    int voteCount = reader.ReadInt32();
                    for (int i = 0; i < voteCount; i++)
                    {
                        string globalName = reader.ReadString();
                        string localName = reader.ReadString();
                        RuleDef ruleDef = RuleCatalog.FindRuleDef(globalName);
                        if (ruleDef != null)
                        {
                            RuleChoiceDef choice = ruleDef.FindChoice(localName);
                            if(choice != null)
                            {
                                if (flag) self.SetVote(ruleDef.globalIndex, choice.localIndex);
                            }
                            else self.SetVote(ruleDef.globalIndex, -1);

                            byte boop2 = (byte)votes.GetValue(ruleDef.globalIndex).GetType().GetField("internalValue", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(votes.GetValue(ruleDef.globalIndex));
                        }
                    }
                }
                TargetedRuleMask = null;
            }

            self.GetType().GetField("shouldUpdateGameVotes", BindingFlags.NonPublic | BindingFlags.Static).SetValue(null, ((bool)self.GetType().GetField("shouldUpdateGameVotes", BindingFlags.NonPublic | BindingFlags.Static).GetValue(null) || flag));
            if (NetworkServer.active) self.SetDirtyBit(2u);
        }

        /// <summary>
        /// Hook to set RuleBook serialize hook
        /// </summary>
        static void NetworkRuleBookSerialize(On.RoR2.NetworkExtensions.orig_Write_NetworkWriter_RuleBook orig, NetworkWriter writer, RuleBook src)
        {
            On.RoR2.RuleBook.Serialize += RuleBookSerialize;
            orig(writer, src);
        }

        /// <summary>
        /// Hook to set RuleBook deserialize hook
        /// </summary>
        static void NetworkRuleBookDeserialize(On.RoR2.NetworkExtensions.orig_ReadRuleBook orig, NetworkReader reader, RuleBook dest)
        {
            On.RoR2.RuleBook.Deserialize += RuleBookDeserialize;
            orig(reader, dest);
        }

        /// <summary>
        /// Hook for caching SelectionResults along with custom write packets of RuleBook, and also handles vanilla writes
        /// </summary>
        static void RuleBookSerialize(On.RoR2.RuleBook.orig_Serialize orig, RuleBook self, NetworkWriter writer)
        {
            byte[] ruleValues = self.GetFieldValue<byte[]>("ruleValues");
            SelectionResults.Clear();
            for (int i = 0; i < ruleValues.Length - RegisteredVoteSelections.Count; i++) writer.Write(ruleValues[i]);
            for (int i = 0; i < RegisteredVoteSelections.Count; i++)
            {
                RuleChoiceDef choiceDef = RegisteredVoteSelections[i].choices[ruleValues[RegisteredVoteSelections[i].globalIndex]];
                RuleBookPendingWrites.Add(Tuple.Create(RegisteredVoteSelections[i].globalName, choiceDef.localName));
                SelectionResults.Add(RegisteredVoteSelections[i].globalName, choiceDef.localName);
            }
        }

        /// <summary>
        /// Hook for applying SelectionResults to the RuleBook, and also handles vanilla reads
        /// </summary>
        static void RuleBookDeserialize(On.RoR2.RuleBook.orig_Deserialize orig, RuleBook self, NetworkReader reader)
        {
            byte[] ruleValues = self.GetFieldValue<byte[]>("ruleValues");
            for (int i = 0; i < ruleValues.Length - RegisteredVoteSelections.Count; i++) ruleValues[i] = reader.ReadByte();
            foreach(KeyValuePair<string, string> kv in SelectionResults)
            {
                RuleDef ruleDef = RuleCatalog.FindRuleDef(kv.Key);
                if (ruleDef != null)
                {
                    RuleChoiceDef choiceDef = ruleDef.FindChoice(kv.Value);
                    if (choiceDef != null) ruleValues[ruleDef.globalIndex] = (byte)choiceDef.localIndex;
                    else ruleValues[ruleDef.globalIndex] = (byte)ruleDef.defaultChoiceIndex;
                }
            }
            TargetedRuleBook = self;
        }

        /// <summary>
        /// Hook for caching custom write packets of RuleMask, and also handles vanilla writes
        /// </summary>
        static void RuleMaskSerialize(On.RoR2.RuleMask.orig_Serialize orig, RuleMask self, NetworkWriter writer)
        {
            byte[] bytes = (byte[])self.GetType().BaseType.GetField("bytes", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(self);
            int difference = bytes.Length - ((RuleCatalog.ruleCount - RegisteredVoteSelections.Count) + 7 >> 3);
            for (int i = 0; i < bytes.Length - difference; i++) writer.Write(bytes[i]);
            for (int i = 0; i < RegisteredVoteSelections.Count; i++) RuleMaskPendingWrites.Add(Tuple.Create(RegisteredVoteSelections[i].globalName, self[RegisteredVoteSelections[i].globalIndex]));
        }

        /// <summary>
        /// Hook for caching TargetedRuleMask, and also handles vanilla reads
        /// </summary>
        static void RuleMaskDeserialize(On.RoR2.RuleMask.orig_Deserialize orig, RuleMask self, NetworkReader reader)
        {
            byte[] bytes = (byte[])self.GetType().BaseType.GetField("bytes", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(self);
            int difference = bytes.Length - ((RuleCatalog.ruleCount - RegisteredVoteSelections.Count) + 7 >> 3);
            for (int i = 0; i < bytes.Length - difference; i++) bytes[i] = reader.ReadByte();
            TargetedRuleMask = self;
        }

        /// <summary>
        /// Hook for caching custom write packets of RuleChoice, and also handles vanilla writes
        /// </summary>
        static void RuleChoiceMaskSerialize(On.RoR2.RuleChoiceMask.orig_Serialize orig, RuleChoiceMask self, NetworkWriter writer)
        {
            byte[] bytes = (byte[])self.GetType().BaseType.GetField("bytes", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(self);
            int difference = bytes.Length - ((RuleCatalog.choiceCount - RegisteredVoteChoices.Count) + 7 >> 3);
            for (int i = 0; i < bytes.Length - difference; i++) writer.Write(bytes[i]);
            for (int i = 0; i < RegisteredVoteChoices.Count; i++) RuleChoiceMaskPendingWrites.Add(Tuple.Create(RegisteredVoteChoices[i].globalName, self[RegisteredVoteChoices[i]]));
        }

        /// <summary>
        /// Hook for caching TargetedRuleChoiceMask, and also handles vanilla reads
        /// </summary>
        static void RuleChoiceMaskDeserialize(On.RoR2.RuleChoiceMask.orig_Deserialize orig, RuleChoiceMask self, NetworkReader reader)
        {
            byte[] bytes = (byte[])self.GetType().BaseType.GetField("bytes", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(self);
            int difference = bytes.Length - ((RuleCatalog.choiceCount - RegisteredVoteChoices.Count) + 7 >> 3);
            for (int i = 0; i < bytes.Length - difference; i++) bytes[i] = reader.ReadByte();
            TargetedRuleChoiceMask = self;
        }
        #endregion

        #region Helpers
        /// <summary>
        /// Registers a new vote poll
        /// </summary>
        /// <param name="pollName">Vote poll name, ensure each name is unique</param>
        public static void AddVotePoll(string pollName)
        {
            if (_VoteResults.ContainsKey(pollName))
            {
                Debug.LogWarning($"Warning! VoteMask name: \"{pollName}\" already registered, this can cause conflicts if another mod uses the same name! @GameModeAPI");
                return;
            }
            _VoteResults.Add(pollName, new VoteResult());
        }

        /// <summary>
        /// Queues request to register a new vote header
        /// </summary>
        /// <param name="categoryToken">Vote header name</param>
        /// <param name="color">Vote header color</param>
        /// <param name="addAfterVanillaHeaders">Set to false to have the vote header be on top of the vanilla headers</param>
        /// <returns>Return the newly created vote header for vote selections to reference</returns>
        public static RuleCategoryDef AddVoteHeader(string categoryToken, Color color, bool addAfterVanillaHeaders = false)
        {
            RuleCategoryDef header = new RuleCategoryDef
            {
                position = -1,
                displayToken = categoryToken,
                color = color,
                emptyTipToken = null,
                hiddenTest = new Func<bool>(HiddenTestFalse)
            };
            if(addAfterVanillaHeaders) _VoteHeadersBottom.Add(header);
            else _VoteHeadersTop.Add(header);
            return header;
        }

        /// <summary>
        /// Queues request to register a new vote selection
        /// </summary>
        /// <param name="header">The vote header this vote selection should belong to</param>
        /// <param name="selectionName">Vote selection name, ensure name is unique</param>
        /// <param name="choiceMenu">The initial vote choice settings for the vote selection</param>
        /// <returns>Return the newly created vote selection for vote choices to reference</returns>
        public static RuleDef AddVoteSelection(RuleCategoryDef header, string selectionName, ChoiceMenu choiceMenu)
        {
            RuleDef selection = new RuleDef("Votes." + selectionName, selectionName);
            selection.category = header;
            AddVoteChoice(selection, choiceMenu);
            header.children.Add(selection);
            _VoteSelections.Add(selection);
            return selection;
        }

        /// <summary>
        /// Queues request to register a new vote choice
        /// </summary>
        /// <param name="selection">The vote selection this vote choice should belong to</param>
        /// <param name="choiceMenu">Vote choice settings</param>
        public static void AddVoteChoice(RuleDef selection, ChoiceMenu choiceMenu)
        {
            VoteChoiceDef choice = CreateChoice(ref selection, choiceMenu.TooltipName, null, false);
            choice.tooltipNameToken = choiceMenu.TooltipName;
            choice.tooltipNameColor = choiceMenu.TooltipNameColor;
            choice.tooltipBodyToken = choiceMenu.TooltipBody;
            choice.tooltipBodyColor = choiceMenu.TooltipBodyColor;
            choice.spritePath = choiceMenu.IconPath;
            choice.VoteIndex = choiceMenu.ChoiceIndex;
            choice.VotePollKey = choiceMenu.VotePollKey;
            choice.extraData = choiceMenu.ExtraData;
        }
        #endregion

        #region Functions
        /// <summary>
        /// Registers a new vote header
        /// </summary>
        /// <param name="header">Queue requested header</param>
        static void RegisterVoteHeader(RuleCategoryDef header)
        {
            if(header.displayToken == null)
            {
                Debug.LogError("Failed to register header, categoryToken cannot be null! @GameModeAPI");
                return;
            }
            if(RegisteredVoteHeaders.Contains(header))
            {
                Debug.LogError("Failed to register header, cannot have duplicate header instances! @GameModeAPI");
                return;
            }
            (typeof(RuleCatalog)).GetFieldValue<List<RuleCategoryDef>>("allCategoryDefs").Add(header);
            RegisteredVoteHeaders.Add(header);
        }

        /// <summary>
        /// Registers a new vote selection along with it's vote choices
        /// </summary>
        /// <param name="selection">Queue requested selection</param>
        static void RegisterVoteSelection(RuleDef selection)
        {
            if(string.IsNullOrEmpty(selection.displayToken))
            {
                Debug.LogError($"Failed to register vote selection: \"{selection.globalName}\", selection name is null or empty! Ensure each selection has a unique name! @GameModeAPI");
                selection.category.children.Remove(selection);
                return;
            }
            if (RuleCatalog.FindRuleDef(selection.globalName) != null)
            {
                Debug.LogError($"Failed to register vote selection: \"{selection.globalName}\", entry already exists! Ensure each selection name is unique to prevent mod conflicts! @GameModeAPI");
                selection.category.children.Remove(selection);
                return;
            }
            if (!RegisteredVoteHeaders.Contains(selection.category))
            {
                Debug.LogError($"Failed to register vote selection: \"{selection.globalName}\", header is not registered! Ensure each selection uses a header called from VoteAPI.AddVoteHeader(...) only! @GameModeAPI");
                return;
            }

            List<RuleDef> allRuleDefs = (typeof(RuleCatalog)).GetFieldValue<List<RuleDef>>("allRuleDefs");
            List<RuleChoiceDef> allChoicesDefs = (typeof(RuleCatalog)).GetFieldValue<List<RuleChoiceDef>>("allChoicesDefs");
            Dictionary<string, RuleChoiceDef> ruleChoiceDefsByGlobalName = (typeof(RuleCatalog)).GetFieldValue<Dictionary<string, RuleChoiceDef>>("ruleChoiceDefsByGlobalName");

            selection.globalIndex = allRuleDefs.Count;
            if (selection.category.position == 0) selection.category.position = selection.globalIndex;
            for (int i = 0; i < selection.choices.Count; i++)
            {
                RuleChoiceDef choice = selection.choices[i];
                if (string.IsNullOrEmpty(choice.localName))
                {
                    Debug.LogError($"Failed to register vote choice: \"{choice.globalName}\", choice name is null or empty! Ensure each choice name is unique locally to each selection! @GameModeAPI");
                    selection.category.children.Remove(selection);
                    return;
                }
                if (RuleCatalog.FindChoiceDef(choice.globalName) != null)
                {
                    Debug.LogError($"Failed to register vote choice: \"{choice.globalName}\", entry already exists! Ensure each choice name is unique locally to each selection to prevent mod conflicts! @GameModeAPI");
                    selection.category.children.Remove(selection);
                    return;
                }
                choice.globalIndex = allChoicesDefs.Count;
                choice.localIndex = i;
                allChoicesDefs.Add(choice);
                ruleChoiceDefsByGlobalName.Add(choice.globalName, choice);
                RegisteredVoteChoices.Add(choice);
            }

            allRuleDefs.Add(selection);
            if (RuleCatalog.highestLocalChoiceCount < selection.choices.Count) (typeof(RuleCatalog)).SetFieldValue<int>("highestLocalChoiceCount", selection.choices.Count);
            (typeof(RuleCatalog)).GetFieldValue<Dictionary<string, RuleDef>>("ruleDefsByGlobalName").Add(selection.globalName, selection);
            RegisteredVoteSelections.Add(selection);
        }

        /// <summary>
        /// Creates a vote choice
        /// </summary>
        static VoteChoiceDef CreateChoice(ref RuleDef selection, string choiceName, object extraData = null, bool excludeByDefault = false)
        {
            RuleChoiceDef choice = new VoteChoiceDef();
            choice.ruleDef = selection;
            choice.localName = choiceName;
            choice.globalName = selection.globalName + "." + choiceName;
            choice.localIndex = selection.choices.Count;
            choice.extraData = extraData;
            choice.excludeByDefault = excludeByDefault;
            selection.GetFieldValue<List<RuleChoiceDef>>("choices").Add(choice);
            return (VoteChoiceDef)choice;
        }

        /// <summary>
        /// Hidden test
        /// </summary>
        static bool HiddenTestFalse()
        {
            return false;
        }
        #endregion
    }
}
