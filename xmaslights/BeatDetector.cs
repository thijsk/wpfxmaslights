using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Un4seen.Bass;
using Un4seen.Bass.AddOn.Fx;
using Un4seen.BassWasapi;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace xmaslights
{
    class BeatDetector
    {

        public event Action OnBeat;
        private int _recordChan;

        private BPMBEATPROC _bpmBeatProc;
        private RECORDPROC _recordProc;
        private BassWasapiHandler _wasapi;
        private static List<AudioDevice> _devices = InitAudiodeviceList();
        private Dispatcher _threadDispatcher;
      
        static BeatDetector()
        {
            byte[] hex = { 0x74, 0x68, 0x69, 0x6A, 0x73, 0x40, 0x62, 0x72, 0x6F, 0x6B, 0x65, 0x6E, 0x77, 0x69, 0x72, 0x65, 0x2E, 0x6E, 0x65, 0x74, 0x2D, 0x32, 0x58, 0x31, 0x38, 0x33, 0x31, 0x32, 0x38, 0x32, 0x30, 0x31, 0x31, 0x33, 0x37, 0x31, 0x38 };
            string[] str = ASCIIEncoding.ASCII.GetString(hex).Split('-');
            BassNet.Registration(str[0], str[1]);

            Bass.LoadMe();
            BassWasapi.LoadMe(); 
            BassFx.LoadMe();
        }

        public BeatDetector()
        {
            
        }

        private void CreateThread()
        {
           
        }

        public class AudioDevice
        {
            public int DeviceId { get; set; }
            public string Name { get; set; }
        }

        public static IEnumerable<AudioDevice> GetAudioDevices()
        {
            return _devices;
        }

        private static List<AudioDevice> InitAudiodeviceList()
        {
            List<AudioDevice> devices = new List<AudioDevice>();
            devices.Add(new AudioDevice() { DeviceId = -1, Name = "Autodetect" });
            int devicecount = 0;
            if (HasWasapi)
            {
                foreach (var bassdevice in BassWasapi.BASS_WASAPI_GetDeviceInfos())
                {
                    if (bassdevice.IsEnabled && bassdevice.IsInput)
                    {
                        devices.Add(new AudioDevice() { DeviceId = devicecount, Name = bassdevice.name + ((bassdevice.IsLoopback) ? " (Loopback)" : "") });
                    }
                    devicecount++;
                }
            }
            else
            {
                foreach (var bassdevice in Bass.BASS_RecordGetDeviceInfos())
                {
                    if (bassdevice.IsEnabled)
                    {
                        devices.Add(new AudioDevice() { DeviceId = devicecount, Name = bassdevice.name });
                    }
                    devicecount++;
                }
            }
            return devices;
        }

        public static void ShowAbout()
        {
            BassNet.ShowAbout(null);
        }

        private static bool HasWasapi
        {
            get {  
                var os = Environment.OSVersion;
                return (os.Platform == PlatformID.Win32NT && os.Version.Major >= 6);
            }
        }

        private void InitBassapi(int deviceId)
        {
            if (deviceId != -1)
            {
                if (Bass.BASS_GetDeviceInfo(deviceId) == null)
                {
                    deviceId = -1;
                }
            }
            Bass.BASS_RecordInit(-1);
            _recordProc = new RECORDPROC(RecordProc);
            _recordChan = Bass.BASS_RecordStart(44100, 2, BASSFlag.BASS_DEFAULT, 10, _recordProc, IntPtr.Zero);
        }

        private void InitWasapi(int deviceId)
        {
            if (deviceId == -1)
            {
                deviceId = AutodetectWasapiDevice();
            }
            else
            {
                if (BassWasapi.BASS_WASAPI_GetDeviceInfo(deviceId) == null)
                    deviceId = AutodetectWasapiDevice();
            }
            var info = BassWasapi.BASS_WASAPI_GetDeviceInfo(deviceId);
            _wasapi = new BassWasapiHandler(deviceId, false, info.mixfreq, info.mixchans, 0, info.minperiod);
            _wasapi.Init();
            _wasapi.Start();
            _recordChan = _wasapi.InputChannel;
        }

        private static int AutodetectWasapiDevice()
        {
            int deviceId = 0;
            foreach (var device in BassWasapi.BASS_WASAPI_GetDeviceInfos())
            {
                if (device.SupportsRecording && device.IsEnabled && device.name.StartsWith("Speaker"))
                {
                    break;
                }
                deviceId++;
            }
            return deviceId;
        }

        public void Start(int deviceId)
        {
            Thread backgroundThread = new Thread(() =>
            {
                try
                {
                    _threadDispatcher = Dispatcher.CurrentDispatcher;
                    _threadDispatcher.BeginInvoke(new Action(() =>
                        {
                            Bass.BASS_SetConfig(BASSConfig.BASS_CONFIG_UPDATEPERIOD, 0);
                            Bass.BASS_Init(0, 44100, BASSInit.BASS_DEVICE_DEFAULT, IntPtr.Zero);
                            // Wasapi only works on Vista, 7 and the likes
                            if (HasWasapi)
                            {
                                InitWasapi(deviceId);
                            }
                            else
                            {
                                InitBassapi(deviceId);
                            }
                            _bpmBeatProc = new BPMBEATPROC(BpmBeatProc);

                            BassFx.BASS_FX_BPM_BeatCallbackSet(_recordChan, _bpmBeatProc, IntPtr.Zero);
                        }
                    ));
                    Dispatcher.Run();
                }
                catch (Exception e)
                {
                    App.ReportException(e);
                }
            });
            backgroundThread.IsBackground = true;
            backgroundThread.SetApartmentState(ApartmentState.STA);
            backgroundThread.Priority = ThreadPriority.Normal;
            backgroundThread.Start();
        }

        public void Stop()
        {
            _threadDispatcher.Invoke(new Action(() =>
                {
                    BassFx.BASS_FX_BPM_BeatCallbackReset(_recordChan);
                    if (HasWasapi)
                    {
                        _wasapi.Stop();
                        _wasapi.Dispose();
                        _wasapi = null;
                        BassWasapi.BASS_WASAPI_Free();
                    }
                    else
                    {
                        Bass.BASS_ChannelStop(_recordChan);
                    }
                    Bass.BASS_Free();
                }));
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
                onBeat();
            }
        }
    }
}
