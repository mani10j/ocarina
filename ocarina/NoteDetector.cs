﻿using System;
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
        private AudioFrameOutputNode audioFrameOutput;
        private int recordToggle;
        private string note;

        public string Note
        {
            get
            { return this.note; }

            internal set
            {
                this.note = value;
                this.OnPropertyChanged();
            }
        }

        public NoteDetector()
        {
            recordToggle = 0;
            output = new List<string>();
            Note = "mynote";
        }

        public int HitButton()
        {

            if (recordToggle % 2 == 0)
                ag.Start();
            else
                ag.Stop();

            recordToggle = (recordToggle + 1) % 2;
            return recordToggle;

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

        public event PropertyChangedEventHandler PropertyChanged = delegate { };

        public void OnPropertyChanged([CallerMemberName] string propertyName = null)
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
            rootPage.ShowLogs("Output Node initialized successfully: " + audioO?.Device?.Name);
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
            rootPage.ShowLogs("Input Node initialized successfully: " + audioI?.Device?.Name);
        }

        private async Task InitProcessorNode()
        {
            audioFrameOutput = ag.CreateFrameOutputNode();
            if(audioFrameOutput!=null)
            {
                rootPage.ShowLogs("Processor Node initialized succeffully:");
                ag.QuantumStarted += Ag_QuantumStarted;
            }
            
        }

        private void Ag_QuantumStarted(AudioGraph sender, object args)
        {
            AudioFrame frame = audioFrameOutput.GetFrame();
            Note = frame.Duration.ToString();
        }

        private async Task ConnectGraphsNodes()
        {
            if(audioI != null)
            {
                if(audioO != null)
                    audioI.AddOutgoingConnection(audioO);
                if (audioFrameOutput != null)
                    audioI.AddOutgoingConnection(audioFrameOutput);
            }
            rootPage.ShowLogs("Connected nodes together.");
        }

    }
}
