using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using NAudio.Wave;

namespace CafeMarkul.Controller
{
    public class AudioManager : IDisposable
    {
        private WaveOutEvent _musicOutput;
        private CachedLoopSampleProvider _musicLoopProvider;
        private string _musicPath;
        private readonly Dictionary<string, LoopingSound> _loopingSounds = new Dictionary<string, LoopingSound>();
        private readonly Dictionary<string, float> _loopingSoundMultipliers = new Dictionary<string, float>();
        private readonly List<OneShotSound> _activeOneShotSounds = new List<OneShotSound>();
        private float _musicVolume = 0.35f;
        private float _soundVolume = 0.85f;

        public float MusicVolume
        {
            get { return _musicVolume; }
            set
            {
                _musicVolume = Clamp01(value);

                if (_musicLoopProvider != null)
                    _musicLoopProvider.Volume = _musicVolume;
            }
        }

        public float SoundVolume
        {
            get { return _soundVolume; }
            set
            {
                _soundVolume = Clamp01(value);
                UpdateLoopingSoundVolumes();
            }
        }

        public void PlayMusicLoop(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                return;

            if (!File.Exists(path))
                return;

            StopMusic();

            try
            {
                _musicPath = path;
                _musicLoopProvider = CachedLoopSampleProvider.FromFile(path, _musicVolume, true);
                if (_musicLoopProvider == null)
                    return;

                _musicOutput = new WaveOutEvent();
                _musicOutput.DesiredLatency = 80;
                _musicOutput.Init(_musicLoopProvider);
                _musicOutput.PlaybackStopped += MusicOutput_PlaybackStopped;
                _musicOutput.Play();
            }
            catch
            {
                StopMusic();
            }
        }

        private void MusicOutput_PlaybackStopped(object sender, StoppedEventArgs e)
        {
            // Фоновую музыку не останавливаем при достижении конца файла.
            // Если WaveOutEvent всё-таки перешёл в Stop, немедленно возвращаем поток в начало и запускаем снова.
            if (_musicOutput == null || _musicLoopProvider == null)
                return;

            try
            {
                _musicLoopProvider.ResetToStart();
                _musicOutput.Play();
            }
            catch
            {
                RestartMusicFromSavedPath();
            }
        }

        public void KeepLoopsAlive()
        {
            if (_musicOutput != null && _musicLoopProvider != null && _musicOutput.PlaybackState != PlaybackState.Playing)
            {
                try
                {
                    _musicOutput.Play();
                }
                catch
                {
                    RestartMusicFromSavedPath();
                }
            }

            foreach (LoopingSound sound in _loopingSounds.Values)
                sound.KeepAlive();
        }

        private void RestartMusicFromSavedPath()
        {
            string path = _musicPath;
            if (string.IsNullOrWhiteSpace(path))
                return;

            StopMusic();
            PlayMusicLoop(path);
        }

        public void PlaySound(string path)
        {
            PlaySoundFor(path, 0);
        }

        public void PlaySoundFor(string path, double maxSeconds)
        {
            if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
                return;

            WaveStream reader = null;
            WaveChannel32 volumeStream = null;
            WaveOutEvent output = null;
            OneShotSound activeSound = null;

            try
            {
                reader = CreateReader(path);
                if (reader == null)
                    return;

                volumeStream = new WaveChannel32(reader);
                volumeStream.Volume = _soundVolume;

                output = new WaveOutEvent();
                output.Init(volumeStream);

                activeSound = new OneShotSound(output, volumeStream, reader);
                _activeOneShotSounds.Add(activeSound);

                OneShotSound soundToDispose = activeSound;
                output.PlaybackStopped += (s, e) =>
                {
                    _activeOneShotSounds.Remove(soundToDispose);
                    soundToDispose.Dispose();
                };

                output.Play();

                if (maxSeconds > 0)
                    StopAfterDelay(soundToDispose, maxSeconds);
            }
            catch
            {
                if (activeSound != null)
                {
                    _activeOneShotSounds.Remove(activeSound);
                    activeSound.Dispose();
                }
                else
                {
                    if (output != null)
                        output.Dispose();
                    if (volumeStream != null)
                        volumeStream.Dispose();
                    if (reader != null)
                        reader.Dispose();
                }
            }
        }

        public void StartLoopingSound(string key, string path)
        {
            StartLoopingSound(key, path, 1.0f);
        }

        public void StartLoopingSound(string key, string path, float volumeMultiplier)
        {
            if (string.IsNullOrWhiteSpace(key) || string.IsNullOrWhiteSpace(path) || !File.Exists(path))
                return;

            _loopingSoundMultipliers[key] = volumeMultiplier;

            LoopingSound existingSound;
            if (_loopingSounds.TryGetValue(key, out existingSound))
            {
                existingSound.SetVolume(Clamp01(_soundVolume * volumeMultiplier));
                existingSound.KeepAlive();
                return;
            }

            try
            {
                float loopVolume = Clamp01(_soundVolume * volumeMultiplier);
                LoopingSound loopingSound = LoopingSound.Create(path, loopVolume);
                if (loopingSound == null)
                    return;

                _loopingSounds[key] = loopingSound;
                loopingSound.Play();
            }
            catch
            {
                StopLoopingSound(key);
            }
        }

        public void StopLoopingSound(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
                return;

            LoopingSound sound;
            if (!_loopingSounds.TryGetValue(key, out sound))
                return;

            sound.Dispose();
            _loopingSounds.Remove(key);
            _loopingSoundMultipliers.Remove(key);
        }

        public void SetLoopingSoundVolume(string key, float volumeMultiplier)
        {
            if (string.IsNullOrWhiteSpace(key))
                return;

            _loopingSoundMultipliers[key] = volumeMultiplier;

            LoopingSound sound;
            if (!_loopingSounds.TryGetValue(key, out sound))
                return;

            sound.SetVolume(Clamp01(_soundVolume * volumeMultiplier));
        }

        private void UpdateLoopingSoundVolumes()
        {
            foreach (KeyValuePair<string, LoopingSound> pair in _loopingSounds)
            {
                float multiplier = 1.0f;
                if (_loopingSoundMultipliers.ContainsKey(pair.Key))
                    multiplier = _loopingSoundMultipliers[pair.Key];

                pair.Value.SetVolume(Clamp01(_soundVolume * multiplier));
            }
        }

        public void StopMusic()
        {
            if (_musicOutput != null)
            {
                _musicOutput.PlaybackStopped -= MusicOutput_PlaybackStopped;
                _musicOutput.Stop();
                _musicOutput.Dispose();
                _musicOutput = null;
            }

            _musicLoopProvider = null;
            _musicPath = null;
        }

        public void Dispose()
        {
            StopMusic();

            foreach (LoopingSound sound in _loopingSounds.Values)
                sound.Dispose();

            _loopingSounds.Clear();
            _loopingSoundMultipliers.Clear();

            foreach (OneShotSound sound in _activeOneShotSounds.ToArray())
                sound.Dispose();

            _activeOneShotSounds.Clear();
        }

        private static float Clamp01(float value)
        {
            if (value < 0f)
                return 0f;

            if (value > 2f)
                return 2f;

            return value;
        }

        private static WaveStream CreateReader(string path)
        {
            string extension = Path.GetExtension(path).ToLowerInvariant();

            if (extension == ".mp3")
                return new Mp3FileReader(path);

            if (extension == ".wav")
                return new WaveFileReader(path);

            return null;
        }

        private async void StopAfterDelay(OneShotSound sound, double seconds)
        {
            try
            {
                await Task.Delay((int)(seconds * 1000.0));
                if (sound != null && sound.IsPlaying)
                    sound.Stop();
            }
            catch
            {
            }
        }

        private class OneShotSound : IDisposable
        {
            private readonly WaveOutEvent _output;
            private readonly WaveChannel32 _volumeStream;
            private readonly WaveStream _reader;
            private bool _disposed;

            public OneShotSound(WaveOutEvent output, WaveChannel32 volumeStream, WaveStream reader)
            {
                _output = output;
                _volumeStream = volumeStream;
                _reader = reader;
            }

            public bool IsPlaying
            {
                get { return _output != null && _output.PlaybackState == PlaybackState.Playing; }
            }

            public void Stop()
            {
                if (_output != null)
                    _output.Stop();
            }

            public void Dispose()
            {
                if (_disposed)
                    return;

                _disposed = true;

                if (_output != null)
                {
                    _output.Stop();
                    _output.Dispose();
                }

                if (_volumeStream != null)
                    _volumeStream.Dispose();

                if (_reader != null)
                    _reader.Dispose();
            }
        }

        private class LoopingSound : IDisposable
        {
            private readonly WaveOutEvent _output;
            private readonly CachedLoopSampleProvider _loopProvider;
            private readonly string _path;
            private readonly float _volume;
            private bool _disposed;

            private LoopingSound(string path, float volume, WaveOutEvent output, CachedLoopSampleProvider loopProvider)
            {
                _path = path;
                _volume = volume;
                _output = output;
                _loopProvider = loopProvider;
            }

            public static LoopingSound Create(string path, float volume)
            {
                CachedLoopSampleProvider loopProvider = CachedLoopSampleProvider.FromFile(path, volume, true);
                if (loopProvider == null)
                    return null;

                WaveOutEvent output = new WaveOutEvent();
                output.DesiredLatency = 80;
                output.Init(loopProvider);

                LoopingSound sound = new LoopingSound(path, volume, output, loopProvider);
                output.PlaybackStopped += sound.Output_PlaybackStopped;
                return sound;
            }

            public void Play()
            {
                if (_disposed || _output == null)
                    return;

                _output.Play();
            }

            private void Output_PlaybackStopped(object sender, StoppedEventArgs e)
            {
                if (_disposed || _output == null || _loopProvider == null)
                    return;

                try
                {
                    _loopProvider.ResetToStart();
                    _output.Play();
                }
                catch
                {
                }
            }

            public void KeepAlive()
            {
                if (_disposed || _output == null)
                    return;

                if (_output.PlaybackState == PlaybackState.Playing)
                    return;

                try
                {
                    _output.Play();
                }
                catch
                {
                }
            }

            public void SetVolume(float volume)
            {
                if (_disposed || _loopProvider == null)
                    return;

                _loopProvider.Volume = volume;
            }

            public void Dispose()
            {
                if (_disposed)
                    return;

                _disposed = true;

                if (_output != null)
                {
                    _output.PlaybackStopped -= Output_PlaybackStopped;
                    _output.Stop();
                    _output.Dispose();
                }
            }
        }
    }

    public class CachedLoopSampleProvider : ISampleProvider
    {
        private const float SilenceTrimThreshold = 0.001f;
        private readonly float[] _samples;
        private readonly int _loopStart;
        private readonly int _loopEnd;
        private int _position;

        public CachedLoopSampleProvider(float[] samples, WaveFormat waveFormat, float volume, int loopStart, int loopEnd)
        {
            _samples = samples;
            WaveFormat = waveFormat;
            Volume = volume;
            _loopStart = loopStart;
            _loopEnd = loopEnd;
            _position = _loopStart;
        }

        public WaveFormat WaveFormat { get; private set; }
        public float Volume { get; set; }

        public static CachedLoopSampleProvider FromFile(string path, float volume, bool trimEndSilence)
        {
            if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
                return null;

            using (AudioFileReader reader = new AudioFileReader(path))
            {
                List<float> samples = new List<float>();
                float[] buffer = new float[reader.WaveFormat.SampleRate * reader.WaveFormat.Channels];

                while (true)
                {
                    int read = reader.Read(buffer, 0, buffer.Length);
                    if (read <= 0)
                        break;

                    for (int i = 0; i < read; i++)
                        samples.Add(buffer[i]);
                }

                if (samples.Count == 0)
                    return null;

                int channels = Math.Max(1, reader.WaveFormat.Channels);
                int loopStart = 0;
                int loopEnd = samples.Count;

                if (trimEndSilence)
                    loopEnd = TrimEndSilence(samples, channels);

                if (loopEnd <= channels)
                    loopEnd = samples.Count;

                loopEnd -= loopEnd % channels;
                if (loopEnd <= 0)
                    loopEnd = samples.Count;

                return new CachedLoopSampleProvider(samples.ToArray(), reader.WaveFormat, volume, loopStart, loopEnd);
            }
        }

        private static int TrimEndSilence(List<float> samples, int channels)
        {
            int lastAudible = samples.Count - 1;

            while (lastAudible >= 0 && Math.Abs(samples[lastAudible]) < SilenceTrimThreshold)
                lastAudible--;

            if (lastAudible < 0)
                return samples.Count;

            int end = lastAudible + 1;
            end += channels - (end % channels);

            if (end > samples.Count)
                end = samples.Count;

            return end;
        }

        public void ResetToStart()
        {
            _position = _loopStart;
        }

        public int Read(float[] buffer, int offset, int count)
        {
            if (_samples == null || _samples.Length == 0 || _loopEnd <= _loopStart)
                return 0;

            int written = 0;

            while (written < count)
            {
                if (_position >= _loopEnd)
                    _position = _loopStart;

                int available = _loopEnd - _position;
                int needed = count - written;
                int toCopy = Math.Min(available, needed);

                for (int i = 0; i < toCopy; i++)
                    buffer[offset + written + i] = _samples[_position + i] * Volume;

                _position += toCopy;
                written += toCopy;
            }

            return written;
        }
    }
}
