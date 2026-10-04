// Minimal stand-ins for the Unity types the package touches, so the C# layer can be
// compiled and unit-tested outside the Unity Editor. Not part of the package.
using System;
using System.Collections;
using System.Collections.Generic;

namespace UnityEngine
{
    public class Object
    {
        public string name;
        public static void DontDestroyOnLoad(Object target) { }
        public static void Destroy(Object obj) { }
    }

    public class Component : Object
    {
        public GameObject gameObject { get; internal set; }
        public T GetComponent<T>() where T : Component
        {
            return gameObject != null ? gameObject.GetComponent<T>() : null;
        }
    }

    public class Coroutine { }

    public class MonoBehaviour : Component
    {
        public Coroutine StartCoroutine(IEnumerator routine) { return new Coroutine(); }
    }

    public sealed class GameObject : Object
    {
        private readonly List<Component> components = new List<Component>();
        public static readonly List<GameObject> All = new List<GameObject>();

        public GameObject(string name)
        {
            this.name = name;
            All.Add(this);
        }

        public static GameObject Find(string name)
        {
            return All.Find(g => g.name == name);
        }

        public T AddComponent<T>() where T : Component, new()
        {
            var component = new T { gameObject = this };
            components.Add(component);
            return component;
        }

        public T GetComponent<T>() where T : Component
        {
            return components.Find(c => c is T) as T;
        }
    }

    public class WaitForSeconds
    {
        public WaitForSeconds(float seconds) { }
    }

    [AttributeUsage(AttributeTargets.Class)]
    public sealed class AddComponentMenuAttribute : Attribute
    {
        public AddComponentMenuAttribute(string menuName) { }
    }

    public static class Debug
    {
        public static readonly List<string> Logs = new List<string>();
        public static readonly List<string> Warnings = new List<string>();
        public static readonly List<string> Errors = new List<string>();

        public static void Log(object message) { Logs.Add(Convert.ToString(message)); }
        public static void LogWarning(object message) { Warnings.Add(Convert.ToString(message)); }
        public static void LogError(object message) { Errors.Add(Convert.ToString(message)); }

        public static void Clear()
        {
            Logs.Clear();
            Warnings.Clear();
            Errors.Clear();
        }
    }

    public class AndroidJavaObject : IDisposable
    {
        public void Dispose() { }
    }

    public class AndroidJavaClass : AndroidJavaObject
    {
        public AndroidJavaClass(string className) { }
        public void CallStatic(string methodName, params object[] args) { }
        public T GetStatic<T>(string fieldName) { return default(T); }
    }
}

namespace UnityEditor
{
    public enum BuildTarget { iOS, Android, StandaloneWindows }
}

namespace UnityEditor.Callbacks
{
    [AttributeUsage(AttributeTargets.Method)]
    public sealed class PostProcessBuildAttribute : Attribute
    {
        public PostProcessBuildAttribute(int callbackOrder) { }
    }
}

namespace UnityEditor.iOS.Xcode
{
    public class PBXProject
    {
        public static string GetPBXProjectPath(string buildPath) { return buildPath + "/Unity-iPhone.xcodeproj/project.pbxproj"; }
        public void ReadFromFile(string path) { }
        public void WriteToFile(string path) { }
        public string GetUnityMainTargetGuid() { return "main"; }
        public string GetUnityFrameworkTargetGuid() { return "framework"; }
        public void SetBuildProperty(string targetGuid, string name, string value) { }
        public void AddBuildProperty(string targetGuid, string name, string value) { }
    }
}
