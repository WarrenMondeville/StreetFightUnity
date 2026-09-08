using System.Collections.Generic;
using UnityEngine;

namespace StreetFighter
{
    /// <summary>
    /// 复刻 interface.js 的 Audio：每个实例独占一个 AudioSource，
    /// 与原版 “一个 Audio 对象同时只能播一个音” 的行为保持一致。
    /// </summary>
    public class Sfx
    {
        private static readonly Dictionary<string, AudioClip> _cache = new Dictionary<string, AudioClip>();
        private static GameObject _root;

        private readonly AudioSource _src;

        public Sfx()
        {
            if (_root == null)
            {
                _root = new GameObject("Sfx");
                if (Application.isPlaying) Object.DontDestroyOnLoad(_root);
            }
            var go = new GameObject("audio");
            go.transform.SetParent(_root.transform);
            _src = go.AddComponent<AudioSource>();
            _src.playOnAwake = false;
        }

        public static AudioClip Load(string path)
        {
            if (string.IsNullOrEmpty(path)) return null;
            string name = path;
            int slash = name.LastIndexOf('/');
            if (slash >= 0) name = name.Substring(slash + 1);
            if (name.EndsWith(".mp3")) name = name.Substring(0, name.Length - 4);

            AudioClip clip;
            if (_cache.TryGetValue(name, out clip)) return clip;
            clip = Resources.Load<AudioClip>("Sound/" + name);
            if (clip == null) Debug.LogWarning("[SF] 找不到音频: " + path);
            _cache[name] = clip;
            return clip;
        }

        public void Play(string path)
        {
            var clip = Load(path);
            if (clip == null) return;
            _src.PlayOneShot(clip);
        }

        public void Loop(string path)
        {
            var clip = Load(path);
            if (clip == null) return;
            _src.clip = clip;
            _src.loop = true;
            _src.Play();
        }

        public void Pause() => _src.Pause();
    }
}
