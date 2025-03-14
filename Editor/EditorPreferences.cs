using UnityEngine;
using UnityEditor;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;

namespace JD.Preferences
{
    // From: https://discussions.unity.com/t/unity-editor-playmode-tint-change-by-script/112352/3
    [InitializeOnLoad]
    public static class EditorPreferences
    {
        private const string ToggleMenu = "Tools/Preferences/Toggle Editor Preferences";
        private const string EditorPref = "EditorPreferences";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatic()
        {
            _initialized = false;
            _prefsField = null;
            _prefColorField = null;
            _setColorPref = null;
            _isEnabled = EditorPrefs.GetBool(EditorPref, true);
        }

        private static bool _initialized = false;
        private static FieldInfo _prefsField = null;
        private static FieldInfo _prefColorField = null;
        private static MethodInfo _setColorPref = null;
        
        private static bool _isEnabled;

        private static void Refresh()
        {
            Debug.Log($"Applied Preferences");
            
            // Set default settings here
            PlaymodeTint = new Color(0.9491914f, 0.8632076f, 1f, 1f);
        }

        static EditorPreferences()
        {
            EditorApplication.delayCall += DelayCall;
        }

        private static void DelayCall()
        {
            EditorApplication.delayCall -= DelayCall;
            Initialize();
        }

        private static void Initialize()
        {
            if (_initialized) return;
            
            var settingsType = GetEditorType("PrefSettings");
            var prefColorType = GetEditorType("PrefColor");
            if (settingsType == null || prefColorType == null)
                throw new System.Exception("Something has changed in Unity and the SettingsHelper class is no longer supported");
            
            _prefsField = settingsType.GetField("m_Prefs", BindingFlags.Static | BindingFlags.NonPublic);
            _prefColorField = prefColorType.GetField("m_Color", BindingFlags.Instance | BindingFlags.NonPublic);
            _setColorPref = prefColorType.GetMethod("ToUniqueString", BindingFlags.Instance | BindingFlags.Public);
            
            if (_prefsField == null || _prefColorField == null || _setColorPref == null)
                throw new System.Exception("Something has changed in Unity and the SettingsHelper class is no longer supported");
            
            _isEnabled = EditorPrefs.GetBool(EditorPref, true);

            _initialized = true;
            if (_isEnabled) Refresh();
        }
        
        [MenuItem("Tools/Preferences/Refresh Editor Preferences")]
        private static void RefreshMenu()
        {
            Refresh();
        }

        [MenuItem(ToggleMenu)]
        private static void ToggleEnabled()
        {
            _isEnabled = !_isEnabled;
            EditorPrefs.SetBool(EditorPref, _isEnabled);

            if (_isEnabled) Refresh();
        }

        [MenuItem(ToggleMenu, true)]
        private static bool ToggleEnabledValidate()
        {
            Menu.SetChecked(ToggleMenu, _isEnabled);
            return true;
       }
        
        private static SortedList<string, object> GetList()
        {
            return (SortedList<string, object>)_prefsField.GetValue(null);
        }
        
        private static object GetPref(string aName)
        {
            var dict = GetList();
            if (dict.ContainsKey(aName)) return GetList()[aName];
            return null;
        }
        
        private static System.Type GetEditorType(string aName)
        {
            return typeof(Editor).Assembly.GetTypes().FirstOrDefault(a => a.Name == aName);
        }

        public static Color PlaymodeTint
        {
            get
            {
                if (!_initialized) return Color.black;
                
                var p = GetPref("Playmode tint");
                if (p == null) return Color.black;
                
                return (Color) _prefColorField.GetValue(p);
            }
            set
            {
                if (!_initialized) return;
                
                var p = GetPref("Playmode tint");
                if (p == null) return;
                
                _prefColorField.SetValue(p, value);
                var data = (string) _setColorPref.Invoke(p, null);
                EditorPrefs.SetString("Playmode tint", data);
            }
        }
    }
}
