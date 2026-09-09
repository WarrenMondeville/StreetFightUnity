using System.Collections.Generic;
using UnityEngine;

namespace StreetFighter.Core
{
    /// <summary>
    /// 音效播放器。
    /// 每个实例独占一个 <see cref="AudioSource"/>，与「一个播放器同时只能播一个音」的行为保持一致。
    /// </summary>
    public sealed class AudioPlayer
    {
        private const string RootObjectName = "Sfx";
        private const string SoundFolder = "Sound/";

        private static readonly Dictionary<string, AudioClip> ClipCache = new Dictionary<string, AudioClip>();

        private static GameObject _root;

        private readonly AudioSource _source;

        public AudioPlayer()
        {
            if (_root == null)
            {
                _root = new GameObject(RootObjectName);
                if (Application.isPlaying)
                {
                    Object.DontDestroyOnLoad(_root);
                }
            }

            var go = new GameObject("audio");
            go.transform.SetParent(_root.transform);
            _source = go.AddComponent<AudioSource>();
            _source.playOnAwake = false;
        }

        /// <summary>按 Resources 相对路径加载音频（带缓存）。</summary>
        public static AudioClip Load(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                return null;
            }

            string name = path;
            int slash = name.LastIndexOf('/');
            if (slash >= 0)
            {
                name = name.Substring(slash + 1);
            }

            if (name.EndsWith(".mp3"))
            {
                name = name.Substring(0, name.Length - 4);
            }

            AudioClip clip;
            if (ClipCache.TryGetValue(name, out clip))
            {
                return clip;
            }

            clip = Resources.Load<AudioClip>(SoundFolder + name);
            if (clip == null)
            {
                Debug.LogWarning($"[SF] 找不到音频: {path}");
            }

            ClipCache[name] = clip;
            return clip;
        }

        /// <summary>播放一次音效。</summary>
        public void Play(string path)
        {
            var clip = Load(path);
            if (clip == null)
            {
                return;
            }

            _source.PlayOneShot(clip);
        }

        /// <summary>循环播放（用于 BGM）。</summary>
        public void PlayLoop(string path)
        {
            var clip = Load(path);
            if (clip == null)
            {
                return;
            }

            _source.clip = clip;
            _source.loop = true;
            _source.Play();
        }

        public void Pause() => _source.Pause();

        public void Stop() => _source.Stop();
    }
}
