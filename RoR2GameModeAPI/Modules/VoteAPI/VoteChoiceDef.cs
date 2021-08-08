using RoR2;

namespace RoR2GameModeAPI
{
    /// <summary>
    /// RuleChoiceDef expanded for VoteAPI
    /// </summary>
    class VoteChoiceDef : RuleChoiceDef
    {
        /// <summary>
        /// Vote choice id, ensure each id is unique locally to it's vote poll (1-32)
        /// <para>Used for VoteAPI.VoteResults.Vote[...].HasVote(ChoiceIndex) lookup</para>
        /// </summary>
        public int VoteIndex = 0;
        /// <summary>
        /// Name of the vote poll this belongs to
        /// <para>Used for VoteAPI.VoteResults[VotePollKey] look up</para>
        /// </summary>
        public string VotePollKey = "";
    }
}
