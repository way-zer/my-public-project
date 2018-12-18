using System;
using System.IO;
using System.Linq;
using System.Speech.AudioFormat;
using System.Speech.Synthesis;
using System.Text;
using NAudio.Lame;
using NAudio.Wave;
using Newtonsoft.Json;

namespace WordVoiceCreater
{
    internal class InputClass
    {
        internal class Word
        {
            public string English { get; set; }
            public string Chinese { get; set; }
            // ReSharper disable once MemberHidesStaticFromOuterClass
            public static Word Default => new Word {English = "Engilsh", Chinese = "中文"};
        }
        public string SpeakVoice { get; set; }
        public int SampleRate { get; set; }
        public int Speed { get; set; }
        public int Volume { get; set; }
        public float WaitTime { get; set; }
        
        public int[] Parts { get; set; }
        public Word[] Words { get; set; }
        
        public static InputClass Default=>new InputClass
        {
            Parts = new[]{1}, Words = new[]{Word.Default}, SpeakVoice = SelectVoices(), 
            WaitTime = 0.7F, Speed = -3, Volume = 100, SampleRate = 8000
        };
        
        private static string SelectVoices()
        {
            var sp = new SpeechSynthesizer();
            var i = 1;
            foreach (var voice in sp.GetInstalledVoices())
            {
                Console.WriteLine($"[{i++}]{voice.VoiceInfo.Name}");
            }
            
            Console.Write("请选择一个: ");
            var selected = sp.GetInstalledVoices()[Console.ReadKey().KeyChar - '1'].VoiceInfo.Name;
            Console.WriteLine($" 已选择{selected}");
            sp.Dispose();
            return selected;
        }
    }

    internal class MyAudioClass
    {
        private readonly MemoryStream _rawStream = new MemoryStream();
//        private readonly string _filename;
        private readonly LameMP3FileWriter _mp3Writer;
        private readonly WaveFileWriter _writer;
        private readonly SpeechSynthesizer _speaker;
        private readonly InputClass _config;
        private readonly LrcClass _lrc;
        private readonly StreamWriter _txtWriter;
        public MyAudioClass(string filename,InputClass config,ID3TagData tagData)
        {
//            _filename = filename;
            _config = config;
            _txtWriter = File.CreateText($"{filename}.txt");
            var format = new WaveFormat(config.SampleRate, 16, 2);
            _mp3Writer = new LameMP3FileWriter($"{filename}.mp3",format,128,tagData);
//            var outfile = $"{filename}.wav";
            var outfile = _rawStream;
            _writer = new WaveFileWriter(outfile, format);
            _lrc = new LrcClass($"{filename}.lrc");
            _speaker = new SpeechSynthesizer();
            _speaker.SelectVoice(_config.SpeakVoice);
            _speaker.Rate = _config.Speed;
            _speaker.Volume = _config.Volume;
        }

        public void AddSpeak(string text)
        {
            using (var stream = new MemoryStream())
            {
                _speaker.SetOutputToAudioStream(stream,new SpeechAudioFormatInfo(_config.SampleRate,AudioBitsPerSample.Sixteen, AudioChannel.Stereo));
                _speaker.Speak(text);
                _speaker.SetOutputToNull();
            
                _writer.Write(stream.GetBuffer(),0,stream.GetBuffer().Length);
                AddEmpty(_config.WaitTime);
                _writer.Write(stream.GetBuffer(),0,stream.GetBuffer().Length);
                AddEmpty(_config.WaitTime);
                stream.Dispose();
            }
        }
        
        public void Run(int start, int i1)
        {
            Console.WriteLine($"Part {i1+1} 生成音频数据-开始!!");
            for (var i = 1; i <= _config.Parts[i1]; i++)
            {
                var word = _config.Words[start++];
                _lrc.Write(Writed,$"{i1+1}:{i:D2}-{word.English} -> {word.Chinese}");
                _txtWriter.WriteLine($"{i1+1}:{i:D2}-{word.English} -> {word.Chinese}");
                AddSpeak(word.English);
            }
            Console.WriteLine($"Part {i1+1} 生成音频数据-结束!!");
            Console.WriteLine($"Part {i1+1} 转换为MP3格式-开始!!");
            var reader = new RawSourceWaveStream(_rawStream, _writer.WaveFormat) {Position = 0};
            reader.CopyTo(_mp3Writer);
            reader.Dispose();
            Console.WriteLine($"Part {i1+1} 转换为MP3格式-结束!!");
            Console.WriteLine($"Part {i1+1} 生成完成!!");
        }

        public void AddEmpty(float time)
        {
            for (var i = 0; i < _config.SampleRate*time; i++)
                _writer.WriteSample(0);
        }

        public double Writed => (double)_writer.Length / _writer.WaveFormat.AverageBytesPerSecond;

        public void Close()
        {
            _speaker.Dispose();
            _writer.Dispose();
            _mp3Writer.Dispose();
            _rawStream.Dispose();
            _txtWriter.Dispose();
            _lrc.Close();
        }
    }
    
    internal class LrcClass
    {
        private readonly FileStream _stream;
        private readonly StreamWriter _writer;
        
        internal LrcClass(string filename)
        {
            _stream=File.OpenWrite(filename);
            _writer=new StreamWriter(_stream,Encoding.UTF8);
        }

        public void Write(double start, string content)
        {
            _writer.WriteLine($"{GetTime(start)} {content}");
        }

        public void Close()
        {
            _writer.Close();
            _stream.Close();
        }

        private static string GetTime(double time)
        {
            var mm = (int)time/60;
            time %= 60;
            var ss=(int)time;
            time %= 1;
            var xx=(int)(time*100);
            return $"[{mm:D2}:{ss:D2}.{xx:D2}]";
        }
    }

    internal class Program
    {
        public static void CheckAddBinPath()
        {
            // find path to 'bin' folder
            var binPath = Path.Combine(new string[] { AppDomain.CurrentDomain.BaseDirectory });
            // get current search path from environment
            var path = Environment.GetEnvironmentVariable("PATH") ?? "";

            // add 'bin' folder to search path if not already present
            if (path.Split(Path.PathSeparator).Contains(binPath, StringComparer.CurrentCultureIgnoreCase)) return;
            path = string.Join(Path.PathSeparator.ToString(), new string[] { path, binPath });
            Environment.SetEnvironmentVariable("PATH", path);
        }
        
        public static void Main(string[] args)
        {
            CheckAddBinPath();
            Console.Write("请输入文件名: ");
            var name = Console.ReadLine();
            if(name==null)return;

            if (!File.Exists($"{name}/in.json"))
            {
                Console.WriteLine("文件不存在,新建配置");
                Directory.CreateDirectory(name);
                var config = InputClass.Default;
                var inWriter=File.CreateText($"{name}/in.json");
                inWriter.Write(JsonConvert.SerializeObject(config));
                inWriter.Close();
                Console.WriteLine("配置新建完成!");
                return;
            }
            var streamReader = File.OpenText($"{name}/in.json");
            var input = JsonConvert.DeserializeObject<InputClass>(streamReader.ReadToEnd());
            streamReader.Close();
            
            var tag = new ID3TagData
            {
                Album = name,
                Artist = "WayZer",
                Comment = "Made by WordVoiceCreater(by Way__Zer)",
            };
            for (int i = 0,n=0; i < input.Parts.Length; n+=input.Parts[i],i++)
            {
                var i1 = i;
                tag.Title = $"{name}-{i1 + 1}";
                var audiof = new MyAudioClass($"{name}/{name}.part{i1+1}",input,tag);
                audiof.Run(n, i1);
                audiof.Close();
            }
        }
    }
}