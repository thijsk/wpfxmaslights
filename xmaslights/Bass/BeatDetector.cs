using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Un4seen.Bass;
using Un4seen.Bass.AddOn.Fx;
using System.Threading;
using System.Threading.Tasks;

namespace BeatDetect
{
    class BeatDetector
    {

        public event Action OnBeat;

        private int _recordChan;
        BPMBEATPROC _bpmBeatProc;
        RECORDPROC _recordProc;
        public BeatDetector()
        {
            BassNet.Registration("thijs@brokenwire.net", "2X18312820113718");
            Bass.BASS_Init(-1, 44100, BASSInit.BASS_DEVICE_DEFAULT, IntPtr.Zero);
            Bass.BASS_RecordInit(-1);
            _bpmBeatProc = new BPMBEATPROC(BpmBeatProc);
            _recordProc = new RECORDPROC(RecordProc);
        }

        public void Start()
        {
            _recordChan = Bass.BASS_RecordStart(44100, 2, BASSFlag.BASS_DEFAULT, 10, _recordProc, IntPtr.Zero);
            BassFx.BASS_FX_BPM_BeatCallbackSet(_recordChan, _bpmBeatProc, IntPtr.Zero);
        }
        
        public void Stop()
        {
            BassFx.BASS_FX_BPM_BeatCallbackReset(_recordChan);
            Bass.BASS_ChannelStop(_recordChan);
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
