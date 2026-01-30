using System;
using System.Drawing;
using System.IO;
using System.Media;
using System.Windows.Forms;

namespace ConsoleApp3
{
    public enum WaveType
    {
        Sine,
        Square,
        Triangle,
        Sawtooth
    }

    public class WaveGeneratorForm : Form
    {
        private ComboBox comboWave;
        private NumericUpDown numFreq;
        private NumericUpDown numAmp;
        private NumericUpDown numDur;
        private NumericUpDown numSampleRate;
        private Button btnPlay;
        private Button btnSave;
        private PictureBox picPreview;
        private Label lblFreq, lblAmp, lblDur, lblSR, lblWave;

        public WaveGeneratorForm()
        {
            Text = "Wave Generator";
            ClientSize = new Size(720, 360);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;

            lblWave = new Label { Text = "Waveform:", Location = new Point(12, 14), AutoSize = true };
            comboWave = new ComboBox { Location = new Point(90, 10), Width = 120, DropDownStyle = ComboBoxStyle.DropDownList };
            comboWave.Items.AddRange(new[] { "Sine", "Square", "Triangle", "Sawtooth" });
            comboWave.SelectedIndex = 0;

            lblFreq = new Label { Text = "Frequency (Hz):", Location = new Point(230, 14), AutoSize = true };
            numFreq = new NumericUpDown { Location = new Point(340, 10), Minimum = 1, Maximum = 20000, Value = 440, Width = 80 };

            lblAmp = new Label { Text = "Amplitude (%):", Location = new Point(430, 14), AutoSize = true };
            numAmp = new NumericUpDown { Location = new Point(520, 10), Minimum = 1, Maximum = 100, Value = 80, Width = 60 };

            lblDur = new Label { Text = "Duration (s):", Location = new Point(12, 46), AutoSize = true };
            numDur = new NumericUpDown { Location = new Point(90, 42), Minimum = 1, Maximum = 30, Value = 2, Width = 80 };

            lblSR = new Label { Text = "Sample Rate:", Location = new Point(190, 46), AutoSize = true };
            numSampleRate = new NumericUpDown { Location = new Point(270, 42), Minimum = 8000, Maximum = 96000, Increment = 11025, Value = 44100, Width = 100 };

            btnPlay = new Button { Text = "Generate && Play", Location = new Point(390, 40), Width = 120 };
            btnSave = new Button { Text = "Save WAV...", Location = new Point(520, 40), Width = 100 };

            picPreview = new PictureBox { Location = new Point(12, 80), Size = new Size(696, 260), BorderStyle = BorderStyle.FixedSingle, BackColor = Color.Black };

            Controls.AddRange(new Control[] { lblWave, comboWave, lblFreq, numFreq, lblAmp, numAmp, lblDur, numDur, lblSR, numSampleRate, btnPlay, btnSave, picPreview });

            comboWave.SelectedIndexChanged += (s, e) => DrawPreview();
            numFreq.ValueChanged += (s, e) => DrawPreview();
            numAmp.ValueChanged += (s, e) => DrawPreview();
            numSampleRate.ValueChanged += (s, e) => DrawPreview();

            btnPlay.Click += BtnPlay_Click;
            btnSave.Click += BtnSave_Click;

            DrawPreview();
        }

        private void BtnPlay_Click(object sender, EventArgs e)
        {
            var waveType = (WaveType)comboWave.SelectedIndex;
            int freq = (int)numFreq.Value;
            double amp = (double)numAmp.Value / 100.0;
            int sampleRate = (int)numSampleRate.Value;
            double duration = (double)numDur.Value;

            short[] pcm = GenerateWaveSamples(waveType, freq, amp, duration, sampleRate);
            using (MemoryStream ms = WriteWavToStream(pcm, sampleRate, 1))
            {
                ms.Position = 0;
                using (SoundPlayer player = new SoundPlayer(ms))
                {
                    try
                    {
                        player.Play();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Playback failed: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        private void BtnSave_Click(object sender, EventArgs e)
        {
            var waveType = (WaveType)comboWave.SelectedIndex;
            int freq = (int)numFreq.Value;
            double amp = (double)numAmp.Value / 100.0;
            int sampleRate = (int)numSampleRate.Value;
            double duration = (double)numDur.Value;

            short[] pcm = GenerateWaveSamples(waveType, freq, amp, duration, sampleRate);
            using (MemoryStream ms = WriteWavToStream(pcm, sampleRate, 1))
            using (SaveFileDialog sfd = new SaveFileDialog { Filter = "WAV files|*.wav", FileName = "wave.wav" })
            {
                if (sfd.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        File.WriteAllBytes(sfd.FileName, ms.ToArray());
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Save failed: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        private void DrawPreview()
        {
            int width = picPreview.Width;
            int height = picPreview.Height;
            Bitmap bmp = new Bitmap(width, height);
            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.Clear(Color.Black);
                // Grid
                using (Pen p = new Pen(Color.DimGray))
                {
                    for (int x = 0; x < width; x += 50) g.DrawLine(p, x, 0, x, height);
                    for (int y = 0; y < height; y += 50) g.DrawLine(p, 0, y, width, y);
                }

                var waveType = (WaveType)comboWave.SelectedIndex;
                int freq = (int)numFreq.Value;
                double amp = (double)numAmp.Value / 100.0;
                int sampleRate = (int)numSampleRate.Value;

                // Generate small preview buffer (one period or a bit more)
                double period = 1.0 / Math.Max(1, freq);
                double previewSeconds = Math.Min(0.02, period * 4); // show up to 20ms or 4 periods
                short[] pcm = GenerateWaveSamples(waveType, freq, amp, previewSeconds, sampleRate);

                // Draw waveform center line
                g.DrawLine(Pens.DarkGreen, 0, height / 2, width, height / 2);

                using (Pen pen = new Pen(Color.Lime, 1.5f))
                {
                    for (int i = 1; i < pcm.Length; i++)
                    {
                        float x1 = (i - 1f) / (pcm.Length - 1f) * (width - 1);
                        float y1 = height / 2 - (pcm[i - 1] / 32768f) * (height / 2 - 4);
                        float x2 = (i) / (pcm.Length - 1f) * (width - 1);
                        float y2 = height / 2 - (pcm[i] / 32768f) * (height / 2 - 4);
                        g.DrawLine(pen, x1, y1, x2, y2);
                    }
                }
            }
            var old = picPreview.Image;
            picPreview.Image = bmp;
            if (old != null) old.Dispose();
        }

        private short[] GenerateWaveSamples(WaveType type, int frequency, double amplitude, double durationSeconds, int sampleRate)
        {
            int sampleCount = (int)(sampleRate * durationSeconds);
            short[] samples = new short[sampleCount];
            double twoPi = 2.0 * Math.PI;

            for (int n = 0; n < sampleCount; n++)
            {
                double t = (double)n / sampleRate;
                double value = 0.0;
                double phase = twoPi * frequency * t;

                switch (type)
                {
                    case WaveType.Sine:
                        value = Math.Sin(phase);
                        break;
                    case WaveType.Square:
                        value = Math.Sign(Math.Sin(phase));
                        break;
                    case WaveType.Triangle:
                        // triangle from sawtooth integration
                        value = 2.0 * Math.Asin(Math.Sin(phase)) / Math.PI;
                        break;
                    case WaveType.Sawtooth:
                        // normalized sawtooth (-1..1)
                        value = 2.0 * (t * frequency - Math.Floor(0.5 + t * frequency));
                        break;
                }

                double scaled = value * amplitude;
                // clamp
                if (scaled > 1.0) scaled = 1.0;
                if (scaled < -1.0) scaled = -1.0;
                samples[n] = (short)(scaled * short.MaxValue);
            }

            return samples;
        }

        private MemoryStream WriteWavToStream(short[] pcm, int sampleRate, short channels)
        {
            MemoryStream ms = new MemoryStream();
            using (BinaryWriter bw = new BinaryWriter(ms, System.Text.Encoding.UTF8, true))
            {
                int bytesPerSample = 2; // 16-bit
                int byteRate = sampleRate * channels * bytesPerSample;
                int blockAlign = (short)(channels * bytesPerSample);
                int dataSize = pcm.Length * bytesPerSample * channels;

                // RIFF header
                bw.Write(System.Text.Encoding.ASCII.GetBytes("RIFF"));
                bw.Write(36 + dataSize); // file size - 8
                bw.Write(System.Text.Encoding.ASCII.GetBytes("WAVE"));

                // fmt chunk
                bw.Write(System.Text.Encoding.ASCII.GetBytes("fmt "));
                bw.Write(16); // PCM chunk size
                bw.Write((short)1); // PCM format
                bw.Write((short)channels);
                bw.Write(sampleRate);
                bw.Write(byteRate);
                bw.Write((short)blockAlign);
                bw.Write((short)(bytesPerSample * 8)); // bits per sample

                // data chunk
                bw.Write(System.Text.Encoding.ASCII.GetBytes("data"));
                bw.Write(dataSize);

                // write samples (mono). If channels >1 you'd interleave.
                for (int i = 0; i < pcm.Length; i++)
                {
                    bw.Write(pcm[i]);
                }

                bw.Flush();
                ms.Position = 0;
                return ms;
            }
        }
    }
}