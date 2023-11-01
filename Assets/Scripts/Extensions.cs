using UnityEngine;
using System.Collections.Generic;
#if !ENABLE_LEGACY_INPUT_MANAGER
using UnityEngine.InputSystem;
#endif
using System;
using Random = UnityEngine.Random;
using Object = UnityEngine.Object;
using UnityEngine.Events;
using System.Reflection;

public static class Extensions
{
    public static Sprite Icon(this Finger.ID finger) => SpriteManager.Singleton.m_fingersIcons[(int)(finger-1)];

    public static void ClearChild(this Transform transform, bool Immediately = false)
    {
        foreach (Transform child in transform)
        {
            if (Immediately) Object.DestroyImmediate(child.gameObject);
            else Object.Destroy(child.gameObject);
        }
    }
    public static float Remap(this float f, float IMin, float IMax, float OMin, float Omax) => OMin + (f - IMin) * (Omax - OMin) / (IMax - IMin);

    public static T AddComponent<T>(this GameObject game, T duplicate) where T : Component
    {
        T target = game.AddComponent<T>();
        foreach (PropertyInfo x in typeof(T).GetProperties()) if (x.CanWrite) x.SetValue(target, x.GetValue(duplicate));
        return target;
    }
    public static T GetComponent<T>(this GameObject Go, out T Component)
    {
        T Return = Go.GetComponent<T>();
        Component = Return;
        return Return;
    }
    public static T GetComponent<T>(this Component Cpnt, out T Component)
    {
        T Return = Cpnt.GetComponent<T>();
        Component = Return;
        return Return;
    }
    public static T GetComponentInChildren<T>(this GameObject GO, out T Component)
    {
        T Return = GO.GetComponentInChildren<T>();
        Component = Return;
        return Return;
    }
    public static bool TryGetComponentInChildren<T>(this GameObject Go, out T Component)
    {
        Component = Go.GetComponentInChildren<T>();
        return Component != null;
    }
    public static bool TryGetComponentInChildren<T>(this Component Go, out T Component)
    {
        Component = Go.GetComponentInChildren<T>();
        return Component != null;
    }
    public static bool TryGetComponentInChildren<T>(this MonoBehaviour Go, out T Component)
    {
        Component = Go.GetComponentInChildren<T>();
        return Component != null;
    }
    public static Vector3 Direction(this Vector3 vec3, Vector3 from, Vector3 to) => (to - from).normalized;
    public static Vector3 Direction01(this Vector3 vec3, Vector3 from, Vector3 to)
    {
        Vector3 dir = (to - from);
        return (dir.magnitude > 1f) ? dir.normalized : dir;
    }

    public static List<T> AddRange<T>(this List<T> list, IEnumerable<T> collection)
    {
        list.AddRange(collection);
        return list;
    }
    public static T Find<T>(this List<T> list, Predicate<T> predicate) => list.Exists(predicate) ? list.Find(predicate) : default;
    public static bool Find<T>(this List<T> list, Predicate<T> predicate, out T item) { item = list.Find(predicate); return list.Exists(predicate); }
    public static void Remove<T>(this List<T> list, Predicate<T> predicate) => list.Remove(list.Find(predicate));
    public static void Remove<T>(this List<T> list, Predicate<T> predicate, out T removed) { removed = list.Find(predicate); list.Remove(removed); }
    public static T Pick<T>(this List<T> list, Predicate<T> predicate) { T removed = list.Find(predicate); list.Remove(removed); return removed; }
    public static T AddAndReturn<T>(this List<T> list, T newItem)
    {
        list.Add(newItem);
        return newItem;
    }


    public static string HashCode() => "#" + Random.Range(0, 9).ToString() + Random.Range(0, 9).ToString() + Random.Range(0, 9).ToString();

    public static T GetRandom<T>(this T[] array) => array.Length > 0 ? array[Random.Range(0, array.Length)] : default;
    public static T GetRandom<T>(this List<T> list) => list.Count > 0 ? list[Random.Range(0, list.Count)] : default;

    public static string GetCode(this string name) => name.Substring(name.LastIndexOf('_') + 1, name.Length - name.LastIndexOf('_') - 1);

    public static void Play(this AudioSource audioSource, AudioClip audioClip, bool forceReplay = false)
    {
        if (audioSource != null)
        {
            if (forceReplay)
            {
                audioSource.clip = audioClip;
                audioSource.Play();
            }
            else if (audioSource.clip != audioSource || !audioSource.isPlaying)
            {
                audioSource.clip = audioClip;
                audioSource.Play();
            }
        }
    }
    public static void RemoveIf<T>(this List<T> list, T item) { if (list.Contains(item)) list.Remove(item); }
    public static void AddIfNo<T>(this List<T> list, T item) { if (!list.Contains(item)) list.Add(item); }
    public static Vector3 ToVector3(this float v1) => new Vector3(v1, v1, v1);
    public static Vector2 ToVector2(this float v1) => new Vector2(v1, v1);
    public static float Negative(this float number) => -Mathf.Abs(number);
    public static float Positive(this float number) => Mathf.Abs(number);
    public static void PlayIfNot(this AudioSource audioSource) { if (!audioSource.isPlaying) audioSource.Play(); }
    public static void StopIfNot(this AudioSource audioSource) { if (audioSource.isPlaying) audioSource.Stop(); }
    public static void DestroyIfExist(this Object obj, Object toDestroy, bool immediate = false)
    {
        if (toDestroy)
        {
            if (immediate) Object.DestroyImmediate(toDestroy);
            else Object.Destroy(toDestroy);
        }
    }
    public static T DontDestroyOnLoad<T>(this T obj) where T : Object
    {
        Object.DontDestroyOnLoad(obj);
        return obj;
    }

    public static GameObject InstantiateAndName(this Object none, GameObject prefab, string name, bool protect = false)
    {
        GameObject Obj = Object.Instantiate(prefab);
        Obj.name = name;
        if (protect) Object.DontDestroyOnLoad(Obj);
        GameObject.FindObjectOfType<Mesh>();
        return Obj;
    }
#if !ENABLE_LEGACY_INPUT_MANAGER

    public static T Get<T>(this InputValue inputValue, out T value) where T : struct
    {
        value = inputValue.Get<T>();
        return value;
    }
#endif
}
//ToDo : Custom Inspector Drawer , bool clamped, synchronised
[Serializable]
public class Carrousel<T>
{
    public T[] Array = new T[0];
    public int index { get; private set; }
    //public bool Clamped;
    public T First => Array[0];
    public T Last => Array[Array.Length - 1];
    public T Current => Array[index];
    public T Next => GetValue(1);
    public T Previous => GetValue(-1);
    //public T GetValue(int offset = 0) => Array[((Clamped? Mathf.Clamp(index + offset, 0,Array.Length): index + offset ) % Array.Length < 0) ? (((Clamped ? Mathf.Clamp(index + offset, 0, Array.Length) : index + offset) % Array.Length) + Array.Length) : ((Clamped ? Mathf.Clamp(index + offset, 0, Array.Length) : index + offset) % Array.Length)];
    public T GetValue(int offset = 0) => Array[((index + offset) % Array.Length < 0) ? (((index + offset) % Array.Length) + Array.Length) : ((index + offset) % Array.Length)];
    public T Random => Array[UnityEngine.Random.Range(0, Array.Length - 1)];

    public Carrousel(T[] array, bool clamped = false) { Array = array; index = 0; Updated?.Invoke(); }// Clamped = clamped;}

    public void SetValues(T[] newValues) { Array = newValues; index = 0; Updated?.Invoke(); }

    public void Increment() { index = (index + 1) % Array.Length; Updated?.Invoke(); }
    public void ClampedIncrement() { index = (index + 1) > Array.Length - 1 ? index : index + 1; Updated?.Invoke(); }
    public void Decrement() { index = (index - 1) % Array.Length < 0 ? ((index - 1) % Array.Length) + Array.Length : (index - 1) % Array.Length; Updated?.Invoke(); }
    public void ClampdDecrement() { index = (index - 1) < 0 ? 0 : index - 1; Updated?.Invoke(); }

    public UnityEvent Updated = default;

}
