using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using System.IO;
using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Networking;
using BepInEx;
using R2API;
using R2API.Utils;
using RoR2;
using RoR2.CharacterAI;
using RoR2.Networking;
using EntityStates;

namespace RoR2GameModeAPI.Utils
{
    /// <summary>
    /// Useful utilities methods for handling the loading of embedded files within a assembly
    /// </summary>
    public static class EmbeddedUtils
    {
        /// <summary>
        /// Load an embedded resource assembly (.dll) to be used by the application
        /// </summary>
        /// <param name="name">Manifest resource name
        /// <para>Name can be obtained from Assembly.GetExecutingAssembly().GetManifestResourceNames()</para></param>
        public static void LoadAssembly(string name)
        {
            using (Stream stream = Assembly.GetCallingAssembly().GetManifestResourceStream(name))
            {
                byte[] assemblyData = new byte[stream.Length];
                stream.Read(assemblyData, 0, assemblyData.Length);
                Assembly.Load(assemblyData);
            }
        }

        /// <summary>
        /// Load an embedded resource asset bundle to be used by the application
        /// </summary>
        /// <param name="name">Manifest resource name
        /// <para>Name can be obtained from Assembly.GetExecutingAssembly().GetManifestResourceNames()</para></param>
        /// <param name="prefix">Prefix to use for asset bundle look up, ensure each prefix is unique
        /// <para>ie: "@MyMod"</para></param>
        /// <param name="assetBundle">Asset bundle to cache to</param>
        /// <param name="provider">Provider to cache to</param>
        public static void LoadAssetBundle(string name, string prefix, ref AssetBundle assetBundle, ref AssetBundleResourcesProvider provider)
        {
            using (Stream stream = Assembly.GetCallingAssembly().GetManifestResourceStream(name))
            {
                assetBundle = AssetBundle.LoadFromStream(stream);
                provider = new AssetBundleResourcesProvider(prefix, assetBundle);
            }
            ResourcesAPI.AddProvider(provider);
        }

        /// <summary>
        /// Load a sound bank (.bnk) from resx to be used by the application, do not call on Awake() when mod is first loaded
        /// </summary>
        /// <param name="bank">Sound bank's raw bytes</param>
        /// <param name="bankName">sound bank's name</param>
        /// <param name="outBankID">sound bank's id output</param>
        /// <returns>Success state</returns>
        public static AKRESULT ManualLoadBank(byte[] bank, string bankName, out uint outBankID)
        {
            IntPtr memory = Marshal.AllocHGlobal(bank.Length);
            Marshal.Copy(bank, 0, memory, bank.Length);
            AKRESULT result = AkSoundEngine.LoadAndDecodeBankFromMemory(memory, (uint)bank.Length, false, bankName, false, out uint id);
            outBankID = id;
            return result;
        }
    }
}
