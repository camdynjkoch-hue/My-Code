using System;
using System.Linq;
using System.Reflection;

using MelonLoader;

using UnityEngine;

namespace HeroStance
{
    /// <summary>
    /// Builds the BoneMenu page through reflection, matching BoneLib 3.x's
    /// Page.Root.CreatePage / CreateBool / CreateFloat. If BoneMenu isn't there, settings still work from
    /// UserData/MelonPreferences.cfg.
    /// </summary>
    public sealed class BoneMenuBridge
    {
        private readonly object _page;

        private BoneMenuBridge(object page)
        {
            _page = page;
        }

        public static BoneMenuBridge CreatePage(Assembly boneLib, string name, Color color)
        {
            try
            {
                var pageType = boneLib?.GetType("BoneLib.BoneMenu.Page");
                if (pageType == null)
                    throw new MissingMemberException("BoneLib.BoneMenu.Page");

                const BindingFlags flags = BindingFlags.Static | BindingFlags.Public;
                object root = pageType.GetProperty("Root", flags)?.GetValue(null) ?? pageType.GetField("Root", flags)?.GetValue(null);
                if (root == null)
                    throw new MissingMemberException("BoneLib.BoneMenu.Page.Root");

                object page = Call(root, "CreatePage", name, color, 0, true);
                return page != null ? new BoneMenuBridge(page) : null;
            }
            catch (Exception e)
            {
                MelonLogger.Warning($"Hero Stance: BoneMenu isn't available ({e.Message}). Edit UserData/MelonPreferences.cfg to change settings.");
                return null;
            }
        }

        public void CreateBool(string name, Color color, bool value, Action<bool> callback)
        {
            TryCall("CreateBool", name, color, value, callback);
        }

        public void CreateFloat(string name, Color color, float value, float increment, float min, float max, Action<float> callback)
        {
            TryCall("CreateFloat", name, color, value, increment, min, max, callback);
        }

        private void TryCall(string method, params object[] args)
        {
            try
            {
                Call(_page, method, args);
            }
            catch (Exception e)
            {
                MelonLogger.Warning($"Hero Stance: couldn't add BoneMenu option '{args[0]}' ({e.Message}).");
            }
        }

        /// <summary>
        /// Calls the overload whose leading parameters accept <paramref name="args"/>; any extra trailing
        /// parameters must be optional and get their default values.
        /// </summary>
        private static object Call(object target, string name, params object[] args)
        {
            var method = target.GetType()
                .GetMethods(BindingFlags.Instance | BindingFlags.Public)
                .Where(m => m.Name == name && Accepts(m.GetParameters(), args))
                .OrderBy(m => m.GetParameters().Length)
                .FirstOrDefault();

            if (method == null)
                throw new MissingMethodException(target.GetType().FullName, name);

            var parameters = method.GetParameters();
            var fullArgs = new object[parameters.Length];
            for (int i = 0; i < parameters.Length; i++)
                fullArgs[i] = i < args.Length ? args[i] : parameters[i].DefaultValue;

            return method.Invoke(target, fullArgs);
        }

        private static bool Accepts(ParameterInfo[] parameters, object[] args)
        {
            if (parameters.Length < args.Length)
                return false;

            for (int i = 0; i < parameters.Length; i++)
            {
                if (i >= args.Length)
                {
                    if (!parameters[i].IsOptional)
                        return false;

                    continue;
                }

                if (args[i] != null && !parameters[i].ParameterType.IsInstanceOfType(args[i]))
                    return false;
            }

            return true;
        }
    }
}
