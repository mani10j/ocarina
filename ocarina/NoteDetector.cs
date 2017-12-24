using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Media.Audio;
using Windows.Media;
using Windows.Devices.Enumeration;
using Windows.Media.Devices;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Windows.Foundation;
using System.Runtime.InteropServices;

namespace ocarina
{
    public class NoteDetector : INotifyPropertyChanged
    {
        private MainPage rootPage;
        private List<string> output;
        private AudioGraph ag;
        private AudioDeviceInputNode audioI;
        private AudioDeviceOutputNode audioO;
        private DeviceInformationCollection outputDevices;
        private DeviceInformationCollection inputDevices;
        private AudioFrameOutputNode audioFrameProcessor;
        private bool isOn;
        private string note;
        private string time;
        public string Note
        {
            get
            { return this.note; }

            internal set
            {
                this.note = value;
                this.OnPropertyChanged();
                //rootPage.ShowNote(value);
            }
        }
        

        public string Timetrack
        {
            get
            { return this.time; }

            internal set
            {
                this.time = value;
                this.OnPropertyChanged();
                //rootPage.ShowNote(value);
            }
        }

        public NoteDetector()
        {
            isOn = false;
            output = new List<string>();
            //Note = "mynote";
        }

        public bool HitButton()
        {
            isOn = !isOn;
            if (isOn)
                ag.Start();
            else
                ag.Stop();

            return isOn;

        }

        public static NoteDetector Create(MainPage page)
        {
            NoteDetector nd = new NoteDetector
            {
                rootPage = page
            };
            nd.InitAudio().ConfigureAwait(false);

            return nd;
        }

        public void CleanUp()
        {
            ag?.Dispose();
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public void OnPropertyChanged([CallerMemberName]string propertyName = null)
        {
            // Raise the PropertyChanged event, passing the name of the property whose value has changed.
            this.PropertyChanged(this, new PropertyChangedEventArgs(propertyName));

        }



        private async Task InitAudio()
        {
            await FindDevices();
            await InitGraph();
            await InitInputeNode();
            await InitOuputNode();
            await InitProcessorNode();
            await ConnectGraphsNodes();
        }

        private async Task InitGraph()
        {
            AudioGraphSettings settings = new AudioGraphSettings(Windows.Media.Render.AudioRenderCategory.Media)
            {
                PrimaryRenderDevice = outputDevices[1],
                QuantumSizeSelectionMode = QuantumSizeSelectionMode.LowestLatency
            };


            var audioGraphResult = await AudioGraph.CreateAsync(settings);
            if (audioGraphResult.Status != AudioGraphCreationStatus.Success)
            {
                rootPage.ShowLogs("Graph failed: " + audioGraphResult.Status);
                return;
            }
            ag = audioGraphResult.Graph;
            rootPage.ShowLogs("Graph initialized successfully.");
        }

        private async Task FindDevices()
        {
            outputDevices = await DeviceInformation.FindAllAsync(DeviceClass.AudioRender);
            inputDevices = await DeviceInformation.FindAllAsync(DeviceClass.AudioCapture);
        }

        private async Task InitOuputNode()
        {
            var audioDeviceOutputNodeResult = await ag.CreateDeviceOutputNodeAsync();
            if (audioDeviceOutputNodeResult.Status != AudioDeviceNodeCreationStatus.Success)
            {
                rootPage.ShowLogs("Output device failed: " + audioDeviceOutputNodeResult.Status);
                return;
            }
            audioO = audioDeviceOutputNodeResult.DeviceOutputNode;
            rootPage.ShowLogs("Output Node initialized successfully: " + audioO?.Device?.Name
                + " (channels :" + audioO?.EncodingProperties.ChannelCount + " )");
        }

        private async Task InitInputeNode()
        {
            var audioDeviceInputNodeResult = await ag.CreateDeviceInputNodeAsync(Windows.Media.Capture.MediaCategory.Other);
            if (audioDeviceInputNodeResult.Status != AudioDeviceNodeCreationStatus.Success)
            {
                rootPage.ShowLogs("Input device failed: " + audioDeviceInputNodeResult.Status);
                return;
            }
            audioI = audioDeviceInputNodeResult.DeviceInputNode;
            rootPage.ShowLogs("Input Node initialized successfully: " + audioI?.Device?.Name 
                + " (channels :" + audioI?.EncodingProperties.ChannelCount+ " )");
            
        }

        private async Task InitProcessorNode()
        {
            audioFrameProcessor = ag.CreateFrameOutputNode();
            if (audioFrameProcessor != null)
            {
                rootPage.ShowLogs("Processor Node initialized successfully:");
                ag.QuantumStarted += Ag_QuantumStarted;
            }

        }

        [ComImport]
        [Guid("5B0D3235-4DBA-4D44-865E-8F1D0E4FD04D")]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        unsafe interface IMemoryBufferByteAccess
        {
            void GetBuffer(out byte* buffer, out uint capacity);
        }
        unsafe private string ProcessFrameOutput(AudioFrame frame)
        {
            
            using (AudioBuffer buffer = frame.LockBuffer(AudioBufferAccessMode.Write))
            using (IMemoryBufferReference reference = buffer.CreateReference())
            {
                byte* dataInBytes;
                uint capacityInBytes;
                float* dataInFloat;

                // Get the buffer from the AudioFrame
                ((IMemoryBufferByteAccess)reference).GetBuffer(out dataInBytes, out capacityInBytes);

                dataInFloat = (float*)dataInBytes;

                uint sampleCount = capacityInBytes / sizeof(float);
                
                float mean1 = 0.0f;
                float mean2 = 0.0f;
                float variance = 0.0f;
                for (uint i =0; i< sampleCount; i++)
                {
                    if(i%2==0)
                        mean1 += Math.Abs( dataInFloat[i]);
                    else
                        mean2 += Math.Abs(dataInFloat[i]);

                }
                mean1 = mean1 / sampleCount;
                mean2 = mean2 / sampleCount;

                return String.Format("{0} <--> {1}", mean1, mean2);
            }
            
        }

        private void Ag_QuantumStarted(AudioGraph sender, object args)
        {
            AudioFrame frame = audioFrameProcessor.GetFrame();
            

            Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal,
                () =>
                {
                    Note = ProcessFrameOutput(frame);
                    Timetrack = frame.RelativeTime.ToString();
                }
                );
            
            
        }

        private async Task ConnectGraphsNodes()
        {
            if (audioI != null)
            {
                if (audioO != null)
                    audioI.AddOutgoingConnection(audioO);
                if (audioFrameProcessor != null)
                    audioI.AddOutgoingConnection(audioFrameProcessor);
            }
            rootPage.ShowLogs("Connected nodes together." + ag.EncodingProperties.ToString());
        }

    }
}
