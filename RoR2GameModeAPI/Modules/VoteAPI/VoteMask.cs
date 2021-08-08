using System;
using UnityEngine;

namespace RoR2GameModeAPI
{
    /// <summary>
    /// Vote poll
    /// <para>Contains vote selection's picked vote choices</para>
    /// </summary>
    [Serializable]
    public struct VoteMask
    {
        /// <summary>
        /// Checks if a vote selection had picked the specified vote choice
        /// </summary>
        /// <param name="voteIndex">Vote choice's id to check</param>
        /// <returns>Returns true if vote choice was picked</returns>
        public bool HasVote(int voteIndex)
        {
            return voteIndex >= 0 && ((int)a & 1 << (int)voteIndex) != 0;
        }

        /// <summary>
        /// Adds vote choice to the vote poll
        /// </summary>
        /// <param name="voteIndex">Vote choice's id to add</param>
        public void AddVote(int voteIndex)
        {
            if (voteIndex < 0) return;
            a |= (ushort)(1 << (int)voteIndex);
        }

        /// <summary>
        /// Toggles vote choice
        /// </summary>
        /// <param name="voteIndex">Vote choice's id</param>
        public void ToggleVote(int voteIndex)
        {
            if (voteIndex < 0) return;
            a ^= (ushort)(1 << (int)voteIndex);
        }

        /// <summary>
        /// Removes vote choice from the vote poll
        /// </summary>
        /// <param name="voteIndex">Vote choice's id to remove</param>
        public void RemoveVote(int voteIndex)
        {
            if (voteIndex < 0) return;
            a &= (ushort)(~(ushort)(1 << (int)voteIndex));
        }

        /// <summary>
        /// Operator
        /// </summary>
        public static VoteMask operator &(VoteMask mask1, VoteMask mask2)
        {
            return new VoteMask
            {
                a = (ushort)(mask1.a & mask2.a)
            };
        }

        /// <summary>
        /// Masked value
        /// </summary>
        [SerializeField]
        public ushort a;

        /// <summary>
        /// Default value
        /// </summary>
        public static readonly VoteMask none;
    }
}
