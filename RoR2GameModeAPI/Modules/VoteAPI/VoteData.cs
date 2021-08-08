using UnityEngine;

namespace RoR2GameModeAPI
{
    /// <summary>
    /// VoteChoiceDef's creation settings
    /// </summary>
    public struct ChoiceMenu
    {
        /// <summary>
        /// Name and title, ensure name is unique locally to the selection it belongs to
        /// </summary>
        public string TooltipName;
        /// <summary>
        /// Title background color
        /// </summary>
        public Color TooltipNameColor;
        /// <summary>
        /// Discription
        /// </summary>
        public string TooltipBody;
        /// <summary>
        /// Discription background color
        /// </summary>
        public Color TooltipBodyColor;
        /// <summary>
        /// Sprite icon path, can be left null if no assets are available
        /// </summary>
        public string IconPath;
        /// <summary>
        /// Vote choice id, ensure each id is unique locally to it's vote poll (1-32)
        /// <para>Used for VoteAPI.VoteResults.Vote[...].HasVote(ChoiceIndex) lookup</para>
        /// </summary>
        public int ChoiceIndex;
        /// <summary>
        /// Name of the vote poll this belongs to
        /// <para>Used for VoteAPI.VoteResults[VotePollKey] look up</para>
        /// </summary>
        public string VotePollKey;
        /// <summary>
        /// Vote poll result's extra data value if vote choice was selected from the vote selection
        /// <para>Used for VoteAPI.VoteResults[...].VoteExtraDatas[...] value</para>
        /// </summary>
        public object ExtraData;

        /// <summary>
        /// Constructor, assigns all the choice menu's fields
        /// </summary>
        /// <param name="tooltipName">Name and title, ensure name is unique locally to the selection it belongs to</param>
        /// <param name="tooltipNameColor">Title background color</param>
        /// <param name="tooltipBody">Discription</param>
        /// <param name="tooltipBodyColor">Discription background color</param>
        /// <param name="iconPath">Sprite icon path, can be left null if no assets are available</param>
        /// <param name="choiceIndex">Vote choice id, ensure each id is unique locally to it's vote poll (1-32)
        /// <para>Used for VoteAPI.VoteResults.Vote[...].HasVote(ChoiceIndex) lookup</para></param>
        /// <param name="votePollKey">Name of the vote poll this belongs to
        /// <para>Used for VoteAPI.VoteResults[VotePollKey] look up</para></param>
        /// <param name="extraData">Vote poll result's extra data value if vote choice was selected from the vote selection
        /// <para>Used for VoteAPI.VoteResults[...].VoteExtraDatas[...] value</para></param>
        public ChoiceMenu(string tooltipName, Color tooltipNameColor, string tooltipBody, Color tooltipBodyColor, string iconPath, int choiceIndex, string votePollKey, object extraData = null)
        {
            TooltipName = tooltipName;
            TooltipNameColor = tooltipNameColor;
            TooltipBody = tooltipBody;
            TooltipBodyColor = tooltipBodyColor;
            IconPath = iconPath;
            ChoiceIndex = choiceIndex;
            VotePollKey = votePollKey;
            ExtraData = extraData;
        }
    }
}
