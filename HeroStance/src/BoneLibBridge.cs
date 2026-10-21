using System;
using System.Linq;
using System.Reflection;

using MelonLoader;

using UnityEngine;

namespace HeroStance
{
    /// <summary>
    /// Reaches BoneLib and the game's rig types by name at runtime, so this DLL doesn't have to be compiled
    /// against BONELAB's own assemblies. Everything here degrades gracefully: if a name ever changes, the
    /// mod logs one warning instead of failing to load.
    /// </summary>
    public static class BoneLibBridge
    {
        private static Assembly _boneLib;
        private static Func<object> _getRigManager;
        private static Func<object> _getLeftController;
        private static Func<object> _getRightController;

        private static Type _rigManagerType;
        private static Func<object, object> _getAvatar;

        private static Type _controllerType;
        private static MethodInfo _getThumbStickAxis;

        private static bool _warnedAvatar;
        private static bool _warnedThumbstick;

        public static Assembly BoneLib => _boneLib;

        public static bool Initialize()
        {
            _boneLib = AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(a => a.GetName().Name == "BoneLib");
            if (_boneLib == null)
            {
                MelonLogger.Error("Hero Stance: BoneLib isn't loaded. Install BoneLib 3.x into BONELAB/Mods.");
                return false;
            }

            var player = _boneLib.GetType("BoneLib.Player");
            if (player == null)
            {
                MelonLogger.Error("Hero Stance: couldn't find BoneLib.Player. Your BoneLib version may be too old.");
                return false;
            }

            _getRigManager = StaticGetter(player, "RigManager");
            _getLeftController = StaticGetter(player, "LeftController");
            _getRightController = StaticGetter(player, "RightController");

            if (_getRigManager == null)
            {
                MelonLogger.Error("Hero Stance: couldn't find BoneLib.Player.RigManager. Your BoneLib version may be too old.");
                return false;
            }

            return true;
        }

        /// <summary>The local player's avatar, or null while no rig is spawned.</summary>
        public static Component GetPlayerAvatar()
        {
            var rigManager = Alive(_getRigManager());
            if (rigManager == null)
                return null;

            var type = rigManager.GetType();
            if (type != _rigManagerType)
            {
                _rigManagerType = type;
                _getAvatar = InstanceGetter(type, "avatar") ?? InstanceGetter(type, "_avatar");
            }

            if (_getAvatar == null)
            {
                if (!_warnedAvatar)
                    MelonLogger.Warning($"Hero Stance: couldn't find the avatar on {type.FullName}.");

                _warnedAvatar = true;
                return null;
            }

            return Alive(_getAvatar(rigManager)) as Component;
        }

        public static bool IsAnyThumbstickHeld(float deadzone)
        {
            return IsThumbstickHeld(_getLeftController, deadzone) || IsThumbstickHeld(_getRightController, deadzone);
        }

        private static bool IsThumbstickHeld(Func<object> getController, float deadzone)
        {
            if (getController == null || _warnedThumbstick)
                return false;

            try
            {
                var controller = Alive(getController());
                if (controller == null)
                    return false;

                var type = controller.GetType();
                if (type != _controllerType)
                {
                    _controllerType = type;
                    _getThumbStickAxis = type.GetMethod("GetThumbStickAxis", BindingFlags.Instance | BindingFlags.Public, null, Type.EmptyTypes, null);
                }

                if (_getThumbStickAxis != null && _getThumbStickAxis.Invoke(controller, null) is Vector2 axis)
                    return axis.x * axis.x + axis.y * axis.y > deadzone * deadzone;
            }
            catch (Exception e)
            {
                MelonLogger.Warning($"Hero Stance: {e.Message}");
            }

            // Idle detection still works from body speed alone
            MelonLogger.Warning("Hero Stance: couldn't read the thumbsticks, so only body movement is used to detect idle.");
            _warnedThumbstick = true;
            return false;
        }

        /// <summary>Returns null for missing objects and for Unity objects that have been destroyed.</summary>
        private static object Alive(object value)
        {
            if (value is UnityEngine.Object unityObject && unityObject == null)
                return null;

            return value;
        }

        private static Func<object> StaticGetter(Type type, string name)
        {
            const BindingFlags flags = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;

            var property = type.GetProperty(name, flags);
            if (property != null && property.GetMethod != null)
                return () => property.GetValue(null);

            var field = type.GetField(name, flags);
            if (field != null)
                return () => field.GetValue(null);

            return null;
        }

        private static Func<object, object> InstanceGetter(Type type, string name)
        {
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

            var property = type.GetProperty(name, flags);
            if (property != null && property.GetMethod != null)
                return target => property.GetValue(target);

            var field = type.GetField(name, flags);
            if (field != null)
                return target => field.GetValue(target);

            return null;
        }
    }
}
