using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Un4seen.Bass;
using Un4seen.Bass.AddOn.Fx;
using Un4seen.BassWasapi;
using System.Threading;
using System.Threading.Tasks;

namespace xmaslights
{
    class BeatDetector
    {

        public event Action OnBeat;
        private int _recordChan;

        private BPMBEATPROC _bpmBeatProc;
        private RECORDPROC _recordProc;
        private BassWasapiHandler _wasapi;
        private bool _isStarted;

        static BeatDetector()
        {
            byte[] hex = { 0x74, 0x68, 0x69, 0x6A, 0x73, 0x40, 0x62, 0x72, 0x6F, 0x6B, 0x65, 0x6E, 0x77, 0x69, 0x72, 0x65, 0x2E, 0x6E, 0x65, 0x74, 0x2D, 0x32, 0x58, 0x31, 0x38, 0x33, 0x31, 0x32, 0x38, 0x32, 0x30, 0x31, 0x31, 0x33, 0x37, 0x31, 0x38 };
            string[] str = ASCIIEncoding.ASCII.GetString(hex).Split('-');
            BassNet.Registration(str[0], str[1]);
        }

        public static void ShowAbout()
        {
            BassNet.ShowAbout(null);
        }

        public BeatDetector()
        {
            Bass.BASS_SetConfig(BASSConfig.BASS_CONFIG_UPDATEPERIOD, 0);
            Bass.BASS_Init(0, 48000, BASSInit.BASS_DEVICE_DEFAULT, IntPtr.Zero);
            // Wasapi only works on Vista, 7 and the likes
            var os = Environment.OSVersion;
            if (os.Platform == PlatformID.Win32NT && os.Version.Major >= 6)
            {
                InitWasapi();
            }
            else
            {
                InitBassapi();
            }
            _bpmBeatProc = new BPMBEATPROC(BpmBeatProc);
            _isStarted = false;
        }

        private void InitBassapi()
        {
            Bass.BASS_RecordInit(-1);
            _recordProc = new RECORDPROC(RecordProc);
        }

        private void InitWasapi()
        {
            int deviceid = 0;
            foreach (var device in BassWasapi.BASS_WASAPI_GetDeviceInfos())
            {
                if (device.IsInput && device.IsLoopback && device.IsEnabled && device.name.StartsWith("Speaker"))
                {
                    break;
                }
                deviceid++;
            }
            _wasapi = new BassWasapiHandler(deviceid, false, 48000, 2, 0, 0);
            _wasapi.Init();
        }

        public void Start()
        {
            if (_wasapi != null)
            {
                _wasapi.Start();
                _recordChan = _wasapi.InputChannel;
            }
            else
            {
                _recordChan = Bass.BASS_RecordStart(44100, 2, BASSFlag.BASS_DEFAULT, 10, _recordProc, IntPtr.Zero);
            }

            BassFx.BASS_FX_BPM_BeatCallbackSet(_recordChan, _bpmBeatProc, IntPtr.Zero);
            _isStarted = true;
        }

        public void Stop()
        {
            BassFx.BASS_FX_BPM_BeatCallbackReset(_recordChan);
            if (_wasapi != null)
            {
                _wasapi.Stop();
            }
            else
            {
                Bass.BASS_ChannelStop(_recordChan);
            }
            _isStarted = false;
        }

        private bool RecordProc(int handle, IntPtr buffer, int length, IntPtr user)
        {
            return true;
        }

        private void BpmBeatProc(int handle, double beatpos, IntPtr user)
        {
            Action onBeat = OnBeat;
            if (onBeat != null)
            {
                Task.Factory.StartNew(onBeat);
            }
        }
    }
}
